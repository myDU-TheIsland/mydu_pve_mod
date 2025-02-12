using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Scenegraph;
using BotLib.Generated;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Commands.Interfaces;
using Mod.DynamicEncounters.Features.Common.Services;
using Mod.DynamicEncounters.Helpers;
using Mod.DynamicEncounters.Vector.Helpers;
using NQ;
using NQ.Interfaces;
using NQ.RDMS;
using NQutils.Def;

namespace Mod.DynamicEncounters.Features.Commands.Services;

public class IndyCommandHandler : IIndyCommandHandler
{
    private readonly string[] _help = new[]
    {
        "@indy - lists all machines and their tags",
        "@indy filter running - filters indy machines to display based on the tag. Tags are (running, stopped, stuck, jammed and also the indy ID)",
        "@indy restart stuck - restarts all indy machines with the stuck tag",
        "@indy restart 12345 - restarts one indy machine with the id 12345",
        "@indy stop stuck - stops all machines with tag stuck. Also works with id"
    };
    
    public async Task HandleCommand(ulong instigatorPlayerId, string command)
    {
        var provider = ModBase.ServiceProvider;
        var playerAlertService = provider.GetRequiredService<IPlayerAlertService>();
        var bank = provider.GetGameplayBank();
        var orleans = provider.GetOrleans();
        var sceneGraph = provider.GetRequiredService<IScenegraph>();
        
        var (local, _) = await sceneGraph.GetPlayerWorldPosition(instigatorPlayerId);

        if (local.constructId <= 0)
        {
            await playerAlertService.SendErrorAlert(instigatorPlayerId, "You need to stand/board the construct you want to use these commands");
            return;
        }
        
        var rdmsRightGrain = orleans.GetRDMSRightGrain(instigatorPlayerId);
        var rights = await rdmsRightGrain.GetRightsForPlayerOnAsset(instigatorPlayerId, new AssetId
        {
            construct = local.constructId,
            type = AssetType.Construct
        });
        
        if(!rights.HasRight(Right.ConstructBuild)) return;
        
        var commandPieces = command.Split(" ");
        var queuePieces = new Queue<string>(commandPieces);
        queuePieces.Dequeue();
        var subcommand = string.Empty;
        var tagValue = string.Empty;
        if (queuePieces.Count > 0)
        {
            var subCommandFound = queuePieces.Dequeue();
            if (subCommandFound == "help")
            {
                foreach (var line in _help)
                {
                    await ModBase.Bot.Req.ChatMessageSend(
                        new MessageContent
                        {
                            channel = new MessageChannel
                            {
                                channel = MessageChannelType.PRIVATE,
                                targetId = instigatorPlayerId
                            },
                            message = line
                        }
                    );
                }
                return;
                    
            }
            
            if (queuePieces.Count > 0)
            {
                subcommand = subCommandFound;
                tagValue = queuePieces.Dequeue().ToUpper();
            }
        }

        var constructElementsGrain = orleans.GetConstructElementsGrain(local.constructId);
        var industryUnits = await constructElementsGrain.GetElementsOfType<IndustryUnit>();
        
        foreach (var elementId in industryUnits)
        {
            var grain = orleans.GetIndustryUnitGrain(elementId);
            var status = await grain.Status();
            var element = await constructElementsGrain.GetElement(elementId);
            var def = bank.GetDefinition(element.elementType);

            IndustryState[] jammedStates =
            [
                IndustryState.JAMMED_OUTPUT_FULL, IndustryState.JAMMED_NO_OUTPUT_CONTAINER,
                IndustryState.JAMMED_MISSING_INGREDIENT, IndustryState.JAMMED_MISSING_SCHEMATIC
            ];
            
            var isJammed = jammedStates.Contains(status.state);
            var isPending = status.state == IndustryState.PENDING;
            var timeDiff = status.end.networkTime - DateTime.UtcNow.ToNQTimePoint().networkTime;
            var isNegativeTime = timeDiff < 0;
            var isStuck = TimeSpan.FromSeconds(timeDiff) < TimeSpan.FromSeconds(2);

            var tags = new HashSet<string> { def!.Name, $"{elementId}" };

            if (isJammed) tags.Add("JAMMED");
            if (!isPending && !isJammed && isNegativeTime && status.state != IndustryState.STOPPED) tags.Add("STUCK");
            if (!isPending && !isJammed && isStuck && status.state != IndustryState.STOPPED) tags.Add("STUCK");
            if (status.state == IndustryState.RUNNING) tags.Add("RUNNING");
            if (status.state == IndustryState.STOPPED) tags.Add("STOPPED");
            if (status.state == IndustryState.PENDING) tags.Add("PENDING");
            
            var elementPos = await sceneGraph.ResolveWorldLocation(new RelativeLocation
            {
                constructId = local.constructId,
                position = element.position,
                rotation = element.rotation
            });
            
            var baseMessage = string.Join(", ", tags);
            baseMessage += $" >> {elementPos.position.Vec3ToPosition()}";
            
            if (subcommand == "restart" && tags.Contains(tagValue))
            {
                await grain.StopHard(false);
                await grain.Start(instigatorPlayerId, new IndustryStart
                {
                    elementId = elementId,
                    numBatches = status.batchesRemaining,
                    maintainProductAmount = status.maintainProductAmount
                });

                baseMessage = $"RESTARTED >> {baseMessage}";
                
                await ModBase.Bot.Req.ChatMessageSend(
                    new MessageContent
                    {
                        channel = new MessageChannel
                        {
                            channel = MessageChannelType.PRIVATE,
                            targetId = instigatorPlayerId
                        },
                        message = baseMessage
                    }
                );
            }
            else if (subcommand == "stop" && tags.Contains(tagValue))
            {
                await grain.StopHard(false);
                baseMessage = $"STOPPED >> {baseMessage}";
                
                await ModBase.Bot.Req.ChatMessageSend(
                    new MessageContent
                    {
                        channel = new MessageChannel
                        {
                            channel = MessageChannelType.PRIVATE,
                            targetId = instigatorPlayerId
                        },
                        message = baseMessage
                    }
                );
            }       
            else if (subcommand == "filter" && tags.Contains(tagValue))
            {
                await ModBase.Bot.Req.ChatMessageSend(
                    new MessageContent
                    {
                        channel = new MessageChannel
                        {
                            channel = MessageChannelType.PRIVATE,
                            targetId = instigatorPlayerId
                        },
                        message = baseMessage
                    }
                );
            }

            if (string.IsNullOrEmpty(subcommand))
            {
                await ModBase.Bot.Req.ChatMessageSend(
                    new MessageContent
                    {
                        channel = new MessageChannel
                        {
                            channel = MessageChannelType.PRIVATE,
                            targetId = instigatorPlayerId
                        },
                        message = baseMessage
                    }
                );
            }
        }
    }
}