using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Common.Helpers;
using Mod.DynamicEncounters.Features.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Services;
using Mod.DynamicEncounters.Helpers;
using Newtonsoft.Json;
using NQutils.Exceptions;

namespace Mod.DynamicEncounters.Features.Scripts.Actions;

[ScriptActionName(ActionName)]
public class SendDiscordMessageAction : IScriptAction
{
    public const string ActionName = "send-discord-message";
    
    public string Name => ActionName;

    public async Task<ScriptActionResult> ExecuteAsync(ScriptContext context)
    {
        await using var scope = ModBase.ServiceProvider.CreateAsyncScope();
        
        var logger = scope.ServiceProvider.CreateLogger<SendDiscordMessageAction>();
        var featureService = scope.ServiceProvider.GetRequiredService<IFeatureReaderService>();

        var properties = context.GetProperties<Properties>();

        var channelIdString = await featureService.GetStringValueAsync($"DiscordChannel_{properties.Channel}", string.Empty);

        if (string.IsNullOrEmpty(channelIdString))
        {
            return ScriptActionResult.Failed($"Channel ID '{properties.Channel}' not found");
        }

        if (!ulong.TryParse(channelIdString, out var channelId))
        {
            return ScriptActionResult.Failed($"Channel ID '{channelIdString}' is not a ulong");
        }
        
        var token = EnvironmentVariableHelper.GetEnvironmentVarOrDefault("DISCORD_BOT_TOKEN", "");
        
        if (string.IsNullOrEmpty(token))
        {
            logger.LogError("Discord Token Invalid");
            return ScriptActionResult.Failed("Discord Token Invalid");
        }
        
        try
        {
            var client = new DiscordRestClient();
            await client.LoginAsync(TokenType.Bot, token);

            var channel = await client.GetChannelAsync(channelId);
            if (channel is ITextChannel textChannel)
            {
                await textChannel.SendMessageAsync(text: properties.Message);
            }
        }
        catch (BusinessException e)
        {
            logger.LogError(e, "Failed to Send Discord Message.");
        }
        
        return ScriptActionResult.Successful();
    }

    public string GetKey() => Name;

    public class Properties
    {
        [JsonProperty] public string Channel { get; set; }
        [JsonProperty] public string Message { get; set; }
    }
}