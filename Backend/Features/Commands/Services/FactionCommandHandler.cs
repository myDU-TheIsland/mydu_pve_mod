using System.Threading.Tasks;
using BotLib.Generated;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Commands.Interfaces;
using Mod.DynamicEncounters.Features.Faction.Interfaces;
using NQ;

namespace Mod.DynamicEncounters.Features.Commands.Services;

public class FactionCommandHandler : IFactionCommandHandler
{
    public async Task HandleCommand(ulong instigatorPlayerId, string command)
    {
        var provider = ModBase.ServiceProvider;
        var factionRepRepository = provider.GetRequiredService<IFactionReputationRepository>();

        var report = await factionRepRepository.GetPlayerFactionReputationAsync(instigatorPlayerId);

        foreach (var item in report)
        {
            await ModBase.Bot.Req.ChatMessageSend(
                new MessageContent
                {
                    channel = new MessageChannel
                    {
                        channel = MessageChannelType.PRIVATE,
                        targetId = instigatorPlayerId
                    },
                    message = $"{item.FactionName}: {item.Reputation}"
                }
            );
            await Task.Delay(100);
        }
    }
}