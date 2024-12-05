using Backend;
using Mod.DynamicEncounters.Features.Loot.Data;
using NQ;
using NQutils.Def;

namespace Mod.DynamicEncounters.Features.Quests.Data;

public readonly struct QuestElementQuantityRef
{
    public QuestElementQuantityRef(IGameplayBank bank,
        ElementId? elementId, 
        ElementTypeName elementTypeName,
        long quantity)
    {
        var def = bank.GetDefinition(elementTypeName);
        var baseObj = def?.BaseObject;
        var displayName = baseObj?.DisplayName ?? string.Empty;
        var scale = "";
        if (baseObj is BaseItem baseItem)
        {
            scale = baseItem.Scale.ToUpper();
        }
        
        ElementId = elementId;
        ElementTypeName = elementTypeName;
        DisplayName = string.Join(" ", displayName, scale);
        Quantity = quantity;
    }

    public ElementId? ElementId { get; }
    public ElementTypeName ElementTypeName { get; }
    public string DisplayName { get; }
    public long Quantity { get; }
}