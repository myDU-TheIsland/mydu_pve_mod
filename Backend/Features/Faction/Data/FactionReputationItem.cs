namespace Mod.DynamicEncounters.Features.Faction.Data;

public class FactionReputationItem
{
    public required long FactionId { get; set; }
    public required string FactionName { get; set; }
    public required long Reputation { get; set; }
}