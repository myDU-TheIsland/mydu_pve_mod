﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Common.Helpers;
using Mod.DynamicEncounters.Common.Interfaces;
using Mod.DynamicEncounters.Common.Services;
using Mod.DynamicEncounters.Database.Interfaces;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Events.Data;
using Mod.DynamicEncounters.Features.Events.Interfaces;
using Mod.DynamicEncounters.Features.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Extensions;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Sector.Data;
using Mod.DynamicEncounters.Features.Sector.Interfaces;
using Mod.DynamicEncounters.Features.Sector.Temporal.Workflows;
using Mod.DynamicEncounters.Helpers;
using NQ;

namespace Mod.DynamicEncounters.Features.Sector.Services;

public class SectorPoolManager(IServiceProvider serviceProvider) : ISectorPoolManager
{
    public const double SectorGridSnap = DistanceHelpers.OneSuInMeters * 20;

    private readonly IRandomProvider _randomProvider = serviceProvider.GetRequiredService<IRandomProvider>();

    private readonly ISectorInstanceRepository _sectorInstanceRepository =
        serviceProvider.GetRequiredService<ISectorInstanceRepository>();

    private readonly IConstructHandleManager _constructHandleManager =
        serviceProvider.GetRequiredService<IConstructHandleManager>();

    private readonly IConstructSpatialHashRepository _constructSpatial =
        serviceProvider.GetRequiredService<IConstructSpatialHashRepository>();

    private readonly ILogger<SectorPoolManager> _logger = serviceProvider.CreateLogger<SectorPoolManager>();

