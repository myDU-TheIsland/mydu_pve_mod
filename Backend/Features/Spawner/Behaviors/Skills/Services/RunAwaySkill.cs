﻿using System;
using System.Threading.Tasks;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Data;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NQ;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Services;

public class RunAwaySkill(RunAwaySkill.RetreatSkillItem skillItem) : ISkill
{
    public bool CanUse(BehaviorContext context)
    {
        return context.IsAlive &&
               !context.Effects.IsEffectActive<CooldownEffect>();
    }

    public virtual bool ShouldUse(BehaviorContext context)
    {
        return true;
    }

    public Task Use(BehaviorContext context)
    {
        var targetConstructId = context.GetTargetConstructId();
        if (!context.Position.HasValue) return Task.CompletedTask;

        Vec3 direction;
        if (targetConstructId != null)
        {
            direction = (context.Position.Value - context.TargetMovePosition).NormalizeSafe();
        }
        else
        {
            direction = context.Position.Value.NormalizeSafe();
        }

        context.SetOverrideTargetMovePosition(context.Position + direction * skillItem.MovePositionDistance);
        context.Effects.Activate<CooldownEffect>(TimeSpan.FromSeconds(skillItem.CooldownSeconds));

        return Task.CompletedTask;
    }

    public static RunAwaySkill Create(JToken jObj)
    {
        return new RunAwaySkill(jObj.ToObject<RetreatSkillItem>());
    }

    public class CooldownEffect : IEffect;

    public class RetreatSkillItem : SkillItem
    {
        [JsonProperty] public double MovePositionDistance { get; set; } = DistanceHelpers.OneSuInMeters * 20;
    }
}