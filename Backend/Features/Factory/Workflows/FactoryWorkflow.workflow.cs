using System;
using System.Threading;
using System.Threading.Tasks;
using Mod.DynamicEncounters.Features.Factory.Activities;
using Mod.DynamicEncounters.Features.Factory.Data;
using Temporalio.Common;
using Temporalio.Workflows;

namespace Mod.DynamicEncounters.Features.Factory.Workflows;

[Workflow]
public class FactoryWorkflow
{
    private bool _stopRequested;
    private bool _finishAndStop;

    [WorkflowRun]
    public async Task RunAsync(StartFactoryCommand command)
    {
        var outcome = await Workflow.ExecuteActivityAsync((FactoryActivities a) => a.StartFactory(),
            DefaultOptions());

        if (outcome.AbortProduction)
            return;

        var state = outcome.State;

        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var waitForStopTask = Workflow.WaitConditionAsync(() => _stopRequested, cancellationToken);
        var waitForCompletionTask = Workflow.DelayAsync(state.GetTaskTimeSpan(), cancellationToken);

        var resultTask = await Workflow.WhenAnyAsync(waitForStopTask, waitForCompletionTask);

        cts.Cancel();
        
        if (resultTask == waitForStopTask)
        {
            await Workflow.ExecuteActivityAsync((FactoryActivities a) => a.RefundInputItems(),
                DefaultOptions());

            return;
        }

        outcome = await Workflow.ExecuteActivityAsync((FactoryActivities a) => a.DeliverProducedItems(),
            DefaultOptions());

        if (_finishAndStop || outcome.AbortProduction)
            return;

        throw Workflow.CreateContinueAsNewException((FactoryWorkflow wf) => wf.RunAsync(command));

        ActivityOptions DefaultOptions() => new()
        {
            HeartbeatTimeout = TimeSpan.FromMinutes(1),
            RetryPolicy = new RetryPolicy { MaximumAttempts = 100 }
        };
    }

    [WorkflowSignal]
    public Task OnFactoryStopped()
    {
        _stopRequested = true;

        return Task.CompletedTask;
    }

    [WorkflowSignal]
    public Task OnFactoryFinishAndStop()
    {
        _finishAndStop = true;

        return Task.CompletedTask;
    }
}