    public async Task GenerateTerritorySectors(SectorGenerationArgs args)
    {
        var count = await _sectorInstanceRepository.GetCountByTerritoryAsync(args.TerritoryId);
        var missingQuantity = args.Quantity - count;

        if (missingQuantity <= 0)
        {
            return;
        }

        var handleCount = await _constructHandleManager.GetActiveCount();
        var featureReaderService = serviceProvider.GetRequiredService<IFeatureReaderService>();
        var maxBudgetConstructs = await featureReaderService.GetIntValueAsync("MaxConstructHandles", 50);

        if (handleCount >= maxBudgetConstructs)
        {
            _logger.LogError(
                "Generate Territory({Territory}) Sector: Reached MAX Number of Construct Handles to Spawn: {Max}",
                args.TerritoryId,
                maxBudgetConstructs
            );
            return;
        }

        var random = _randomProvider.GetRandom();

        var randomMinutes = random.Next(0, 60);

        for (var i = 0; i < missingQuantity; i++)
        {
            if (!args.Encounters.Any())
            {
                continue;
            }

            var encounter = random.PickOneAtRandom(args.Encounters);

            var radius = MathFunctions.Lerp(
                encounter.Properties.MinRadius,
                encounter.Properties.MaxRadius,
                random.NextDouble()
            );

            var position = random.RandomDirectionVec3() * radius;
            position += encounter.Properties.CenterPosition;
            position = position.GridSnap(SectorGridSnap);

            var id = Guid.NewGuid();

            await SectorInstanceWorkflow.CreateWorkflowAsync(new SectorInstanceWorkflow.Input
            {
                SectorId = id,
                FactionId = args.FactionId,
                ExpirationTimeSpan = encounter.Properties.ExpirationTimeSpan,
                ForcedExpirationTimeSpan = (encounter.Properties.ForcedExpirationTimeSpan ?? TimeSpan.FromHours(3)) -
                                           encounter.Properties.ExpirationTimeSpan
            });
            
            var instance = new SectorInstance
            {
                Id = id,
                Name = encounter.Name,
                Sector = position,
                FactionId = args.FactionId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow + encounter.Properties.ExpirationTimeSpan +
                            TimeSpan.FromMinutes(randomMinutes * i),
                ForceExpiresAt = DateTime.UtcNow +
                                 (encounter.Properties.ForcedExpirationTimeSpan ?? TimeSpan.FromHours(6)),
                TerritoryId = args.TerritoryId,
                OnLoadScript = encounter.OnLoadScript,
                OnSectorEnterScript = encounter.OnSectorEnterScript,
                Properties = new SectorInstanceProperties
                {
                    Tags = [],
                    HasActiveMarker = encounter.Properties.HasActiveMarker
                }
            };

            if (position is { x: 0, y: 0, z: 0 })
            {
                _logger.LogWarning("BLOCKED Sector 0,0,0 creation");
                return;
            }

            try
            {
                await _sectorInstanceRepository.AddAsync(instance);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "Failed to create sector. Likely violating unique constraint. It will be tried again on the next cycle");
                await Task.Delay(500);
            }
        }
    }

    public async Task LoadUnloadedSectors()
    {
        var scriptService = serviceProvider.GetRequiredService<IScriptService>();
        var unloadedSectors = (await _sectorInstanceRepository.FindUnloadedAsync()).ToList();

        if (unloadedSectors.Count == 0)
        {
            _logger.LogInformation("No Sectors {Count} Need Loading", unloadedSectors.Count);
            return;
        }

        var handleCount = await _constructHandleManager.GetActiveCount();
        var featureReaderService = serviceProvider.GetRequiredService<IFeatureReaderService>();
        var maxBudgetConstructs = await featureReaderService.GetIntValueAsync("MaxConstructHandles", 50);

        if (handleCount >= maxBudgetConstructs)
        {
            _logger.LogError("LoadUnloadedSectors: Reached MAX Number of Construct Handles to Spawn: {Max}",
                maxBudgetConstructs);
            return;
        }

        foreach (var sector in unloadedSectors)
        {
            try
            {
                await scriptService.ExecuteScriptAsync(
                    sector.OnLoadScript,
                    new ScriptContext(
                        sector.FactionId,
                        [],
                        sector.Sector,
                        sector.TerritoryId
                    )
                    {
                        // TODO properties for OnLoadScript
                        // Properties = 
                    }
                ).OnError(exception =>
                {
                    _logger.LogError(exception, "Failed to Execute On Load Script (Aggregate). {Script}", sector.OnLoadScript);

                    foreach (var e in exception.InnerExceptions)
                    {
                        _logger.LogError(e, "Failed to Execute On Load Script");
                    }
                });

                await Task.Delay(200);
                await _sectorInstanceRepository.SetLoadedAsync(sector.Id, true);

                _logger.LogInformation("Loaded Sector {Id}({Sector}) Territory = {Territory}", sector.Id, sector.Sector,
                    sector.TerritoryId);
            }
            catch (Exception e)
            {
                // On Failure... expire the sector quicker.
                // Maybe the server is under load
                _logger.LogError(e, "Failed to Load Sector {Id}({Sector})", sector.Id, sector.Sector);
                await _sectorInstanceRepository.SetLoadedAsync(sector.Id, true);
                await _sectorInstanceRepository.SetExpirationFromNowAsync(sector.Id, TimeSpan.FromMinutes(10));
            }
        }
    }

    public async Task ExecuteSectorCleanup()
    {
        var sw = new Stopwatch();
        sw.Start();
        
        try
        {
            await _sectorInstanceRepository.ExpireSectorsWithDeletedConstructHandles();
            await _constructHandleManager.TagAsDeletedConstructHandlesThatAreDeletedConstructs();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to ExpireSectorsWithDeletedConstructHandles");
        }

        var expiredSectors = await _sectorInstanceRepository.FindExpiredAsync();

        foreach (var sector in expiredSectors)
        {
            var players = await _constructSpatial.FindPlayerLiveConstructsOnSector(sector.Sector);
            if (!sector.IsForceExpired(DateTime.UtcNow) && players.Any())
            {
                _logger.LogWarning("Players Nearby - Extended Expiration of {Sector} {SectorGuid}", sector.Sector,
                    sector.Id);
                await _sectorInstanceRepository.SetExpirationFromNowAsync(sector.Id, TimeSpan.FromMinutes(60));
                continue;
            }

            await _constructHandleManager.CleanupConstructHandlesInSectorAsync(sector.Sector);
            try
            {
                await DeleteNpcsBySector(sector.Sector);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to Delete NPCs");
            }

            try
            {
                await DeleteWrecksBySector(sector.Sector);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to Delete Wrecks");
            }
            await Task.Delay(200);
        }

        await _sectorInstanceRepository.DeleteExpiredAsync();
        // await _constructHandleManager.CleanupConstructsThatFailedSectorCleanupAsync();

        _logger.LogInformation("Executed Sector Cleanup");
        StatsRecorder.Record("ExecuteSectorCleanup", sw.ElapsedMilliseconds);
    }

    public async Task DeleteNpcsBySector(Vec3 sector)
    {
        var areaScanService = serviceProvider.GetRequiredService<IAreaScanService>();
        var contacts = await areaScanService.ScanForNpcConstructs(sector,
            DistanceHelpers.OneSuInMeters * 10, 50);

        foreach (var contact in contacts)
        {
            var context =
                new ScriptContext(1, [], new Vec3(), null).WithConstructId(contact.ConstructId);
            await serviceProvider.GetScriptAction(new ScriptActionItem
            {
                Type = "delete",
                ConstructId = contact.ConstructId
            }).ExecuteAsync(context);
        }
    }
    
    public async Task DeleteWrecksBySector(Vec3 sector)
    {
        var areaScanService = serviceProvider.GetRequiredService<IAreaScanService>();
        var contacts = await areaScanService.ScanForAbandonedConstructs(sector,
            DistanceHelpers.OneSuInMeters * 10, 50);

        foreach (var contact in contacts)
        {
            var context = new ScriptContext(1, [], new Vec3(), null)
                .WithConstructId(contact.ConstructId);
            
            await serviceProvider.GetScriptAction(new ScriptActionItem
            {
                Type = "delete",
                ConstructId = contact.ConstructId
            }).ExecuteAsync(context);
        }
    }

    public async Task SetExpirationFromNow(Vec3 sector, TimeSpan span)
    {
        var instance = await _sectorInstanceRepository.FindBySector(sector);

        if (instance == null)
        {
            return;
        }

        await _sectorInstanceRepository.SetExpirationFromNowAsync(instance.Id, span);

        _logger.LogInformation(
            "Set Sector expiration for {Sector}({Id}) to {Minutes} from now",
            instance.Sector,
            instance.Id,
            span
        );
    }

    public async Task SetExpirationFromNowIfGreater(Vec3 sector, TimeSpan span)
    {
        var instance = await _sectorInstanceRepository.FindBySector(sector);

        if (instance == null)
        {
            return;
        }

        if (instance.ExpiresAt < DateTime.UtcNow + span)
        {
            return;
        }

        await _sectorInstanceRepository.SetExpirationFromNowAsync(instance.Id, span);

        _logger.LogInformation(
            "Set Sector expiration for {Sector}({Id}) to {Minutes} from now",
            instance.Sector,
            instance.Id,
            span
        );
    }

    public async Task<SectorActivationOutcome> ForceActivateSector(Guid sectorId)
    {
        var sectorInstance = await _sectorInstanceRepository.FindById(sectorId);

        if (sectorInstance == null)
        {
            return SectorActivationOutcome.Failed($"Sector {sectorId} not found");
        }

        return await ActivateSector(sectorInstance);
    }

    public async Task ActivateEnteredSectors()
    {
        var sw = new Stopwatch();
        sw.Start();
        
        var sectorsToActivate = (await _sectorInstanceRepository
                .ScanForInactiveSectorsVisitedByPlayersV2(DistanceHelpers.OneSuInMeters * 10))
            .DistinctBy(x => x.Sector)
            .ToList();
        
        sw.Stop();
        
        StatsRecorder.Record("InactiveSectorTrigger", sw.ElapsedMilliseconds);

        if (sectorsToActivate.Count == 0)
        {
            _logger.LogDebug("No sectors need startup");
            return;
        }

        foreach (var sectorInstance in sectorsToActivate)
        {
            await ActivateSector(sectorInstance);
        }
    }

    private async Task<SectorActivationOutcome> ActivateSector(SectorInstance sectorInstance)
    {
        var areaScanService = serviceProvider.GetRequiredService<IAreaScanService>();
        var contacts = await areaScanService.ScanForPlayerContacts(0, 
            sectorInstance.Sector, 
            DistanceHelpers.OneSuInMeters * 10, 
            1);
        
        var constructs = contacts.Select(x => x.ConstructId).ToList();

        if (constructs.Count == 0)
        {
            return SectorActivationOutcome.Failed("No Player Constructs");
        }

        var constructService = serviceProvider.GetRequiredService<IConstructService>();
        var constructInfo = await constructService.GetConstructInfoAsync(constructs.First()); 
        
        HashSet<ulong> playerIds = [];

        if (constructInfo.Info?.mutableData.pilot != null)
        {
            playerIds.Add(constructInfo.Info.mutableData.pilot.Value);
        }

        _logger.LogInformation(
            "Starting up sector F({Faction}) ({Sector}) encounter: '{Encounter}'",
            sectorInstance.FactionId,
            sectorInstance.Sector,
            sectorInstance.OnSectorEnterScript
        );

        return await ActivateSectorInternal(sectorInstance, playerIds, constructs.ToHashSet());
    }

    private async Task<SectorActivationOutcome> ActivateSectorInternal(
        SectorInstance sectorInstance,
        HashSet<ulong> playerIds,
        HashSet<ulong> constructIds
    )
    {
        var scriptService = serviceProvider.GetRequiredService<IScriptService>();
        var eventService = serviceProvider.GetRequiredService<IEventService>();
        var random = serviceProvider.GetRandomProvider().GetRandom();

        try
        {
            await scriptService.ExecuteScriptAsync(
                sectorInstance.OnSectorEnterScript,
                new ScriptContext(
                    sectorInstance.FactionId,
                    [],
                    sectorInstance.Sector,
                    sectorInstance.TerritoryId
                )
                {
                    PlayerIds = playerIds,
                    // TODO Properties for OnSectorEnterScript
                    // Properties = 
                }
            );

            await _sectorInstanceRepository.TagAsStartedAsync(sectorInstance.Id);
            await Task.Delay(200);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "Failed to start encounter({Encounter}) at sector({Sector})",
                sectorInstance.OnSectorEnterScript,
                sectorInstance.Sector
            );

            return SectorActivationOutcome.Failed(e.Message);
        }

        try
        {
            ulong? playerId = null;
            if (playerIds.Count > 0)
            {
                playerId = random.PickOneAtRandom(playerIds);
            }

            await eventService.PublishAsync(
                new SectorActivatedEvent(
                    playerIds,
                    playerId,
                    sectorInstance.Sector,
                    constructIds.First(),
                    playerIds.Count
                )
            );
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to publish {Event}", nameof(SectorActivatedEvent));
        }

        return SectorActivationOutcome.Activated();
    }

    public async Task UpdateExpirationNames()
    {
        var constructHandleRepository = serviceProvider.GetRequiredService<IConstructHandleRepository>();
        var items = (await constructHandleRepository.GetPoiConstructExpirationTimeSpansAsync()).ToList();

        _logger.LogInformation("Update Expiration Names found: {Count}", items.Count);

        using var db = serviceProvider.GetRequiredService<IPostgresConnectionFactory>().Create();
        db.Open();

        foreach (var item in items)
        {
            try
            {
                var newName = $"{item.SectorName} [{(int)item.ExpiresAt.TotalMinutes}m]";
                if (item.StartedAt.HasValue && item.SectorInstanceProperties.HasActiveMarker)
                {
                    newName = $"{item.SectorName} [!!!]";
                }

                var constructService = serviceProvider.GetRequiredService<IConstructService>();
                await constructService.RenameConstruct(item.ConstructId, newName);

                _logger.LogDebug("Construct {Construct} Name Updated to: {Name}", item.ConstructId, newName);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to update expiration name of {Construct}", item.ConstructId);
            }
        }
    }
}