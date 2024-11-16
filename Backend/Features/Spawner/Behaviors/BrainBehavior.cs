using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Helpers;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors;

public class BrainBehavior(ulong constructId, IPrefab prefab) : IConstructBehavior
{
    private ILogger<BrainBehavior> _logger;
    public BehaviorTaskCategory Category => BehaviorTaskCategory.MediumPriority;

    public Task InitializeAsync(BehaviorContext context)
    {
        _logger = context.ServiceProvider.CreateLogger<BrainBehavior>();
        
        return Task.CompletedTask;
    }

    public async Task TickAsync(BehaviorContext context)
    {
        await Task.Yield();
        
        _logger.LogInformation("Construct {Construct}-{Prefab} Brain", constructId, prefab.DefinitionItem.Name);
        
        
    }
}