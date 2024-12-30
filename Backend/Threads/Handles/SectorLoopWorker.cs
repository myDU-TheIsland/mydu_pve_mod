using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Common.Helpers;
using Mod.DynamicEncounters.Features.Faction.Data;
using Mod.DynamicEncounters.Features.Faction.Interfaces;
using Mod.DynamicEncounters.Features.Sector.Data;
using Mod.DynamicEncounters.Features.Sector.Interfaces;
using Mod.DynamicEncounters.Helpers;

namespace Mod.DynamicEncounters.Threads.Handles;

public class SectorLoopWorker : BackgroundService
{
    private readonly IServiceProvider _provider = ModBase.ServiceProvider;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);
            
            await Tick(cts.Token);
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
    
    public async Task Tick(CancellationToken stoppingToken)
    {
        var logger = ModBase.ServiceProvider.CreateLogger<SectorLoopWorker>();

        try
        {
            await ExecuteAction(stoppingToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to execute {Name}", nameof(SectorLoopWorker));
        }
    }
    
    private async Task ExecuteAction(CancellationToken stoppingToken)
    {
        var sw = new Stopwatch();
        sw.Start();

        var logger = _provider.CreateLogger<SectorLoopWorker>();

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            { "Thread", Environment.CurrentManagedThreadId }
        });

        var factionRepository = _provider.GetRequiredService<IFactionRepository>();
        var sectorPoolManager = _provider.GetRequiredService<ISectorPoolManager>();

        await sectorPoolManager.ExecuteSectorCleanup()
            .OnError(exception => { logger.LogError(exception, "Failed to Execute Sector Cleanup"); })
            .WaitAsync(stoppingToken);

        var factionSectorPrepTasks = (await factionRepository.GetAllAsync())
            .Select(PrepareFactionSector);
        await Task.WhenAll(factionSectorPrepTasks).WaitAsync(stoppingToken);

        await sectorPoolManager.LoadUnloadedSectors().WaitAsync(stoppingToken);
        await sectorPoolManager.ActivateEnteredSectors().WaitAsync(stoppingToken);

        logger.LogInformation("Sector Loop Action took {Time}ms", sw.ElapsedMilliseconds);
        StatsRecorder.Record("SectorLoop", sw.ElapsedMilliseconds);
    }

    private async Task PrepareFactionSector(FactionItem faction)
    {
        var logger = _provider.CreateLogger<SectorLoopWorker>();
        logger.LogDebug("Preparing Sector for {Faction}", faction.Name);

        // TODO sector encounters becomes tied to a territory
        // a territory has center, max and min radius
        // a territory is owned by a faction
        // a territory is a point on a map - not constructs nor DU's territories - perhaps name it differently
        var sectorEncountersRepository = _provider.GetRequiredService<ISectorEncounterRepository>();

        var factionTerritoryRepository = _provider.GetRequiredService<IFactionTerritoryRepository>();
        var factionTerritories = await factionTerritoryRepository.GetAllByFactionAsync(faction.Id);

        var sectorPoolManager = _provider.GetRequiredService<ISectorPoolManager>();

        foreach (var ft in factionTerritories)
        {
            var encountersTask =
                sectorEncountersRepository.FindActiveByFactionTerritoryAsync(ft.FactionId, ft.TerritoryId);
            encountersTask.Wait();
            
            var encounters = encountersTask.Result.ToList();

            if (encounters.Count == 0)
            {
                logger.LogDebug("No Encounters for Faction: {Faction}({Id}) Territory({Territory})", faction.Name,
                    faction.Id, ft.TerritoryId);
                continue;
            }

            var args = new SectorGenerationArgs
            {
                Encounters = encounters,
                Quantity = ft.SectorCount,
                FactionId = faction.Id,
                Tag = faction.Tag,
                TerritoryId = ft.TerritoryId,
            };

            try
            {
                sectorPoolManager.GenerateTerritorySectors(args).Wait();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to generate faction({F}) territory({T}) sectors", args.FactionId,
                    args.TerritoryId);
            }
        }
    }
}