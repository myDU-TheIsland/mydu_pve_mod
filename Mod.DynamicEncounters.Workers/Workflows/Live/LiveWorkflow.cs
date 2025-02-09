using Mod.DynamicEncounters.Workers.Workflows.Live.Activities;
using Temporalio.Converters;
using Temporalio.Workflows;

namespace Mod.DynamicEncounters.Workers.Workflows.Live;

[Workflow]
public class LiveWorkflow
{
    [WorkflowRun]
    public async Task RunAsync(IRawValue[] args)
    {
        var options = new ActivityOptions
        {
            StartToCloseTimeout = TimeSpan.FromSeconds(15)
        };
        
        await Workflow.ExecuteActivityAsync((SendTestMessageActivity a) => a.SendTestMessage(args), options);
        await Workflow.DelayAsync(TimeSpan.FromSeconds(5));
    }
}