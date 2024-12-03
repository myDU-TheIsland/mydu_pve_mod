using System.Collections.Generic;
using Mod.DynamicEncounters.Features.Common.Data;
using Mod.DynamicEncounters.Features.Market.Interfaces;

namespace Mod.DynamicEncounters.Features.Market.Data;

public class OrePriceReader : IOrePriceReader
{
    public Dictionary<string, Quanta> GetOrePrices()
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
            { "ThoramineOre", 5000000 },
        };
    }
}