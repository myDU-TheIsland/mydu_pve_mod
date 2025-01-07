namespace Mod.DynamicEncounters.SDK;

public abstract class Actor : IActor
{
    public int FramesPerSecond { get; set; } = 20;
    public bool FixedStep { get; set; } = true;
    public bool TickActive { get; set; } = true;

    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public virtual Task Tick(TimeSpan deltaTime, CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    public virtual Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}