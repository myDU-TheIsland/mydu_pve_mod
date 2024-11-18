using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Common.Interfaces;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Services;
using Mod.DynamicEncounters.Features.TaskQueue.Interfaces;
using Mod.DynamicEncounters.Helpers;
using NQ;
using NQ.Interfaces;

namespace Mod.DynamicEncounters.Features.Scripts.Actions;

[ScriptActionName(ActionName)]
public class SpawnSectorAsteroid(ScriptActionItem actionItem) : IScriptAction
{
    public const string ActionName = "spawn-sector-asteroid";
    public string Name => ActionName;
    public string GetKey() => Name;

    public async Task<ScriptActionResult> ExecuteAsync(ScriptContext context)
    {
        var logger = context.ServiceProvider.CreateLogger<SpawnSectorAsteroid>();
        var areaScanService = context.ServiceProvider.GetRequiredService<IAreaScanService>();
        var taskQueueService = context.ServiceProvider.GetRequiredService<ITaskQueueService>();
        var random = context.ServiceProvider.GetRequiredService<IRandomProvider>().GetRandom();
        var orleans = context.ServiceProvider.GetOrleans();
        var asteroidManagerGrain = orleans.GetAsteroidManagerGrain();

        if (!actionItem.Properties.TryGetValue("File", out var file) || file == null)
        {
            logger.LogError("File not found on script properties");
            
            return ScriptActionResult.Failed();
        }

        var contacts = await areaScanService.ScanForAsteroids(context.Sector, 20 * DistanceHelpers.OneSuInMeters);
        foreach (var contact in contacts)
        {
            await taskQueueService.EnqueueScript(
                new ScriptActionItem
                {
                    Type = "delete",
                    ConstructId = contact.ConstructId
                },
                DateTime.UtcNow
            );
        }

        var offset = random.NextDouble() * actionItem.Area.Radius;
        var direction = random.RandomDirectionVec3();
        var position = context.Sector + new Vec3
        {
            x = offset * direction.x,
            y = offset * direction.y,
            z = offset * direction.z
        };
        
        var asteroidId = await asteroidManagerGrain.SpawnAsteroid(
            5,
            $"{file}",
            position,
            2
        );

        await asteroidManagerGrain.ForcePublish(asteroidId);

        return ScriptActionResult.Successful();
    }
}