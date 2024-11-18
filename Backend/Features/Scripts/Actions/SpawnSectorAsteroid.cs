using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Services;
using Mod.DynamicEncounters.Features.TaskQueue.Interfaces;
using Mod.DynamicEncounters.Helpers;
using NQ.Interfaces;

namespace Mod.DynamicEncounters.Features.Scripts.Actions;

[ScriptActionName(ActionName)]
public class SpawnSectorAsteroid(ScriptActionItem actionItem) : IScriptAction
{
    private readonly ScriptActionItem _actionItem = actionItem;
    public const string ActionName = "spawn-sector-asteroid";
    public string Name => ActionName;
    public string GetKey() => Name;

    public async Task<ScriptActionResult> ExecuteAsync(ScriptContext context)
    {
        var logger = context.ServiceProvider.CreateLogger<SpawnSectorAsteroid>();
        var areaScanService = context.ServiceProvider.GetRequiredService<IAreaScanService>();
        var taskQueueService = context.ServiceProvider.GetRequiredService<ITaskQueueService>();
        var orleans = context.ServiceProvider.GetOrleans();
        var asteroidManagerGrain = orleans.GetAsteroidManagerGrain();

        if (!context.Properties.TryGetValue("File", out var file) || file == null)
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
        
        var asteroidId = await asteroidManagerGrain.SpawnAsteroid(
            5,
            $"{file}",
            context.Sector,
            2
        );

        await asteroidManagerGrain.ForcePublish(asteroidId);

        return ScriptActionResult.Successful();
    }
}