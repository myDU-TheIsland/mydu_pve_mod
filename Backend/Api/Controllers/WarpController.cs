using System;
using System.Threading.Tasks;
using Backend.Database;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Database.Interfaces;
using Mod.DynamicEncounters.Features.Common.Data;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.ExtendedProperties.Extensions;
using Mod.DynamicEncounters.Features.ExtendedProperties.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.TaskQueue.Interfaces;
using Mod.DynamicEncounters.Helpers;
using NQ;
using NQutils.Sql;

namespace Mod.DynamicEncounters.Api.Controllers;

[Route("warp")]
public class WarpController : Controller
{
    [HttpPost]
    [Route("anchor")]
    public async Task<IActionResult> CreateWarpAnchor([FromBody] WarpAnchorRequest request)
    {
        if (request.PlayerId == default)
        {
            return BadRequest();
        }

        var provider = ModBase.ServiceProvider;
        var spawner = provider.GetRequiredService<IBlueprintSpawnerService>();
        var taskQueueService = provider.GetRequiredService<ITaskQueueService>();
        var traitRepository = provider.GetRequiredService<ITraitRepository>();
        var elementTraitMap = (await traitRepository.GetElementTraits(request.ElementTypeName)).Map();

        var blueprintFileName = "Warp_Signature.json";
        if (elementTraitMap.TryGetValue("supercruise", out var trait))
        {
            if (trait.Properties.TryGetValue("blueprintFileName", out var prop))
            {
                blueprintFileName = prop.Prop.ValueAs<string>();
            }
        }

        var constructId = await spawner.SpawnAsync(
            new SpawnArgs
            {
                Folder = "pve",
                File = blueprintFileName,
                Position = request.Position,
                IsUntargetable = true,
                OwnerEntityId = new EntityId { playerId = request.PlayerId },
                Name = "[!] Warp Signature"
            }
        );

        var connectionFactory = provider.GetRequiredService<IPostgresConnectionFactory>();
        using var db = connectionFactory.Create();

        // Make sure the beacon is active by setting all elements to have been created 3 days in the past *shrugs*
        await db.ExecuteAsync(
            """
            UPDATE public.element SET created_at = NOW() - INTERVAL '3 DAYS' WHERE construct_id = @constructId
            """,
            new
            {
                constructId = (long)constructId
            }
        );

        await taskQueueService.EnqueueScript(
            new ScriptActionItem
            {
                Type = "delete",
                ConstructId = constructId
            },
            DateTime.UtcNow + TimeSpan.FromMinutes(1)
        );

        return Ok(constructId);
    }

    [HttpPost]
    [Route("anchor/v2")]
    public async Task<IActionResult> CreateWarpAnchorV2([FromBody] WarpAnchorRequestV2 request)
    {
        if (request.PlayerId == default)
        {
            return BadRequest();
        }

        var provider = ModBase.ServiceProvider;
        var sql = provider.GetRequiredService<ISql>();
        var spawner = provider.GetRequiredService<IBlueprintSpawnerService>();
        var taskQueueService = provider.GetRequiredService<ITaskQueueService>();
        var traitRepository = provider.GetRequiredService<ITraitRepository>();
        var elementTraitMap = (await traitRepository.GetElementTraits(request.ElementTypeName)).Map();

        if (!elementTraitMap.TryGetValue("supercruise", out var trait))
        {
            return BadRequest("Supercruise trait not found");
        }

        trait.TryGetPropertyValue("blueprintFileName", out var blueprintFileName, "Warp_Signature.json");
        trait.TryGetPropertyValue("maxRange", out var maxRange, DistanceHelpers.OneSuInMeters * 100);

        var delta = request.TargetPosition - request.FromPosition;
        var distance = delta.Size();
        var direction = delta.NormalizeSafe();
        var beaconPosition = request.TargetPosition;

        if (distance > maxRange)
        {
            beaconPosition = direction * maxRange + request.FromPosition;
        }

        const string warpDestinationConstructName = "[!] Warp Signature";

        var constructId = await spawner.SpawnAsync(
            new SpawnArgs
            {
                Folder = "pve",
                File = blueprintFileName,
                Position = beaconPosition,
                IsUntargetable = true,
                OwnerEntityId = new EntityId { playerId = request.PlayerId },
                Name = warpDestinationConstructName
            }
        );

        var connectionFactory = provider.GetRequiredService<IPostgresConnectionFactory>();
        using var db = connectionFactory.Create();

        // Make sure the beacon is active by setting all elements to have been created 3 days in the past *shrugs*
        await db.ExecuteAsync(
            """
            UPDATE public.element SET created_at = NOW() - INTERVAL '3 DAYS' WHERE construct_id = @constructId
            """,
            new
            {
                constructId = (long)constructId
            }
        );

        await taskQueueService.EnqueueScript(
            new ScriptActionItem
            {
                Type = "delete",
                ConstructId = constructId
            },
            DateTime.UtcNow + TimeSpan.FromMinutes(1)
        );

        await sql.UpdatePlayerProperty_Generic(
            request.PlayerId,
            "warpDestinationConstructName",
            new PropertyValue(warpDestinationConstructName)
        );

        await sql.UpdatePlayerProperty_Generic(
            request.PlayerId,
            "warpDestinationConstructId",
            new PropertyValue(constructId)
        );

        var beaconPosString = string.Join(
            "",
            "::pos{0,0,",
            $"{beaconPosition.x:F}",
            ",",
            $"{beaconPosition.y:F}",
            ",",
            $"{beaconPosition.z:F}",
            "}"
        );
        
        await sql.UpdatePlayerProperty_Generic(
            request.PlayerId,
            "warpDestinationWorldPosition",
            new PropertyValue(beaconPosString)
        );

        return Ok(
            new WarpAnchorResponse(
                constructId,
                warpDestinationConstructName,
                beaconPosition,
                beaconPosString
            )
        );
    }

    public class WarpAnchorRequest
    {
        public ulong PlayerId { get; set; }
        public Vec3 Position { get; set; }
        public string ElementTypeName { get; set; } = "WarpDrive";
    }

    public class WarpAnchorRequestV2
    {
        public ulong PlayerId { get; set; }
        public Vec3 FromPosition { get; set; }
        public Vec3 TargetPosition { get; set; }
        public string ElementTypeName { get; set; } = "WarpDrive";
    }

    public class WarpAnchorResponse(ulong constructId, string constructName, Vec3 position, string positionString)
    {
        public ulong ConstructId { get; set; } = constructId;
        public string ConstructName { get; set; } = constructName;
        public Vec3 Position { get; set; } = position;
        public string PositionString { get; set; } = positionString;
    }
}