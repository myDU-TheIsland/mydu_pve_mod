using System;
using System.Threading;
using System.Threading.Tasks;
using FluentMigrator.Runner;

namespace Mod.DynamicEncounters.Threads.Handles;

public abstract class HighTickModLoop : ThreadHandle
{
    private StopWatch _stopWatch = new();
    private DateTime _lastTickTime;
    private readonly int _framesPerSecond;
    private readonly bool _fixedStep;
    private const double FixedDeltaTime = 1 / 20d;
    private TimeSpan _accumulatedTime = TimeSpan.Zero;

    protected HighTickModLoop(
        int framesPerSecond, 
        ThreadId threadId,
        IThreadManager threadManager,
        CancellationToken token,
        bool fixedStep
    ) : base(threadId, threadManager, token)
    {
        _framesPerSecond = framesPerSecond;
        _fixedStep = fixedStep;
        _stopWatch.Start();
        _lastTickTime = DateTime.UtcNow;

        if (_framesPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(_framesPerSecond), "Frames per second should be > 0");
        }
    }

    public override Task Tick()
    {
        if (_fixedStep)
        {
            return FixedStepTickInternal();
        }

        return TickInternal();
    }
    
    public async Task TickInternal()
    {
        var currentTickTime = DateTime.UtcNow;
        var deltaTime = currentTickTime - _lastTickTime;
        _lastTickTime = currentTickTime;

        var fpsSeconds = 1d / _framesPerSecond;
        if (deltaTime.TotalSeconds < fpsSeconds)
        {
            var waitSeconds = Math.Max(0, fpsSeconds - deltaTime.TotalSeconds);
            Thread.Sleep(TimeSpan.FromSeconds(waitSeconds));
        }
            
        await Tick(deltaTime);

        _stopWatch = new StopWatch();
        _stopWatch.Start();
    }
    
    public async Task FixedStepTickInternal()
    {
        var currentTickTime = DateTime.UtcNow;
        var deltaTime = currentTickTime - _lastTickTime;
        _lastTickTime = currentTickTime;

        var fpsSeconds = 1d / _framesPerSecond;
        if (deltaTime.TotalSeconds < fpsSeconds)
        {
            var waitSeconds = Math.Max(0, fpsSeconds - deltaTime.TotalSeconds);
            Thread.Sleep(TimeSpan.FromSeconds(waitSeconds));
        }

        _accumulatedTime += deltaTime;
        var fixedDeltaSpan = TimeSpan.FromSeconds(FixedDeltaTime);
        
        while (_accumulatedTime >= fixedDeltaSpan)
        {
            await Tick(fixedDeltaSpan);
            _accumulatedTime -= fixedDeltaSpan;
        }

        _stopWatch = new StopWatch();
        _stopWatch.Start();
    }

    public virtual Task Tick(TimeSpan deltaTime)
    {
        return Task.CompletedTask;
    }
}