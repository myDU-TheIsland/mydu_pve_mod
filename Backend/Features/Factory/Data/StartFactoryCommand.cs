namespace Mod.DynamicEncounters.Features.Factory.Data;

public class StartFactoryCommand
{
    public required ulong ElementId { get; set; }
    public required ulong NumBatches { get; set; }
    public required ulong MaintainProductAmount { get; set; }
}