using System;
using System.Threading.Tasks;
using Backend;
using Backend.Scenegraph;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Common.Interfaces;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Services;
using Mod.DynamicEncounters.Helpers;
using Newtonsoft.Json;
using NQ;
using NQ.Interfaces;
using NQutils.Def;

namespace Mod.DynamicEncounters.Features.Scripts.Actions;

[ScriptActionName(ActionName)]
public class SpawnAsteroid(ScriptActionItem actionItem) : IScriptAction
{
    public const string ActionName = "spawn-asteroid";
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

        var number = random.Next(1, 100);
        var properties = actionItem.GetProperties<Properties>();

        var minTier = properties.MinTier;
        var maxTier = properties.MaxTier + 1;
        var isPublished = properties.Published;
        var center = properties.Center ?? context.Sector;

        var tier = random.Next(minTier, maxTier);

        var pointGenerator = pointGeneratorFactory.Create(actionItem.Area);
        var position = center + pointGenerator.NextPoint(random);

        var asteroidId = await asteroidManagerGrain.SpawnAsteroid(
            properties.TierOverride ?? tier,
            $"{properties.FileNamePrefix}_{tier}_{number}.json",
            position,
            properties.PlanetId
        );

        var info = await constructService.GetConstructInfoAsync(asteroidId);

        if (info.Info == null)
        {
            return ScriptActionResult.Failed();
        }

        var name = info.Info.rData.name
            .Replace("A-", "R-");

        await constructService.RenameConstruct(asteroidId, name);

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
                    name: name
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

        await Task.Delay(500);

        return actionResult;
    }

    public class Properties
    {
        [JsonProperty] public int MinTier { get; set; }
        [JsonProperty] public int MaxTier { get; set; }
        [JsonProperty] public bool Published { get; set; }
        [JsonProperty] public Vec3? Center { get; set; }
        [JsonProperty] public string PointOfInterestPrefabName { get; set; } = "poi-asteroid";
        [JsonProperty] public string FileNamePrefix { get; set; } = "basic";
        [JsonProperty] public ulong PlanetId { get; set; } = 2;
        [JsonProperty] public TimeSpan? AutoDeleteTimeSpan { get; set; }
        [JsonProperty] public int? TierOverride { get; set; }

        /// <summary>
        /// Does not show on DSAT but deletes automatically.
        /// </summary>
        [JsonProperty]
        public bool HiddenFromDsat { get; set; }
    }
}