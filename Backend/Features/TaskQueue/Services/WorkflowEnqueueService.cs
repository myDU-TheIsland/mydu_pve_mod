using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mod.DynamicEncounters.Features.TaskQueue.Interfaces;
using Mod.DynamicEncounters.Features.TaskQueue.Workflows;
using Mod.DynamicEncounters.Temporal.Services;
using Temporalio.Client;
using Temporalio.Common;

namespace Mod.DynamicEncounters.Features.TaskQueue.Services;

public class WorkflowEnqueueService : IWorkflowEnqueueService
{
    public async Task EnqueueAsync(IWorkflowEnqueueService.RunScriptCommand command)
    {
        var connectOptions = TemporalConfig.CreateClientConnectOptions(ModBase.ServiceProvider);
        var client = await TemporalClient.ConnectAsync(connectOptions);

        var workflowId = $"{nameof(RunScriptWorkflow)}/{command.Script.Type}/{Guid.NewGuid()}";
        var tags = $"{string.Join("/", command.Script.Tags)}";

        var pieces = new List<string> { workflowId, tags }.Where(r => !string.IsNullOrEmpty(r));
        var finalWorkflowId = string.Join("/", pieces);
        
        var options = new WorkflowOptions
        {
            Id = finalWorkflowId,
            TaskQueue = TemporalConfig.GetTaskQueue(),
            RetryPolicy = new RetryPolicy { MaximumAttempts = command.Context.RetryCount },
            RunTimeout = TimeSpan.FromMinutes(1),
        };
        
        var now = DateTime.UtcNow;
        
        if (command.StartAt.HasValue && command.StartAt.Value > DateTime.UtcNow)
        {
            options.StartDelay = command.StartAt.Value - now;
            if (options.StartDelay < TimeSpan.Zero)
            {
                options.StartDelay = null;
            }
        }
        
        await client.StartWorkflowAsync((RunScriptWorkflow wf) => wf.RunAsync(command), options);
    }
}