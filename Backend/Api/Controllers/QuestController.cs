﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Common.Interfaces;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Faction.Interfaces;
using Mod.DynamicEncounters.Features.Market.Interfaces;
using Mod.DynamicEncounters.Features.Quests.Data;
using Mod.DynamicEncounters.Features.Quests.Interfaces;
using Mod.DynamicEncounters.Helpers;
using NQutils.Def;

namespace Mod.DynamicEncounters.Api.Controllers;

[Route("quest")]
public class QuestController : Controller
{
    private readonly IServiceProvider _provider = ModBase.ServiceProvider;
    
    private readonly IProceduralQuestGeneratorService _proceduralQuestGeneratorService
        = ModBase.ServiceProvider.GetRequiredService<IProceduralQuestGeneratorService>();

    private readonly IPlayerQuestService _playerQuestService
        = ModBase.ServiceProvider.GetRequiredService<IPlayerQuestService>();

    private readonly ILogger<QuestController> _logger
        = ModBase.ServiceProvider.CreateLogger<QuestController>();

    [HttpPost]
    [Route("setup-territory-container")]
    public async Task<IActionResult> SetupTerritoryContainer([FromBody] SetupTerritoryContainerRequest request)
    {
        var repository = _provider.GetRequiredService<ITerritoryContainerRepository>();

        ulong elementId;

        if (!request.ElementId.HasValue)
        {
            var constructElementService = _provider.GetRequiredService<IConstructElementsService>();
            var elements = (await constructElementService.GetContainerElements(request.ConstructId)).ToList();

            if (elements.Count == 0)
            {
                return BadRequest("No Container on Construct");
            }

            elementId = elements.First().elementId;
        }
        else
        {
            elementId = request.ElementId.Value;
        }
        
        await repository.Add(request.TerritoryId, request.ConstructId, elementId);

        return Ok();
    }
    
    [HttpPost]
    [Route("player/accept")]
    public async Task<IActionResult> AcceptQuest([FromBody] AcceptQuestRequest request)
    {
        var quests = await _proceduralQuestGeneratorService
            .Generate(
                request.PlayerId,
                request.FactionId,
                request.TerritoryId,
                request.Seed
            );

        var questMap = quests.QuestList
            .ToDictionary(
                k => k.Id,
                v => v
            );

        if (!questMap.TryGetValue(request.QuestId, out var questItem))
        {
            return Ok(AcceptQuestResponse.Failed("No longer available. Refresh the board"));
        }

        var outcome = await _playerQuestService.AcceptQuestAsync(
            request.PlayerId,
            questMap[request.QuestId]
        );

        if (!outcome.Success)
        {
            return BadRequest(outcome);
        }

        return Ok(AcceptQuestResponse.Accepted($"Accepted '{questItem.Title}'. {outcome.Message}"));
    }

    [HttpPost]
    [Route("player/abandon")]
    public async Task<IActionResult> AbandonQuest([FromBody] AbandonQuestRequest request)
    {
        try
        {
            var playerQuestRepository = _provider.GetRequiredService<IPlayerQuestRepository>();

            await playerQuestRepository.DeleteAsync(request.PlayerId, request.QuestId);

            return Ok(AbandonQuestResponse.Abandoned());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to Abandon Player {Player} Quest {Id}", request.PlayerId, request.QuestId);
            
            return StatusCode(500, AbandonQuestResponse.Failed("Failed to Abandon Mission"));
        }
    }

    [HttpPost]
    [Route("interact")]
    public async Task<IActionResult> QuestInteract([FromBody] QuestInteractRequest request)
    {
        var questInteractionService = _provider.GetRequiredService<IQuestInteractionService>();
        var outcomeCollection = await questInteractionService.InteractAsync(
            new QuestInteractCommand
            {
                PlayerId = request.PlayerId,
                ConstructId = request.ConstructId,
                ElementId = request.ElementId
            }
        );
        
        return Ok(new QuestInteractResponse(outcomeCollection.Outcomes));
    }

    [HttpPost]
    [Route("callback/{questId:guid}/task/{questTaskId:guid}/complete")]
    public async Task<IActionResult> CompleteQuestTask(Guid questId, Guid questTaskId)
    {
        var questInteractionService = _provider.GetRequiredService<IQuestInteractionService>();
        
        _logger.LogInformation("Completed Quest {Quest} / Task {Task}", questId, questTaskId);
        
        return Ok(await questInteractionService.CompleteTaskAsync(new QuestTaskId(questId, questTaskId)));
    }
    
