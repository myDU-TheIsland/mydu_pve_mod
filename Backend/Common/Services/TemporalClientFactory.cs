using System.Threading.Tasks;
using Mod.DynamicEncounters.Common.Interfaces;
using Mod.DynamicEncounters.Temporal.Services;
using Temporalio.Client;

namespace Mod.DynamicEncounters.Common.Services;

public class TemporalClientFactory : ITemporalClientFactory
{
    public async Task<ITemporalClient> CreateAsync()
    {
        var connectOptions = TemporalConfig.CreateClientConnectOptions(ModBase.ServiceProvider);
        return await TemporalClient.ConnectAsync(connectOptions);
    }

    public static async Task<ITemporalClient> GetClientAsync()
    {
        return await new TemporalClientFactory().CreateAsync();
    }
}