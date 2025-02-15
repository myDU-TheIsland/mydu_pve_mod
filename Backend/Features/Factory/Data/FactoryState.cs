using System;
using System.Collections.Generic;

namespace Mod.DynamicEncounters.Features.Factory.Data;

public class FactoryState
{
    public required FactoryRunStatus RunStatus { get; set; }
    public required ulong RecipeId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public required List<FactoryItemEntry> InputItems { get; set; }
    public required List<FactoryItemEntry> OutputItems { get; set; }

    public TimeSpan GetTaskTimeSpan()
    {
        if (StartTime >= EndTime) return TimeSpan.FromSeconds(1);

        return EndTime - StartTime;
    }
}