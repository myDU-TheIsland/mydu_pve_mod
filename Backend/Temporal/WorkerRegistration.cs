using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Common.Helpers;
using Mod.DynamicEncounters.Features.TaskQueue.Activities;
using Mod.DynamicEncounters.Features.TaskQueue.Workflows;
using Mod.DynamicEncounters.Temporal.Activities;
using Mod.DynamicEncounters.Temporal.Services;
using Mod.DynamicEncounters.Temporal.Workflows;
using Temporalio.Extensions.Hosting;

namespace Mod.DynamicEncounters.Temporal;

public static class WorkerRegistration
{
    public static void RegisterTemporalWorker(this IServiceCollection services)
    {
        try
        {
            var builder = services.AddHostedTemporalWorker(
                clientTargetHost: TemporalConfig.GetHost(),
                clientNamespace: TemporalConfig.GetNamespace(),
                taskQueue: TemporalConfig.GetTaskQueue()
            );

            builder.AddScopedActivities<DiscordActivities>();
            builder.AddScopedActivities<ScriptActivities>();

            builder.AddWorkflow<LiveWorkflow>();
            builder.AddWorkflow<RunScriptWorkflow>();
        
            services.AddHostedService<TemporalStartupBackgroundService>();
        }
        catch (Exception e)
        {
            LoggerFactory.Create(builder => builder.SetupPveModLog())
                .CreateLogger(nameof(WorkerRegistration))
                .LogError(e, "Failed to Start Temporal");
        }
    }
}