using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Database.Interfaces;
using Mod.DynamicEncounters.Features.Market.Interfaces;

namespace Mod.DynamicEncounters.Features.Market.Repository;

public class MarketOrderRepository(IServiceProvider provider) : IMarketOrderRepository
{
    private readonly IPostgresConnectionFactory _factory = provider.GetRequiredService<IPostgresConnectionFactory>();
    
    public async Task<double> GetAveragePriceOfItemAsync(ulong itemTypeId)
    {
        using var db = _factory.Create();
        db.Open();

        return await db.ExecuteScalarAsync<double>(
            """
            SELECT AVG(price)
            	FROM public.market_order
            WHERE item_type_id = @item_type_id
            """,
            new
            {
                item_type_id = (long)itemTypeId
            }
        );
    }
}