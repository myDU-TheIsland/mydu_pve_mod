using System.Threading.Tasks;
using Mod.DynamicEncounters.Features.Factory.Data;
using NQ;
using Temporalio.Activities;

namespace Mod.DynamicEncounters.Features.Factory.Activities;

public class FactoryActivities
{
    [Activity]
    public async Task<FactoryState> QueryFactoryState(ElementId elementId)
    {
        await Task.CompletedTask;
        
        return new FactoryState
        {
            RunStatus = FactoryRunStatus.Pending,
            InputItems = [],
            OutputItems = [],
            RecipeId = 0
        };
    }

    [Activity]
    public Task<FactoryOperationOutcome> PersistFactoryState(FactoryState state)
    {
        return null;
    }

    [Activity]
    public Task<FactoryOperationOutcome> StartFactory()
    {
        return null;
    }
    
    [Activity]
    public Task<FactoryOperationOutcome> RefundInputItems()
    {
        return null;
    }

    [Activity]
    public Task<FactoryOperationOutcome> DeliverProducedItems()
    {
        return null;
    }
}