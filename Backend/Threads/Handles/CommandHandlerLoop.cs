﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Features.Commands.Interfaces;
using Mod.DynamicEncounters.Features.Party.Interfaces;
using Mod.DynamicEncounters.Helpers;

namespace Mod.DynamicEncounters.Threads.Handles;

public class CommandHandlerLoop(IThreadManager threadManager, CancellationToken token)
    : ThreadHandle(ThreadId.CommandHandler, threadManager, token)
{
    private readonly ILogger<CommandHandlerLoop> _logger = ModBase.ServiceProvider.CreateLogger<CommandHandlerLoop>();
    private readonly IPendingCommandRepository _pendingCommandRepository =
        ModBase.ServiceProvider.GetRequiredService<IPendingCommandRepository>();

    private DateTime _refDate = DateTime.UtcNow;
    
    public override async Task Tick()
    {
        var now = DateTime.UtcNow;
        var commandItems = await _pendingCommandRepository.QueryAsync(_refDate);
        _refDate = now;

        foreach (var commandItem in commandItems)
        {
            using var commandScope = _logger.BeginScope(new Dictionary<string, object>
            {
                { nameof(commandItem.PlayerId), commandItem.PlayerId },
                { nameof(commandItem.Message), commandItem.Message },
            });
            
            try
            {
                if (commandItem.Message.StartsWith("@g"))
                {
                    var playerPartyCommandHandler =
                        ModBase.ServiceProvider.GetRequiredService<IPlayerPartyCommandHandler>();
                    await playerPartyCommandHandler.HandleCommand(commandItem.PlayerId, commandItem.Message);
                }

                if (commandItem.Message.StartsWith("@kills npc"))
                {
                    var npcKillsCommandHandler =
                        ModBase.ServiceProvider.GetRequiredService<INpcKillsCommandHandler>();
                    await npcKillsCommandHandler.HandleCommand(commandItem.PlayerId, commandItem.Message);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to handle command");
            }
        }
        
        ReportHeartbeat();
        Thread.Sleep(150);
    }
}