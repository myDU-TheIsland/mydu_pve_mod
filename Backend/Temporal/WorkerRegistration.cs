using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.TaskQueue.Activities;
using Mod.DynamicEncounters.Features.TaskQueue.Workflows;
using Mod.DynamicEncounters.Temporal.Services;
using Mod.DynamicEncounters.Temporal.Workflows;
using Temporalio.Extensions.Hosting;

namespace Mod.DynamicEncounters.Temporal;

public static class WorkerRegistration
{
    public static void RegisterTemporalWorker(this IServiceCollection services)
    {
        var builder = services.AddHostedTemporalWorker(
            clientTargetHost: TemporalConfig.GetHost(),
            clientNamespace: TemporalConfig.GetNamespace(),
            taskQueue: TemporalConfig.GetTaskQueue()
        );

        builder.AddScopedActivities<SendTestMessageActivity>();
        builder.AddScopedActivities<ScriptActivities>();

        builder.AddWorkflow<LiveWorkflow>();
        builder.AddWorkflow<RunScriptWorkflow>();
    }
}