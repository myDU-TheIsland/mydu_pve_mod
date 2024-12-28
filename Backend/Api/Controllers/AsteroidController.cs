using System;
using System.Linq;
using System.Threading.Tasks;
using Backend.Scenegraph;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Common.Interfaces;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Data;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Interfaces;
using Mod.DynamicEncounters.Features.TaskQueue.Interfaces;
using Mod.DynamicEncounters.Helpers;
using Newtonsoft.Json;
using NQ;
using NQ.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace Mod.DynamicEncounters.Api.Controllers;

[Route("asteroid")]
public class AsteroidController : Controller
{
    public class SpawnRequest
    {
        public ulong? ConstructId { get; set; }
        public string File { get; set; }
        public Vec3? Position { get; set; }
    }

    [HttpGet]
    [Route("waypoints")]
    public async Task<IActionResult> GetAsteroidWaypoints([FromBody] AsteroidWaypointRequest request)
    {
        var provider = ModBase.ServiceProvider;
        var repository = provider.GetRequiredService<IConstructRepository>();
        var travelRouteService = provider.GetRequiredService<ITravelRouteService>();

        var asteroids = await repository.FindAsteroids();
        var route = travelRouteService.Solve(
            new WaypointItem
            {
                Position = request.FromPosition,
            },
            asteroids.Select(x => new WaypointItem
            {
                Position = x.Position,
                Name = x.Name
            }));
        
        return Ok(route);
    }

    [HttpDelete]
    [Route("")]
    public async Task<IActionResult> DeleteAsteroidAround([FromBody] DeleteAsteroidsAroundRequest request)
    {
        var provider = ModBase.ServiceProvider;
        var areaScanService = provider.GetRequiredService<IAreaScanService>();
        var sceneGraph = provider.GetRequiredService<IScenegraph>();
        var taskQueueService = provider.GetRequiredService<ITaskQueueService>();

        var pos = await sceneGraph.GetConstructCenterWorldPosition(request.ConstructId);

        var result = await areaScanService.ScanForAsteroids(pos, request.Radius);

        foreach (var contact in result)
        {
            await taskQueueService.EnqueueScript(new ScriptActionItem
            {
                Type = "delete-asteroid",
                ConstructId = contact.ConstructId
            }, DateTime.UtcNow);
        }

        return Ok();
    }

    [SwaggerOperation("Spawns an Asteroid at a position or around a construct")]
    [HttpPost]
    [Route("spawn")]
    public async Task<IActionResult> SpawnAsync([FromBody] SpawnRequest request)
    {
        var provider = ModBase.ServiceProvider;
        var orleans = provider.GetOrleans();
        var sceneGraph = provider.GetRequiredService<IScenegraph>();

        if (!request.Position.HasValue && request.ConstructId.HasValue)
        {
            var constructPos = await sceneGraph.GetConstructCenterWorldPosition(request.ConstructId.Value);

            var random = provider.GetRequiredService<IRandomProvider>().GetRandom();

            var offset = random.RandomDirectionVec3() * 200;
            var pos = offset + constructPos;

            request.Position = pos;
        }
        else if (!request.Position.HasValue)
        {
            return BadRequest();
        }

        var asteroidManagerGrain = orleans.GetAsteroidManagerGrain();
        var asteroidId = await asteroidManagerGrain.SpawnAsteroid(
            1, request.File, request.Position.Value, 2
        );

        await asteroidManagerGrain.ForcePublish(asteroidId);

        return Ok(asteroidId);
    }

    public class DeleteAsteroidsAroundRequest
    {
        [JsonProperty] public ulong ConstructId { get; set; }
        [JsonProperty] public double Radius { get; set; } = DistanceHelpers.OneSuInMeters * 5;
    }

    public class AsteroidWaypointRequest
    {
        [JsonProperty] public Vec3 FromPosition { get; set; }
    }
}