    [HttpPost]
    [Route("callback/{questId:guid}/task/{questTaskId:guid}/failed")]
    public async Task<IActionResult> FailedCompleteQuestTask(Guid questId, Guid questTaskId)
    {
        await Task.Yield();
        
        _logger.LogError("Failed to complete quest / {Quest} / Task {Task}", questId, questTaskId);
        
        return Ok();
    }

    [HttpGet]
    [Route("player/{playerId:long}")]
    public async Task<IActionResult> GetPlayerQuests(ulong playerId)
    {
        var playerQuestRepository = _provider.GetRequiredService<IPlayerQuestRepository>();

        var result = (await playerQuestRepository.GetAllByStatusAsync(playerId, [QuestStatus.InProgress])).ToList();

        return Ok(new PlayerQuestPanelViewModel(result));
    }

    [HttpPost]
    [Route("giver")]
    public async Task<IActionResult> Generate([FromBody] GenerateQuestsRequest request)
    {
        var factionRepository = _provider.GetRequiredService<IFactionRepository>();
        var playerQuestRepository = _provider.GetRequiredService<IPlayerQuestRepository>();
        var factionMap = (await factionRepository.GetAllAsync())
            .ToDictionary(
                k => k.Id,
                v => v
            );

        if (!factionMap.TryGetValue(request.FactionId, out var faction))
        {
            return BadRequest("Invalid Faction");
        }

        var playerQuestsMap = (await playerQuestRepository.GetAllByStatusAsync(request.PlayerId, QuestStatus.Any()))
            .ToDictionary(
                k => k.OriginalQuestId,
                v => true
            );
        
        var quests = await _proceduralQuestGeneratorService
            .Generate(
                request.PlayerId,
                request.FactionId,
                request.TerritoryId,
                request.Seed
            );

        return Ok(
            new QuestPanelViewModel(
                request.ConstructId,
                faction.Id,
                faction.Name,
                request.TerritoryId,
                quests,
                playerQuestsMap
            )
        );
    }

    [Route("calculate/industry-package")]
    [HttpPost]
    public async Task<IActionResult> CalculateIndustryDeliveryContainer([FromBody] CalculateIndustryDeliveryPackageRequest request)
    {
        var bank = _provider.GetGameplayBank();
        var marketOrderRepository = _provider.GetRequiredService<IMarketOrderRepository>();

        var response = new CalculateIndustryDeliveryPackageResponse();
        
        foreach (var item in request.Items)
        {
            var def = bank.GetDefinition(item.ItemName);
            if (def == null) continue;
            var obj = bank.GetBaseObject<BaseItem>(def.Id);
            if (obj == null) continue;

            response.TotalMass += obj.UnitMass * item.Quantity;
            response.TotalVolume += obj.UnitVolume * item.Quantity;

            var unitPrice = await marketOrderRepository.GetAveragePriceOfItemAsync(def.Id);
            response.QuantaReward += Math.Clamp(unitPrice, 100000, long.MaxValue) * item.Quantity;
        }

        response.ItemRewardQuantity = response.QuantaReward / request.ItemRewardPrice;
        response.QuantaReward *= request.QuantaRewardFactor;

        return Ok(response);
    }

    public class CalculateIndustryDeliveryPackageResponse
    {
        public double TotalVolume { get; set; }
        public double TotalMass { get; set; }
        public double QuantaReward { get; set; }
        public double ItemRewardQuantity { get; set; }
    }

    public class CalculateIndustryDeliveryPackageRequest
    {
        public double QuantaRewardFactor { get; set; } = 0.25d;
        public double ItemRewardPrice { get; set; } = 500000;
        public List<ItemQuantity> Items { get; set; } = [];

        public struct ItemQuantity
        {
            public string ItemName { get; set; }
            public long Quantity { get; set; }
        }
    }

    public class QuestInteractRequest
    {
        public ulong PlayerId { get; set; }
        public ulong ConstructId { get; set; }
        public ulong? ElementId { get; set; }
    }

    public class QuestInteractResponse(IEnumerable<QuestInteractionOutcome> outcomes)
    {
        public IEnumerable<QuestInteractionOutcome> Outcomes { get; } = outcomes;
    }
    
    public class SetupTerritoryContainerRequest
    {
        public Guid TerritoryId { get; set; }
        public ulong ConstructId { get; set; }
        public ulong? ElementId { get; set; }
    }
    
