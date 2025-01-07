using System;
using System.Threading;
using System.Threading.Tasks;
using Mod.DynamicEncounters.SDK;

namespace Mod.DynamicEncounters.Threads.Handles;

public class ActorLoop(IActor actor) : HighTickModLoop(actor.FramesPerSecond, actor.FixedStep)
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await actor.StartAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }
    
    public override async Task Tick(TimeSpan deltaTime, CancellationToken stoppingToken)
    {
        await Task.Yield();
        await actor.Tick(deltaTime, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await actor.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}