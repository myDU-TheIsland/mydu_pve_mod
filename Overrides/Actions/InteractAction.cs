using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Overrides.Common;
using Newtonsoft.Json;
using NQ;
using NQ.Interfaces;
using Orleans;

namespace Mod.DynamicEncounters.Overrides.Actions;

public class InteractAction(IServiceProvider provider) : IModActionHandler
{
    public async Task HandleAction(ulong playerId, ModAction action)
    {
        var logger = provider.GetRequiredService<ILoggerFactory>()
            .CreateLogger<InteractAction>();

        var orleans = provider.GetRequiredService<IClusterClient>();
        var spsGrain = orleans.GetSPSGrain(playerId);
        var isInVr = await spsGrain.CurrentSession() != 0L;

        if (isInVr)
        {
            await Notifications.ErrorNotification(provider, playerId, "Cannot use this in VR");
        }

        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        using var httpClient = httpClientFactory.CreateClient();

        var baseUrl = Config.GetPveModBaseUrl();
        var questInteractUrl = Path.Combine(baseUrl, "quest/interact");

        var payload = JsonConvert.DeserializeObject<InteractPayload>(action.payload);
        if (payload.ConstructId == 0)
        {
            payload.ConstructId = action.constructId;
        }
        
        await httpClient.PostAsync(
            questInteractUrl,
            new StringContent(
                JsonConvert.SerializeObject(new
                {
                    playerId,
                    constructId = payload.ConstructId,
                    elementId = action.elementId == 0 ? (ulong?)null : action.elementId
                }),
                Encoding.UTF8,
                "application/json"
            )
        );
    }

    public class InteractPayload
    {
        [JsonProperty("constructId")] public ulong ConstructId { get; set; }
    }
}