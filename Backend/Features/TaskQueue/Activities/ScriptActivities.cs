using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.TaskQueue.Interfaces;
using Temporalio.Activities;

namespace Mod.DynamicEncounters.Features.TaskQueue.Activities;

public sealed class ScriptActivities
{
    [Activity]
    public async Task<ScriptActionResult> RunScriptAsync(IWorkflowEnqueueService.RunScriptCommand command)
    {
        await using var scope = ModBase.ServiceProvider.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        var scriptActionFactory = provider.GetRequiredService<IScriptActionFactory>();
        var scriptAction = scriptActionFactory.Create(command.Script);

        return await scriptAction.ExecuteAsync(
            new ScriptContext(
                scope.ServiceProvider,
                command.Context.FactionId,
                command.Context.PlayerIds,
                command.Context.Sector,
                command.Context.TerritoryId
            ));
    }
}