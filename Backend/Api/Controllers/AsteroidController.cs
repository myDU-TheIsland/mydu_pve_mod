using System.Threading.Tasks;
using Backend.Scenegraph;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Common.Interfaces;
using Mod.DynamicEncounters.Helpers;
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
}