    public class AcceptQuestRequest
    {
        public Guid QuestId { get; set; }
        public ulong PlayerId { get; set; }
        public long FactionId { get; set; }
        public Guid TerritoryId { get; set; }
        public int Seed { get; set; }
    }

    public class AbandonQuestRequest
    {
        public Guid QuestId { get; set; }
        public ulong PlayerId { get; set; }
    }

    public class AcceptQuestResponse(bool isSuccess, string message) : IOutcome
    {
        public bool Success { get; } = isSuccess;
        public string Message { get; } = message;

        public static AcceptQuestResponse Accepted(string message) => new(true, message);
        public static AcceptQuestResponse Failed(string message) => new(false, message);
    }
    
    public class AbandonQuestResponse(bool isSuccess) : IOutcome
    {
        public bool Success { get; } = isSuccess;
        public string Message { get; init; }

        public static AbandonQuestResponse Abandoned() => new(true);
        public static AbandonQuestResponse Failed(string message) => new(false){Message = message};
    }

    public class CompleteQuestTaskRequest
    {
        public ulong ConstructId { get; set; }
        public ulong ElementId { get; set; }
        public ulong PlayerId { get; set; }
    }

    public class PlayerQuestPanelViewModel
    {
        public IEnumerable<QuestViewModel> Jobs { get; set; }

        public PlayerQuestPanelViewModel(IEnumerable<PlayerQuestItem> questItems)
        {
            Jobs = questItems
                .Select(pq => new QuestViewModel(pq))
                .OrderBy(q => q.Title);
        }
    }

    public class QuestPanelViewModel
    {
        public ulong ConstructId { get; }
        public long FactionId { get; set; }
        public Guid Territory { get; set; }
        public string Faction { get; set; }
        public IEnumerable<QuestViewModel> Jobs { get; set; }

        public QuestPanelViewModel(
            ulong constructId,
            long factionId,
            string factionName,
            Guid territory,
            GenerateQuestListOutcome outcome,
            Dictionary<Guid, bool> acceptedMap)
        {
            ConstructId = constructId;
            FactionId = factionId;
            Faction = factionName;
            Territory = territory;
            Jobs = outcome.QuestList
                .Select(pq => new QuestViewModel(pq, acceptedMap.TryGetValue(pq.Id, out var accepted) && accepted))
                .OrderBy(q => q.Title);
        }
    }

    public class QuestViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public bool Accepted { get; set; }
        public bool Safe { get; set; }
        public long QuantaReward { get; set; }

        public IEnumerable<QuestTaskViewModel> Tasks { get; set; }
        public IEnumerable<string> Rewards { get; set; }

        public QuestViewModel(ProceduralQuestItem item, bool accepted)
        {
            Id = item.Id;
            Title = item.Title;
            Type = item.Type;
            Tasks = item.TaskItems.Select(t => new QuestTaskViewModel(t));
            Rewards = item.Properties.RewardTextList;
            Accepted = accepted;
            Safe = item.Safe;
            QuantaReward = item.Properties.QuantaReward;
        }

        public QuestViewModel(PlayerQuestItem item)
        {
            Id = item.Id;
            Title = item.Properties.Title;
            Type = item.Type;
            Tasks = item.TaskItems.Select(t => new QuestTaskViewModel(t));
            Rewards = item.Properties.RewardTextList;
            Accepted = true;
        }
    }

    public class QuestTaskViewModel(QuestTaskItem questTaskItem)
    {
        public string Title { get; set; } = questTaskItem.Text;

        public string Position { get; set; } =
            $"::pos{{0,{questTaskItem.BaseConstruct ?? 0},{questTaskItem.Position.x}, {questTaskItem.Position.y}, {questTaskItem.Position.z}}}";

        public string Status { get; set; } = questTaskItem.Status;

        public IEnumerable<QuestItemQuantityViewModel> Items { get; set; } = questTaskItem.Definition
            .Items.Select(x => new QuestItemQuantityViewModel(x.Quantity, x.DisplayName));
    }

    public class QuestItemQuantityViewModel(double quantity, string name)
    {
        public double Quantity { get; } = Math.Abs(quantity);
        public string Name { get; } = name;
    }

    public class GenerateQuestsRequest
    {
        public ulong PlayerId { get; set; }
        public ulong ConstructId { get; set; }
        public long FactionId { get; set; }
        public Guid TerritoryId { get; set; }
        public int Seed { get; set; }
    }
}