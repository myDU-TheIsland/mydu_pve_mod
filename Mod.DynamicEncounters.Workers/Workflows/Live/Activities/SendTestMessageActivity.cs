using Discord;
using Discord.Rest;
using Temporalio.Activities;

namespace Mod.DynamicEncounters.Workers.Workflows.Live.Activities;

public class SendTestMessageActivity
{
    [Activity]
    public async Task SendDiscordMessage(string message)
    {
        var token = EnvironmentVariableHelper.GetEnvironmentVarOrDefault("DISCORD_BOT_TOKEN", "");

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