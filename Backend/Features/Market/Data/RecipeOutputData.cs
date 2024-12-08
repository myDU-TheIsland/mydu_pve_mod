using Mod.DynamicEncounters.Features.Common.Data;
using Mod.DynamicEncounters.Features.Market.Interfaces;

namespace Mod.DynamicEncounters.Features.Market.Data;

public readonly struct RecipeOutputData
{
    public IItemQuantity Quantity { get; init; }
    public Quanta Quanta { get; init; }

    public double GetUnitPrice() => Quanta.Value / Quantity.GetReadableValue();
}