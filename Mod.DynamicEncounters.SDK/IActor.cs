namespace Mod.DynamicEncounters.SDK;

public interface IActor
{
    int FramesPerSecond { get; }
    bool FixedStep { get; }

    Task StartAsync(CancellationToken cancellationToken);
    Task Tick(TimeSpan deltaTime, CancellationToken stoppingToken);
    Task StopAsync(CancellationToken cancellationToken);
}