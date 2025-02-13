using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Temporal.Workflows;
using Temporalio.Client;

namespace Mod.DynamicEncounters.Temporal.Services;

public class TemporalStartupBackgroundService(IServiceProvider provider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var logger = provider.GetRequiredService<ILoggerFactory>()
            .CreateLogger<TemporalStartupBackgroundService>();
        
        try
        {
            var client = await TemporalClient.ConnectAsync(TemporalConfig.CreateClientConnectOptions(provider));
            
            await client.StartWorkflowAsync((LiveWorkflow wf) => wf.RunAsync(),
                new WorkflowOptions(id: $"{nameof(LiveWorkflow)}/{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}", taskQueue: TemporalConfig.GetTaskQueue()));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to Start");
        }
    }
}