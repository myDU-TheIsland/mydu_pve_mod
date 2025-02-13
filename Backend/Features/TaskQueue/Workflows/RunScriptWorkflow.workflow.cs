using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Features.TaskQueue.Activities;
using Mod.DynamicEncounters.Features.TaskQueue.Interfaces;
using Temporalio.Common;
using Temporalio.Workflows;

namespace Mod.DynamicEncounters.Features.TaskQueue.Workflows;

[Workflow]
public class RunScriptWorkflow
{
    [WorkflowRun]
    public async Task RunAsync(IWorkflowEnqueueService.RunScriptCommand command)
    {
        var result = await Workflow.ExecuteActivityAsync((ScriptActivities a) => a.RunScriptAsync(command),
            new ActivityOptions
            {
                StartToCloseTimeout = TimeSpan.FromMinutes(10),
                RetryPolicy = new RetryPolicy { MaximumAttempts = command.Context.RetryCount }
            });

        if (!result.Success)
        {
            Workflow.Logger.LogError("Script Run Failed: {Message}", result.Message);
        }
    }
}