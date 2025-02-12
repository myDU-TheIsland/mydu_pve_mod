using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Temporalio.Client;

namespace Mod.DynamicEncounters.Workers;

public static class TemporalConfig
{
    public static TemporalClientConnectOptions CreateClientConnectOptions(IServiceProvider provider)
    {
        return new TemporalClientConnectOptions
        {
            LoggerFactory = provider.GetRequiredService<ILoggerFactory>(),
            TargetHost = GetHost(),
            Namespace = GetNamespace(),
        };
    }

    public static string GetTaskQueue()
    {
        return EnvironmentVariableHelper.GetEnvironmentVarOrDefault("TEMPORAL_TASK_QUEUE", "default");
    }

    public static string GetHost()
    {
        return EnvironmentVariableHelper.GetEnvironmentVarOrDefault("TEMPORAL_HOST", "10.10.42.100:7233");
    }

    public static string GetNamespace()
    {
        return EnvironmentVariableHelper.GetEnvironmentVarOrDefault("TEMPORAL_NAMESPACE", "default");
    }
}