using System;
using System.Threading.Tasks;
using Backend;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Common.Interfaces;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Services;
using Mod.DynamicEncounters.Helpers;
using Newtonsoft.Json.Linq;
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
        var random = context.ServiceProvider.GetRequiredService<IRandomProvider>().GetRandom();
        var pointGeneratorFactory = context.ServiceProvider.GetRequiredService<IPointGeneratorFactory>();
        var orleans = context.ServiceProvider.GetOrleans();
        var asteroidManagerGrain = orleans.GetAsteroidManagerGrain();
        var constructService = context.ServiceProvider.GetRequiredService<IConstructService>();
        var bank = context.ServiceProvider.GetGameplayBank();

        var number = random.Next(1, 100);
        var properties = JObject.FromObject(actionItem.Properties).ToObject<Properties>();

        var minTier = properties.MinTier;
        var maxTier = properties.MaxTier + 1;
        var isPublished = properties.Published;
        var center = properties.Center ?? context.Sector;

        var tier = random.Next(minTier, maxTier);

        var pointGenerator = pointGeneratorFactory.Create(actionItem.Area);
        var position = center + pointGenerator.NextPoint(random);

        var asteroidId = await asteroidManagerGrain.SpawnAsteroid(
            tier,
            $"basic_{tier}_{number}.json",
            position,
            2
        );

        var info = await constructService.GetConstructInfoAsync(asteroidId);

        if (info.Info == null)
        {
            return ScriptActionResult.Failed();
        }

        var name = info.Info.rData.name
            .Replace("A-", "R-");

        await constructService.RenameConstruct(asteroidId, name);

        if (isPublished)
        {
            await asteroidManagerGrain.ForcePublish(asteroidId);

            var spawnScriptAction = new SpawnScriptAction(new ScriptActionItem
            {
                Position = position,
                Prefab = properties.PointOfInterestPrefabName,
                Override = new ScriptActionOverrides
                {
                    ConstructName = name,
                }
            });

            var spawnResult = await spawnScriptAction.ExecuteAsync(
                new ScriptContext(
                    context.ServiceProvider,
                    context.FactionId,
                    context.PlayerIds,
                    position,
                    context.TerritoryId)
                {
                    Properties = context.Properties
                });

            if (!spawnResult.Success)
            {
                return spawnResult;
            }

            if (!context.ConstructId.HasValue)
            {
                return spawnResult;
            }

            var asteroidManagerConfig = bank.GetBaseObject<AsteroidManagerConfig>();
            var deletePoiTimeSpan = properties.DeletePointOfInterestTimeSpan ?? TimeSpan.FromDays(asteroidManagerConfig.lifetimeDays);

            await constructService.SetAutoDeleteFromNowAsync(
                context.ConstructId.Value,
                deletePoiTimeSpan
            );
        }

        return ScriptActionResult.Successful();
    }

    public class Properties
    {
        public int MinTier { get; set; } = 4;
        public int MaxTier { get; set; } = 5;
        public bool Published { get; set; } = true;
        public Vec3? Center { get; set; }
        public string PointOfInterestPrefabName { get; set; } = "poi-asteroid";
        public TimeSpan? DeletePointOfInterestTimeSpan { get; set; }
    }
}