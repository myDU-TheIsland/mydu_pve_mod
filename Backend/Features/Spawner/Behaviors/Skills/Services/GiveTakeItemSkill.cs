using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Common.Services;
using Mod.DynamicEncounters.Features.Loot.Data;
using Mod.DynamicEncounters.Features.Loot.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Extensions;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Data;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Services;

public class GiveTakeItemSkill(GiveTakeItemSkill.GiveTakeItemSkillItem skillItem) : ISkill
{
    public int CurrentIteration { get; set; }
    public ulong? OverrideConstructId { get; set; }
    public bool Finished { get; set; }
    
    public virtual bool CanUse(BehaviorContext context)
    {
        return !context.Effects.IsEffectActive<CooldownEffect>() && !Finished;
    }

    public virtual bool ShouldUse(BehaviorContext context)
    {
        return true;
    }

    public virtual async Task Use(BehaviorContext context)
    {
        var provider = context.Provider;
        
        if (CurrentIteration >= skillItem.MaxIterations)
        {
            Finished = true;
            if (skillItem.OnFinishedScript.Any())
            {
                var scriptAction = provider.GetScriptAction(skillItem.OnFinishedScript);
                await scriptAction.ExecuteAsync(new ScriptContext(
                    provider,
                    context.FactionId,
                    context.PlayerIds,
                    context.Sector,
                    context.TerritoryId)
                {
                    ConstructId = context.ConstructId
                });
            }
            return;
        }
        
        context.Effects.Activate<CooldownEffect>(TimeSpan.FromSeconds(skillItem.CooldownSeconds));
        CurrentIteration++;

        var constructId = OverrideConstructId ?? context.ConstructId;
        
        
        // TODO change to give take operation
        var itemSpawnerService = provider.GetRequiredService<IItemSpawnerService>();
        await itemSpawnerService.SpawnItems(new SpawnItemOnRandomContainersCommand(
            constructId,
            new ItemBagData
            {
                Name = string.Empty,
                MaxBudget = 1,
                Tags = [],
                Entries = skillItem.Items
                    .Select(x => new ItemBagData.ItemAndQuantity(x.Item, new DefaultQuantity(x.Quantity)))
                    .ToList()
            }));

        if (!skillItem.SendPlayerAlert) return;
            
        var constructService = provider.GetRequiredService<IConstructService>();
        var info = await constructService.GetConstructInfoAsync(constructId);
        var pilot = info.Info?.mutableData.pilot;
        
        if (!pilot.HasValue) return;

        var alertService = provider.GetRequiredService<IPlayerAlertService>();
        await alertService.SendInfoAlert(pilot.Value, "Items beamed to your cargo hold");
    }

    public static GiveTakeItemSkill Create(JToken jObj)
    {
        return new GiveTakeItemSkill(jObj.ToObject<GiveTakeItemSkillItem>());
    }

    public class CooldownEffect : IEffect;

    public class GiveTakeItemSkillItem : SkillItem
    {
        [JsonProperty] public bool SendPlayerAlert { get; set; } = true;
        [JsonProperty] public int MaxIterations { get; set; } = 10;
        [JsonProperty] public IEnumerable<ScriptActionItem> OnFinishedScript { get; set; } = [];
        [JsonProperty] public IEnumerable<ItemQuantity> Items { get; set; } = [];
        
        public class ItemQuantity
        {
            public long Quantity { get; set; } = 1;
            public string Item { get; set; } = string.Empty;
        }
    }
}