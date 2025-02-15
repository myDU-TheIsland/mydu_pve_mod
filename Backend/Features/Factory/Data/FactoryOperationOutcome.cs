namespace Mod.DynamicEncounters.Features.Factory.Data;

public class FactoryOperationOutcome
{
    public bool AbortProduction { get; set; }
    public required FactoryState State { get; set; }
}