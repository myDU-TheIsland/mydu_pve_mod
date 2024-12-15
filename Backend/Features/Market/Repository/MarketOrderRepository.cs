using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Common.Helpers;
using Mod.DynamicEncounters.Database.Interfaces;
using Mod.DynamicEncounters.Features.Interfaces;
using Mod.DynamicEncounters.Features.Market.Interfaces;

namespace Mod.DynamicEncounters.Features.Market.Repository;

public class MarketOrderRepository(IServiceProvider provider) : IMarketOrderRepository
{
    private readonly IPostgresConnectionFactory _factory = provider.GetRequiredService<IPostgresConnectionFactory>();
    private readonly IFeatureReaderService _feature = provider.GetRequiredService<IFeatureReaderService>();

    public async Task<double> GetAveragePriceOfItemAsync(ulong itemTypeId)
    {
        using var db = _factory.Create();
        db.Open();

        // TODO replace with polymorphism later
        var useEnhancedQuery = await _feature.GetBoolValueAsync(
            "MarketOrderRepository_UseEnhancedQuery",
            false
        );
        if (useEnhancedQuery)
        {
            var tableName = await _feature.GetStringValueAsync(
                "MarketOrderRepository_TableName",
                "mod_eco_market_event");

            return await db.ExecuteScalarAsync<double>(
                $"""
                SELECT 
                	TRUNC(AVG(E.price))
                FROM public.{tableName} E
                WHERE E.event_name IN('item_purchased') AND
                	E.create_date > NOW() - INTERVAL '14 DAYS' AND
                	E.item_type_id = @item_type_id
                """,
                new
                {
                    item_type_id = (long)itemTypeId
                }
            );
        }

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