using Mod.DynamicEncounters.Features.Market.Interfaces;
using Mod.DynamicEncounters.Features.Market.Services;

namespace Mod.DynamicEncounters.Features.Market.Data;

public class MaterialQuantity(long value) : IItemQuantity
{
    public long Value { get; } = value;
    
    public long GetReadableValue()
    {
        return Value >> 24;
    }

    public override string ToString()
    {
        return $"{Value >> 24:N2}";
    }
}