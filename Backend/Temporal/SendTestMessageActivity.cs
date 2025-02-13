using System;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Common.Helpers;
using Temporalio.Activities;
using Temporalio.Workflows;

namespace Mod.DynamicEncounters.Temporal;

public class SendTestMessageActivity
{
    [Activity]
    public async Task SendDiscordMessage(string message)
    {
        var token = EnvironmentVariableHelper.GetEnvironmentVarOrDefault("DISCORD_BOT_TOKEN", "");

        if (string.IsNullOrEmpty(token))
        {
            Workflow.Logger.LogError("Discord Token Invalid");
            return;
        }
        
        try
        {
            var client = new DiscordRestClient();
            await client.LoginAsync(TokenType.Bot, token);

            var channel = await client.GetChannelAsync(1337913634424225862);
            if (channel is ITextChannel textChannel)
            {
                await textChannel.SendMessageAsync(message);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}