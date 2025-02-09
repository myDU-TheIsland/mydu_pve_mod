using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Workers.Workflows.Live;
using Temporalio.Client;
using Temporalio.Converters;

namespace Mod.DynamicEncounters.Workers;

public class TemporalStartupBackgroundService(IServiceProvider provider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var logger = provider.GetRequiredService<ILoggerFactory>()
            .CreateLogger<TemporalStartupBackgroundService>();
        
        var client = await TemporalClient.ConnectAsync(TemporalConfig.CreateClientConnectOptions(provider));

        try
        {
            await client.StartWorkflowAsync((LiveWorkflow wf) => wf.RunAsync(Array.Empty<IRawValue>()),
                new WorkflowOptions(id: "test", taskQueue: TemporalConfig.GetTaskQueue()));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to Start");
        }
    }
}