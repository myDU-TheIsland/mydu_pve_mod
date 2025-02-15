namespace Mod.DynamicEncounters.Features.Factory.Data;

public enum FactoryRunStatus
{
    Stopped = 1,
    Running = 2,
    JammedOutputFull = 3,
    JammedMissingIngredient = 4,
    JammedMissingSchematic = 5,
    Pending = 6,
    JammedNoOutputContainer = 7
}