using System;
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

        var options = new WorkflowOptions
        {
            Id = $"{nameof(RunScriptWorkflow)}/{command.Script.Type}/{Guid.NewGuid()}",
            TaskQueue = TemporalConfig.GetTaskQueue(),
            RetryPolicy = new RetryPolicy { MaximumAttempts = command.Context.RetryCount },
            RunTimeout = TimeSpan.FromMinutes(1)
        };
        
        var now = DateTime.UtcNow;
        
        if (command.DeliveryAt.HasValue && command.DeliveryAt.Value > DateTime.UtcNow)
        {
            options.StartDelay = now - command.DeliveryAt.Value;
        }
        
        await client.StartWorkflowAsync((RunScriptWorkflow wf) => wf.RunAsync(command), options);
    }
}