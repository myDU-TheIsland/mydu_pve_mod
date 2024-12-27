using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Data;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Services;

public class WaveScriptSkill(WaveScriptSkill.WaveScriptSkillItem skillItem) : ISkill
{
    public int CurrentCycle { get; set; } = 0;

    public bool CanUse(BehaviorContext context)
    {
        return CurrentCycle < skillItem.CycleCount &&
               !context.Effects.IsEffectActive<CooldownEffect>();
    }

    public bool ShouldUse(BehaviorContext context)
    {
        return true;
    }

    public Task Use(BehaviorContext context)
    {
        context.Effects.Activate<CooldownEffect>(TimeSpan.FromSeconds(skillItem.CooldownSeconds));
        CurrentCycle++;

        var provider = context.Provider;
        var actionFactory = provider.GetRequiredService<IScriptActionFactory>();
        var scriptAction = actionFactory.Create(skillItem.Script);

        scriptAction.ExecuteAsync(new ScriptContext(
            provider,
            context.FactionId,
            context.PlayerIds,
            context.Sector,
            context.TerritoryId)
        {
            ConstructId = context.ConstructId
        });

        return Task.CompletedTask;
    }

    public static WaveScriptSkill Create(JObject item)
    {
        return new WaveScriptSkill(item.ToObject<WaveScriptSkillItem>());
    }

    public class CooldownEffect : IEffect;

    public class WaveScriptSkillItem : SkillItem
    {
        [JsonProperty] public int CycleCount { get; set; } = 3;
        [JsonProperty] public IEnumerable<ScriptActionItem> Script { get; set; } = [];
    }
}