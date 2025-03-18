using System;
using System.Threading.Tasks;
using Mod.DynamicEncounters.Common.Services;
using Mod.DynamicEncounters.Features.Sector.Temporal.Activities;
using Mod.DynamicEncounters.Temporal.Services;
using Temporalio.Client;
using Temporalio.Common;
using Temporalio.Workflows;

namespace Mod.DynamicEncounters.Features.Sector.Temporal.Workflows;

[Workflow]
public class SectorInstanceWorkflow
{
    [WorkflowRun]
    public async Task RunAsync(Input input)
    {
        await Workflow.DelayAsync(input.ExpirationTimeSpan);

        var expirationOutcome = await Workflow.ExecuteActivityAsync((SectorInstanceActivities a) => a.TryExpireSectorAsync(input.SectorId), Options());
        if (!expirationOutcome.Expired)
        {
            await Workflow.DelayAsync(input.ForcedExpirationTimeSpan);
            await Workflow.ExecuteActivityAsync((SectorInstanceActivities a) => a.ForceExpireSectorAsync(input.SectorId), Options());
        }

        await Workflow.WaitConditionAsync(() => Workflow.AllHandlersFinished);
        
        return;

        ActivityOptions Options() => new()
        {
            HeartbeatTimeout = TimeSpan.FromMinutes(1),
            RetryPolicy = new RetryPolicy { MaximumAttempts = 100 }
        };
    }

    public static async Task CreateWorkflowAsync(Input input)
    {
        var client = await TemporalClientFactory.GetClientAsync();
        await client.StartWorkflowAsync<SectorInstanceWorkflow>(
            wf => wf.RunAsync(input),
            new WorkflowOptions
            {
                Id = $"{nameof(SectorInstanceWorkflow)}/faction/{input.FactionId}/id/{input.SectorId}",
                TaskQueue = TemporalConfig.GetTaskQueue(),
                RunTimeout = TimeSpan.FromDays(1),
            });
    }

    public record Input
    {
        public required Guid SectorId { get; set; }
        public long FactionId { get; set; }
        public required TimeSpan ExpirationTimeSpan { get; set; }
        public required TimeSpan ForcedExpirationTimeSpan { get; set; }
    }
}