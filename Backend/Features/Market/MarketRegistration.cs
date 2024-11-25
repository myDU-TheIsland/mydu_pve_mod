using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Market.Interfaces;
using Mod.DynamicEncounters.Features.Market.Repository;

namespace Mod.DynamicEncounters.Features.Market;

public static class MarketRegistration
{
    public static void RegisterMarketServices(this IServiceCollection services)
    {
        services.AddSingleton<IMarketOrderRepository, MarketOrderRepository>();
    }
}