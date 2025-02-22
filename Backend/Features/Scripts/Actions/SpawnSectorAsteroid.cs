using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Common.Interfaces;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Interfaces;
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
        var provider = ModBase.ServiceProvider;
        var logger = provider.CreateLogger<SpawnSectorAsteroid>();
        var areaScanService = provider.GetRequiredService<IAreaScanService>();
        var random = provider.GetRequiredService<IRandomProvider>().GetRandom();
        var orleans = provider.GetOrleans();
        var asteroidManagerGrain = orleans.GetAsteroidManagerGrain();
        var featureService = provider.GetRequiredService<IFeatureReaderService>();

        var sectorAsteroidDeleteHours = await featureService
            .GetIntValueAsync("SectorAsteroidDeleteHours", 4);

        if (!actionItem.Properties.TryGetValue("File", out var file) || file == null)
        {
            logger.LogError("File not found on script properties");

            return ScriptActionResult.Failed();
        }

        var contacts = await areaScanService.ScanForAsteroids(context.Sector, 20 * DistanceHelpers.OneSuInMeters);
        foreach (var contact in contacts)
        {
            await Script.DeleteAsteroid(contact.ConstructId).EnqueueRunAsync();
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

        var constructService = ModBase.ServiceProvider.GetRequiredService<IConstructService>();
        var info = await constructService.GetConstructInfoAsync(asteroidId);

        await Script.DeleteAsteroid(asteroidId)
            .WithTag("sector")
            .EnqueueRunAsync(startAt: DateTime.UtcNow + TimeSpan.FromHours(sectorAsteroidDeleteHours));

        if (info.Info != null)
        {
            var name = info.Info.rData.name
                .Replace("A-", "T-");

            await constructService.RenameConstruct(asteroidId, name);
        }

        await asteroidManagerGrain.ForcePublish(asteroidId);

        return ScriptActionResult.Successful();
    }
}