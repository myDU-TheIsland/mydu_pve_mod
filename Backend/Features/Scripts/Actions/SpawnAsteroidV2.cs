﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend;
using Backend.Scenegraph;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Common.Interfaces;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Services;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Features.Spawner.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Services;
using Mod.DynamicEncounters.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NQ;
using NQ.Interfaces;
using NQutils.Def;

namespace Mod.DynamicEncounters.Features.Scripts.Actions;

[ScriptActionName(ActionName)]
public class SpawnAsteroidV2(ScriptActionItem actionItem) : IScriptAction
{
    public const string ActionName = "spawn-asteroid-v2";
    public string Name => ActionName;
    public string GetKey() => Name;

    public async Task<ScriptActionResult> ExecuteAsync(ScriptContext context)
    {
        var provider = ModBase.ServiceProvider;
        var random = provider.GetRequiredService<IRandomProvider>().GetRandom();
        var pointGeneratorFactory = provider.GetRequiredService<IPointGeneratorFactory>();
        var orleans = provider.GetOrleans();
        var asteroidManagerGrain = orleans.GetAsteroidManagerGrain();
        var constructService = provider.GetRequiredService<IConstructService>();
        var bank = provider.GetGameplayBank();
        var sceneGraph = provider.GetRequiredService<IScenegraph>();
        var asteroidSpawner = provider.GetRequiredService<IAsteroidSpawnerService>();

        var properties = actionItem.GetProperties<Properties>();

        if (properties.Ores.Count == 0)
        {
            var ore4 = bank.GetDefinition("Ore4");
            var ore5 = bank.GetDefinition("Ore5");

            var ore4List = ore4!.GetChildren()
                .Select(o => o.Name)
                .ToList();
            var ore5List = ore5!.GetChildren()
                .Select(o => o.Name)
                .ToHashSet();
            ore5List.Remove("ThoramineOre");

            var ores = new List<string>(ore4List);
            ores.AddRange(ore5List);

            properties.Ores = new Dictionary<string, string[]>
            {
                { "Ore1", ores.ToArray() }
            };
        }

        var minTier = properties.MinTier;
        var maxTier = properties.MaxTier + 1;
        var isPublished = properties.Published;
        var center = properties.Center ?? context.Sector;

        var tier = random.Next(minTier, maxTier);

        var pointGenerator = pointGeneratorFactory.Create(actionItem.Area);
        var position = center + pointGenerator.NextPoint(random);

        var minRadius = Math.Clamp(properties.MinRadius, AsteroidData.MinRadius, AsteroidData.MaxRadius);
        var maxRadius = Math.Clamp(properties.MaxRadius, minRadius, AsteroidData.MaxRadius);
        var size = random.Next(minRadius, maxRadius + 1);

        var jsonString = properties.Data.ToString();
        foreach (var kvp in properties.Ores)
        {
            jsonString = jsonString.Replace(kvp.Key, random.PickOneAtRandom(kvp.Value));
        }

        properties.Data = JToken.Parse(jsonString);

        var outcome = await asteroidSpawner.SpawnAsteroidWithData(new SpawnAsteroidCommand
        {
            Position = position,
            Tier = tier,
            Radius = size,
            Gravity = properties.Gravity,
            Planet = properties.PlanetId,
            Prefix = properties.NamePrefix,
            AreaSize = properties.AreaSize,
            VoxelLod = properties.VoxelLod,
            VoxelSize = properties.VoxelSize,
            RegisterAsteroid = properties.Published,
            Data = properties.Data
        });

        if (!outcome.Success)
        {
            return ScriptActionResult.Failed();
        }

        var asteroidId = outcome.AsteroidId!.Value;

        var info = await constructService.GetConstructInfoAsync(asteroidId);

        if (info.Info == null)
        {
            return ScriptActionResult.Failed();
        }

        var asteroidCenterPos = await sceneGraph.GetConstructCenterWorldPosition(asteroidId);

        var asteroidManagerConfig = bank.GetBaseObject<AsteroidManagerConfig>();
        var deletePoiTimeSpan = properties.AutoDeleteTimeSpan ??
                                TimeSpan.FromDays(asteroidManagerConfig.lifetimeDays);

        if (isPublished)
        {
            await asteroidManagerGrain.ForcePublish(asteroidId);

            var spawnContext = new ScriptContext(
                context.FactionId,
                context.PlayerIds,
                asteroidCenterPos,
                context.TerritoryId)
            {
                Properties = context.Properties
            };

            var spawnResult = await Script.SpawnAsteroidMarker(
                    prefab: properties.PointOfInterestPrefabName,
                    name: info.Info.rData.name
                )
                .ToScriptAction()
                .ExecuteAsync(spawnContext);

            if (!spawnResult.Success)
            {
                return spawnResult;
            }

            if (!spawnContext.ConstructId.HasValue)
            {
                return spawnResult;
            }

            await constructService.SetAutoDeleteFromNowAsync(
                spawnContext.ConstructId.Value,
                deletePoiTimeSpan
            );

            await Script.DeleteConstruct(spawnContext.ConstructId.Value)
                .EnqueueRunAsync(startAt: DateTime.UtcNow + deletePoiTimeSpan);
        }

        if (properties.HiddenFromDsat)
        {
            await provider.GetRequiredService<IAsteroidService>()
                .HideFromDsatListAsync(asteroidId);

            await Script.DeleteAsteroid(asteroidId)
                .EnqueueRunAsync(startAt: DateTime.UtcNow + deletePoiTimeSpan);
        }

        var scriptActionFactory = provider.GetRequiredService<IScriptActionFactory>();
        var action = scriptActionFactory.Create(actionItem.Actions);

        var actionContext = new ScriptContext(
            context.FactionId,
            context.PlayerIds,
            asteroidCenterPos,
            context.TerritoryId)
        {
            Properties = context.Properties
        };

        var actionResult = await action.ExecuteAsync(actionContext);

        return actionResult;
    }

    public class Properties
    {
        [JsonProperty] public int MinTier { get; set; }
        [JsonProperty] public int MaxTier { get; set; }
        [JsonProperty] public bool Published { get; set; }
        [JsonProperty] public Vec3? Center { get; set; }
        [JsonProperty] public string PointOfInterestPrefabName { get; set; } = "poi-asteroid";
        [JsonProperty] public ulong PlanetId { get; set; } = 2;
        [JsonProperty] public TimeSpan? AutoDeleteTimeSpan { get; set; }
        [JsonProperty] public int? TierOverride { get; set; }
        [JsonProperty] public double Gravity { get; set; }
        [JsonProperty] public string NamePrefix { get; set; } = "R";
        [JsonProperty] public int VoxelLod { get; set; } = 7;
        [JsonProperty] public int VoxelSize { get; set; } = 512;
        [JsonProperty] public int AreaSize { get; set; } = 256;
        [JsonProperty] public int MinRadius { get; set; } = AsteroidData.MinRadius;
        [JsonProperty] public int MaxRadius { get; set; } = 64;

        [JsonProperty] public Dictionary<string, string[]> Ores { get; set; } = [];
        [JsonProperty] public JToken Data { get; set; }

        /// <summary>
        /// Does not show on DSAT but deletes automatically.
        /// </summary>
        [JsonProperty]
        public bool HiddenFromDsat { get; set; }
    }
}