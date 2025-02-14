using System;
using System.Threading.Tasks;
using Mod.DynamicEncounters.Temporal.Activities;
using Temporalio.Workflows;

namespace Mod.DynamicEncounters.Temporal.Workflows;

[Workflow]
public class LiveWorkflow
{
    [WorkflowRun]
    public async Task RunAsync()
    {
        var options = new ActivityOptions
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(15)
        };
        
        await Workflow.ExecuteActivityAsync((DiscordActivities a) => a.SendDiscordMessage("PVE Started"), options);
    }
}