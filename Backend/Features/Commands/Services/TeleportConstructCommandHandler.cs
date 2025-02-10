using System.Threading.Tasks;
using Backend.Scenegraph;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Commands.Interfaces;
using Mod.DynamicEncounters.Features.Common.Services;
using Mod.DynamicEncounters.Helpers;
using Mod.DynamicEncounters.Vector.Helpers;
using NQ;
using NQ.Interfaces;

namespace Mod.DynamicEncounters.Features.Commands.Services;

public class TeleportConstructCommandHandler : ITeleportConstructCommandHandler
{
    public async Task Teleport(ulong instigatorPlayerId, string command)
    {
        var provider = ModBase.ServiceProvider;
        var orleans = provider.GetOrleans();
        var sceneGraph = provider.GetRequiredService<IScenegraph>();

        var playerGrain = orleans.GetPlayerGrain(instigatorPlayerId);
        if (!await playerGrain.IsAdmin())
        {
            return;
        }
        
        var commandPieces = command.Split(" ");
        var positionString = commandPieces[1];
        var position = positionString.PositionToVec3();

        var tpLocation = new RelativeLocation
        {
            position = position,
            constructId = 0,
            rotation = Quat.Identity
        };
        
        var (local, _) = await sceneGraph.GetPlayerWorldPosition(instigatorPlayerId);

        if (local.constructId <= 0)
        {
            await playerGrain.TeleportPlayer(tpLocation);
            return;
        }

        var constructGrain = orleans.GetConstructGrain(local.constructId);
        await constructGrain.TeleportConstruct(tpLocation);
    }
}