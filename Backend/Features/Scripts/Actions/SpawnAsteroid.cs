using System.Collections.Generic;
using System.Threading.Tasks;
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

        var number = random.Next(1, 100);
        var minTier = (int)(long)actionItem.Properties.GetValueOrDefault("MinTier", 4);
        var maxTier = (int)(long)actionItem.Properties.GetValueOrDefault("MaxTier", 5) + 1;

        var isPublished = actionItem.Properties.GetValueOrDefault("Published", false)
            .SafeCastOrDefault(false);

        var centerJObject = actionItem.Properties.GetValueOrDefault("Center", JObject.FromObject(context.Sector))
            .SafeCastOrDefault(JObject.FromObject(context.Sector));
        
        var center = centerJObject.ToObject<Vec3>();

        var tier = random.Next(minTier, maxTier);

        var pointGenerator = pointGeneratorFactory.Create(actionItem.Area);
        var position = center + pointGenerator.NextPoint(random);

        var asteroidId = await asteroidManagerGrain.SpawnAsteroid(
            tier,
            $"basic_{tier}_{number}.json",
            position,
            2
        );

        var constructService = context.ServiceProvider.GetRequiredService<IConstructService>();
        var info = await constructService.GetConstructInfoAsync(asteroidId);

        if (info.Info != null)
        {
            var name = info.Info.rData.name
                .Replace("A-", "R-");

            await constructService.RenameConstruct(asteroidId, name);
        }

        if (isPublished)
        {
            await asteroidManagerGrain.ForcePublish(asteroidId);
        }

        return ScriptActionResult.Successful();
    }
}