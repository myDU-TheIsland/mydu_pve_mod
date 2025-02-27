﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Scenegraph;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Common.Helpers;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Faction.Data;
using Mod.DynamicEncounters.Features.Faction.Interfaces;
using Mod.DynamicEncounters.Features.Interfaces;
using Mod.DynamicEncounters.Features.Loot.Interfaces;
using Mod.DynamicEncounters.Features.Quests.Data;
using Mod.DynamicEncounters.Features.Quests.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Helpers;
using NQ;

namespace Mod.DynamicEncounters.Features.Quests.Services;

public class ProceduralTransportMissionGeneratorService(IServiceProvider provider)
    : IProceduralTransportMissionGeneratorService
{
    private readonly ILogger<ProceduralQuestGeneratorService> _logger =
        provider.CreateLogger<ProceduralQuestGeneratorService>();

    private readonly IConstructService _constructService =
        provider.GetRequiredService<IConstructService>();
    
    private readonly IFeatureReaderService _featureReaderService =
        provider.GetRequiredService<IFeatureReaderService>();
    
    public async Task<ProceduralQuestOutcome> GenerateAsync(
        PlayerId playerId,
        FactionId factionId,
        TerritoryId territoryId,
        int seed
    )
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            {nameof(playerId), playerId},
            {nameof(factionId), factionId},
            {nameof(territoryId), territoryId},
        });

        var factionRepository = provider.GetRequiredService<IFactionRepository>();
        var faction = await factionRepository.FindAsync(factionId);

        if (faction == null)
        {
            return ProceduralQuestOutcome.Failed($"Faction {factionId.Id} not found");
        }

        var timeFactor = TimeUtility.GetTimeSnapped(DateTimeOffset.UtcNow, MissionProceduralGenerationConfig.TransportMissionTimeFactor);
        var random = new Random(seed);

        var questSeed = random.Next();
        const string questType = QuestTypes.Transport;

        var factionTerritoryRepository = provider.GetRequiredService<IFactionTerritoryRepository>();
        
        var territoryMap = (await factionTerritoryRepository.GetAll())
            .DistinctBy(v => v.TerritoryId)
            .ToDictionary(
                k => k.TerritoryId,
                v => v
            );

        // remove param territory
        territoryMap.Remove(territoryId);

        if (territoryMap.Keys.Count == 0)
        {
            return ProceduralQuestOutcome.Failed("No other faction territories available");
        }

        var territoryContainerRepository = provider.GetRequiredService<ITerritoryContainerRepository>();
        var fromContainerList = (await territoryContainerRepository.GetAll(territoryId)).ToList();

        if (fromContainerList.Count == 0)
        {
            return ProceduralQuestOutcome.Failed("No pickup containers available");
        }

        var questPickupContainer = random.PickOneAtRandom(fromContainerList);

        var dropContainerTerritory = random.PickOneAtRandom(territoryMap.Keys);
        var dropContainerList = (await territoryContainerRepository.GetAll(dropContainerTerritory)).ToList();

        if (dropContainerList.Count == 0)
        {
            return ProceduralQuestOutcome.Failed($"No drop containers available for '{dropContainerTerritory}'");
        }

        var dropContainer = random.PickOneAtRandom(dropContainerList);

        var pickupGuid = GuidUtility.Create(
            territoryId,
            $"{playerId}-{QuestTaskItemType.Pickup}-{factionId.Id}-{territoryId.Id}-{timeFactor}"
        );
        var dropGuid = GuidUtility.Create(
            territoryId,
            $"{playerId}-{QuestTaskItemType.Deliver}-{factionId.Id}-{dropContainerTerritory}-{timeFactor}"
        );
        var questGuid = GuidUtility.Create(
            territoryId,
            $"{questType}-{factionId.Id}-{territoryId.Id}-{pickupGuid}-{dropGuid}-{timeFactor}"
        );

        var constructService = provider.GetRequiredService<IConstructService>();
        var pickupConstructInfo = await constructService.GetConstructInfoAsync(questPickupContainer.ConstructId);
        var dropConstructInfo = await constructService.GetConstructInfoAsync(dropContainer.ConstructId);

        if (!pickupConstructInfo.ConstructExists || pickupConstructInfo.Info == null)
        {
            return ProceduralQuestOutcome.Failed(
                $"Pickup Construct '{questPickupContainer.ConstructId}' doesn't exist");
        }

        if (!dropConstructInfo.ConstructExists || dropConstructInfo.Info == null)
        {
            return ProceduralQuestOutcome.Failed($"Drop Construct '{dropContainer.ConstructId}' doesn't exist");
        }

        var transportMissionTemplateProvider = provider.GetRequiredService<ITransportMissionTemplateProvider>();
        var missionTemplate = await transportMissionTemplateProvider.GetMissionTemplate(random.Next());
        missionTemplate = missionTemplate
            .SetPickupConstructName(pickupConstructInfo.Info.rData.name)
            .SetDeliverConstructName(dropConstructInfo.Info.rData.name);

        var sceneGraph = provider.GetRequiredService<IScenegraph>();
        var pickupPos = await sceneGraph.GetConstructCenterWorldPosition(questPickupContainer.ConstructId);
        var deliveryPos = await sceneGraph.GetConstructCenterWorldPosition(dropContainer.ConstructId);

        var distanceMeters = (pickupPos - deliveryPos).Size();
        var distanceSu = distanceMeters / DistanceHelpers.OneSuInMeters;

        var multiplier = 1;
        if (dropConstructInfo.Info.kind == ConstructKind.STATIC)
        {
            multiplier++;
        }

        if (pickupConstructInfo.Info.kind == ConstructKind.STATIC)
        {
            multiplier++;
        }
        
        var pickupInSafeZone = await _constructService.IsInSafeZone(pickupConstructInfo.Info.rData.constructId);
        var dropInSafeZone = await _constructService.IsInSafeZone(dropConstructInfo.Info.rData.constructId);
        var isSafe = pickupInSafeZone && dropInSafeZone;

        var quantaMultiplier = await _featureReaderService.GetDoubleValueAsync("TransportMissionMultiplier", 1);
        var safeMultiplier = await _featureReaderService.GetDoubleValueAsync("TransportMissionSafeMultiplier", 1);
        var pvpMultiplier = await _featureReaderService.GetDoubleValueAsync("TransportMissionPvpMultiplier", 3d);
        
        var unsafeMultiplier = isSafe ? safeMultiplier : pvpMultiplier;
        var quantaReward = (long)(distanceSu * 10000d * 100d * quantaMultiplier * multiplier * unsafeMultiplier);
        var influenceReward = 1;

        var kergonQuantity = new LitreQuantity(3000);

        return ProceduralQuestOutcome.Created(
            new ProceduralQuestItem(
                questGuid,
                factionId,
                questType,
                questSeed,
                missionTemplate.Title,
                isSafe,
                DistanceHelpers.OneSuInMeters / 4d,
                new ProceduralQuestProperties
                {
                    RewardTextList =
                    [
                        $"{quantaReward / 100:N2}h",
                        $"Kergon X1: {kergonQuantity.GetRawQuantity()}L",
                        $"Influence with {faction.Name} +{influenceReward}"
                    ],
                    QuantaReward = quantaReward,
                    InfluenceReward =
                    {
                        { factionId, influenceReward }
                    },
                    ExpiresAt = DateTime.UtcNow + TimeSpan.FromHours(3),
                    ItemRewardMap =
                    {
                        {"Kergon1", kergonQuantity.ToQuantity()}
                    },
                    DistanceMeters = distanceMeters,
                    DistanceSu = distanceMeters
                },
                new List<QuestTaskItem>
                {
                    new(
                        new QuestTaskId(
                            questGuid,
                            Guid.NewGuid()
                        ),
                        missionTemplate.PickupMessage,
                        QuestTaskItemType.Pickup,
                        QuestTaskItemStatus.InProgress,
                        pickupPos,
                        null,
                        new ScriptActionItem
                        {
                            Type = "assert-task-completion",
                            FactionId = factionId,
                            TerritoryId = territoryId,
                            ConstructId = pickupConstructInfo.Info.rData.constructId,
                            Properties =
                            {
                                { "questId", questGuid },
                                { "questTaskId", pickupGuid }
                            }
                        },
                        new PickupItemTaskItemDefinition(
                            questPickupContainer,
                            missionTemplate.Items
                        )
                    ),
                    new(
                        new QuestTaskId(
                            questGuid,
                            Guid.NewGuid()
                        ),
                        missionTemplate.DeliverMessage,
                        QuestTaskItemType.Deliver,
                        QuestTaskItemStatus.InProgress,
                        deliveryPos,
                        null,
                        new ScriptActionItem
                        {
                            Type = "assert-task-completion",
                            FactionId = factionId,
                            TerritoryId = territoryId,
                            ConstructId = dropConstructInfo.Info.rData.constructId,
                            Properties =
                            {
                                { "questId", questGuid },
                                { "questTaskId", dropGuid }
                            }
                        },
                        new DeliverItemTaskDefinition(
                            dropContainer,
                            missionTemplate.Items
                                .Select(x => new QuestElementQuantityRef(
                                    x.ElementId,
                                    x.ElementTypeName, 
                                    -x.Quantity
                                ))
                        )
                    )
                }
            )
        );
    }
}