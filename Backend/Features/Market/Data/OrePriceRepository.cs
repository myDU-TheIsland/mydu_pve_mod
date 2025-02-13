﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Database.Interfaces;
using Mod.DynamicEncounters.Features.Common.Data;
using Mod.DynamicEncounters.Features.Market.Interfaces;

namespace Mod.DynamicEncounters.Features.Market.Data;

public class OrePriceRepository(IServiceProvider provider) : IOrePriceRepository
{
    private readonly IPostgresConnectionFactory _factory = provider.GetRequiredService<IPostgresConnectionFactory>();

    public async Task<Dictionary<string, Quanta>> GetOrePrices()
    {
        using var db = _factory.Create();
        db.Open();

        var rows = await db.QueryAsync<DbRow>(
            """
            SELECT 
            	name,
            	CEIL(price / qt) as price
            FROM (
            	SELECT
            		I.name,
            		AVG(ABS(original_buy_quantity)) qt,
            		TRUNC(AVG(price * ABS(original_buy_quantity))) AS price
            	FROM public.market_order MO
            	INNER JOIN item_definition I ON (I.id = MO.item_type_id)
            	WHERE 
            		parent_id IN (1240631464, 1240631465, 1240631466, 1240631467, 1240631468)
            		AND update_date >= CURRENT_DATE - INTERVAL '7 days'
            		AND update_date < CURRENT_DATE + INTERVAL '1 day'
            		AND completion_date IS NOT NULL
            	GROUP BY I.name
            ) as Base;
            """
        );

        return MapToModel(rows);
    }

    private static Dictionary<string, Quanta> MapToModel(IEnumerable<DbRow> rows)
    {
        var orePrices = GetDefaultOrePrices();
        
        foreach (var row in rows)
        {
            var marketPrice = (long)row.price;
            
            var quanta = new Quanta(marketPrice);
            if (!orePrices.TryAdd(row.name, quanta))
            {
                var orePrice = orePrices[row.name].Value;
                var clampedPrice = Math.Clamp(marketPrice, orePrice, orePrice * 4);
                quanta = new Quanta(clampedPrice);
                
                orePrices[row.name] = quanta;
            }
        }

        return orePrices;
    }

    private static Dictionary<string, Quanta> GetDefaultOrePrices()
    {
        return new Dictionary<string, Quanta>
        {
            { "AluminiumOre", 2500 },
            { "CarbonOre", 2500 },
            { "IronOre", 2500 },
            { "SiliconOre", 2500 },
            { "CalciumOre", 20000 },
            { "ChromiumOre", 20000 },
            { "CopperOre", 20000 },
            { "SodiumOre", 20000 },
            { "LithiumOre", 60000 },
            { "NickelOre", 46000 },
            { "SilverOre", 51000 },
            { "SulfurOre", 11000 },
            { "CobaltOre", 140000 },
            { "FluorineOre", 9500 },
            { "GoldOre", 190000 },
            { "ScandiumOre", 135000 },
            { "ManganeseOre", 135000 },
            { "NiobiumOre", 88800 },
            { "TitaniumOre", 1800000 },
            { "VanadiumOre", 700000 },
            { "ThoramineOre", 20000000 },
        };
    }

    private readonly struct DbRow
    {
        public string name { get; init; }
        public double price { get; init; }
    }
}