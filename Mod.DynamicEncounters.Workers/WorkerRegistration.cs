using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Workers.Workflows.Live;
using Mod.DynamicEncounters.Workers.Workflows.Live.Activities;
using Temporalio.Extensions.Hosting;

namespace Mod.DynamicEncounters.Workers;

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

        builder.AddWorkflow<LiveWorkflow>();
    }
}