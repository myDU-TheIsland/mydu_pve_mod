using System.Collections.Generic;
using Mod.DynamicEncounters.Features.Common.Data;

namespace Mod.DynamicEncounters.Features.Market.Interfaces;

public interface IOrePriceReader
{
    Dictionary<string, Quanta> GetOrePrices();
}