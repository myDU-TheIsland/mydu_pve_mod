using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Helpers;

namespace Mod.DynamicEncounters.Threads.Handles;

public class MediumPriority(int framesPerSecond, BehaviorTaskCategory category, bool fixedStep = false)
    : ConstructBehaviorLoop(framesPerSecond, category, fixedStep);
public class HighPriority(int framesPerSecond, BehaviorTaskCategory category, bool fixedStep = false)
    : ConstructBehaviorLoop(framesPerSecond, category, fixedStep);

public class MovementPriority(int framesPerSecond, BehaviorTaskCategory category, bool fixedStep = false)
    : ConstructBehaviorLoop(framesPerSecond, category, fixedStep);

public class ConstructBehaviorLoop : HighTickModLoop
{
    private readonly BehaviorTaskCategory _category;
    private readonly IServiceProvider _provider;
    private readonly ILogger<ConstructBehaviorLoop> _logger;
    private readonly IConstructBehaviorFactory _behaviorFactory;
    private readonly IConstructDefinitionFactory _constructDefinitionFactory;

    public static readonly ConcurrentDictionary<ulong, ConstructHandleItem> ConstructHandles = [];
    public static readonly ConcurrentDictionary<ulong, DateTime> ConstructHandleHeartbeat = [];
    public static readonly object ListLock = new();

    public ConstructBehaviorLoop(
        int framesPerSecond,
        BehaviorTaskCategory category,
        bool fixedStep = false
    ) : base(framesPerSecond, fixedStep)
    {
        _category = category;
        _provider = ModBase.ServiceProvider;
        _logger = _provider.CreateLogger<ConstructBehaviorLoop>();

        _behaviorFactory = _provider.GetRequiredService<IConstructBehaviorFactory>();
        _constructDefinitionFactory = _provider.GetRequiredService<IConstructDefinitionFactory>();
    }

    public override async Task Tick(TimeSpan deltaTime, CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested) return;
        
        var sw = new Stopwatch();
        sw.Start();

        List<ConstructHandleItem> constructHandleList;

        lock (ListLock)
        {
            constructHandleList = ConstructHandles.Select(x => x.Value).ToList();
        }

        if (constructHandleList.Count == 0)
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            StatsRecorder.Record(_category, sw.ElapsedMilliseconds);
            return;
        }

        await Parallel.ForEachAsync(
            constructHandleList, async (item, token) =>
            {
                if (token.IsCancellationRequested) return;
                await RunIsolatedAsync(() => TickConstructHandle(deltaTime, item, stoppingToken));
            });

        StatsRecorder.Record(_category, sw.ElapsedMilliseconds);
    }

    private async Task RunIsolatedAsync(Func<Task> taskFn)
    {
        try
        {
            await taskFn();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Construct Handle Task Failed");
        }
    }

    private async Task TickConstructHandle(
        TimeSpan deltaTime, 
        ConstructHandleItem handleItem, 
        CancellationToken stoppingToken)
    {
        if (handleItem.ConstructDefinitionItem == null)
        {
            return;
        }

        var constructDef = _constructDefinitionFactory.Create(handleItem.ConstructDefinitionItem);

        var finalBehaviors = new List<IConstructBehavior>();

        var behaviors = _behaviorFactory.CreateBehaviors(
            handleItem.ConstructId,
            constructDef,
            handleItem.JsonProperties.Behaviors
        ).Where(x => x.Category == _category);

        finalBehaviors.AddRange(behaviors);

        // TODO TerritoryId
        var context = await ConstructBehaviorContextCache.Data
            .TryGetOrSetValue(
                handleItem.ConstructId,
                () => Task.FromResult(
                    new BehaviorContext(
                        handleItem.ConstructId,
                        handleItem.FactionId,
                        null,
                        handleItem.Sector,
                        _provider,
                        constructDef
                    )
                    {
                        Properties = new ConcurrentDictionary<string, object>(handleItem.JsonProperties.Context),
                    }
                )
            );

        context.DeltaTime = deltaTime.TotalSeconds;

        foreach (var behavior in finalBehaviors)
        {
            if (stoppingToken.IsCancellationRequested) return;
            
            await behavior.InitializeAsync(context);
        }

        foreach (var behavior in finalBehaviors)
        {
            if (stoppingToken.IsCancellationRequested) return;
            
            if (!context.IsBehaviorActive(behavior.GetType()))
            {
                continue;
            }

            await behavior.TickAsync(context);
        }
    }

    public static void RecordConstructHeartBeat(ulong constructId)
    {
        ConstructHandleHeartbeat.AddOrUpdate(
            constructId,
            _ => DateTime.UtcNow,
            (_, _) => DateTime.UtcNow
        );
    }
}