using System;
using System.Threading.Tasks;
using Backend;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Data;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Helpers;
using NQutils.Def;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Services;

public class StasisTargetSkill : ISkill
{
    public required string? ItemTypeName { get; set; }
    public required TimeSpan Cooldown { get; set; }

    public bool CanUse(BehaviorContext context)
    {
        return !context.Effects.IsEffectActive<CooldownEffect>();
    }

    public bool ShouldUse(BehaviorContext context)
    {
        return context.TargetConstructId.HasValue;
    }

    public async Task Use(BehaviorContext context)
    {
        if (!context.TargetConstructId.HasValue) return;

        var constructService = context.Provider.GetRequiredService<IConstructService>();
        var bank = context.Provider.GetGameplayBank();

        var speedConfig = bank.GetBaseObject<ConstructSpeedConfig>();
        var totalMass = await constructService.GetConstructTotalMass(context.TargetConstructId.Value);

        var stasis = bank.GetDefinition(ItemTypeName ?? "StasisWeaponMedium");

        if (stasis?.BaseObject is not StasisWeaponUnit stasisWeaponUnit)
        {
            return;
        }

        var maxRange = stasisWeaponUnit.RangeMax;
        var distance = context.TargetDistance;

        if (totalMass <= speedConfig.heavyConstructMass)
        {
            var num2 = (stasisWeaponUnit.RangeMin - stasisWeaponUnit.RangeMax) /
                       (1.0 - 1.0 / (stasisWeaponUnit.RangeCurvature + 1.0));
            maxRange = stasisWeaponUnit.RangeMin - num2 +
                       num2 / (stasisWeaponUnit.RangeCurvature * totalMass / speedConfig.heavyConstructMass + 1.0);
        }

        context.Effects.Activate<CooldownEffect>(Cooldown);

        if (distance > maxRange * 3.0)
        {
            // miss
            return;
        }

        var strength = Math.Pow(0.5, Math.Max(distance - maxRange, 0.0) / maxRange) * stasisWeaponUnit.effectStrength;
        var duration = stasisWeaponUnit.effectDuration;

        await constructService.ApplyStasisEffect(
            context.TargetConstructId.Value,
            strength,
            duration
        );
    }

    public static StasisTargetSkill Create(SkillItem item)
    {
        return new StasisTargetSkill
        {
            Cooldown = TimeSpan.FromSeconds(item.CooldownSeconds),
            ItemTypeName = item.ItemTypeName
        };
    }

    public class CooldownEffect : IEffect;
}