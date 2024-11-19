using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Scenegraph;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Features.Commands.Interfaces;
using Mod.DynamicEncounters.Features.Common.Services;
using Mod.DynamicEncounters.Features.Interfaces;
using Mod.DynamicEncounters.Helpers;
using NQ;
using NQ.Interfaces;

namespace Mod.DynamicEncounters.Features.Commands.Services;

public class WarpAnchorCommandHandler : IWarpAnchorCommandHandler
{
    private readonly ILogger<NpcKillsCommandHandler> _logger =
        ModBase.ServiceProvider.CreateLogger<NpcKillsCommandHandler>();

    private readonly IFeatureReaderService _featureReaderService =
        ModBase.ServiceProvider.GetRequiredService<IFeatureReaderService>();

    private readonly IModManagerGrain _modManagerGrain = ModBase.ServiceProvider.GetOrleans().GetModManagerGrain();
    private readonly IScenegraph _sceneGraph = ModBase.ServiceProvider.GetRequiredService<IScenegraph>();

    private readonly IPlayerAlertService _playerAlertService =
        ModBase.ServiceProvider.GetRequiredService<IPlayerAlertService>();

    public async Task HandleCommand(ulong instigatorPlayerId, string command)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            { nameof(instigatorPlayerId), instigatorPlayerId },
            { nameof(command), command }
        });

        if (command == "@wac")
        {
            await HandleCreateWarpAnchorCommand(instigatorPlayerId, command);
        }
    }

    private async Task HandleCreateWarpAnchorCommand(ulong instigatorPlayerId, string command)
    {
        var warpAnchorModActionId = await _featureReaderService.GetIntValueAsync("WarpAnchorActionId", 3);
        var warpAnchorModName =
            await _featureReaderService.GetStringValueAsync("WarpAnchorModName", "Mod.DynamicEncounters");

        var (_, world) = await _sceneGraph.GetPlayerWorldPosition(instigatorPlayerId);

        if (world.constructId <= 0)
        {
            await _playerAlertService.SendErrorAlert(instigatorPlayerId, "You need to be on a construct");
            return;
        }

        var constructGrain = ModBase.ServiceProvider.GetOrleans().GetConstructGrain(world.constructId);
        var pilot = await constructGrain.GetPilot();

        if (pilot != instigatorPlayerId)
        {
            await _playerAlertService.SendErrorAlert(instigatorPlayerId, "You need to pilot the construct");
            return;
        }

        await _modManagerGrain.TriggerModAction(
            instigatorPlayerId,
            new ModAction
            {
                modName = warpAnchorModName,
                actionId = (ulong)warpAnchorModActionId,
                constructId = world.constructId
            }
        );
    }
}