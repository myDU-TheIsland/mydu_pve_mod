using System;
using System.Threading.Tasks;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Data;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Helpers;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Services;

public class LeashSkill : ISkill
{
    public bool CanUse(BehaviorContext context) => !context.Effects.IsEffectActive<LeashCooldown>();

    public bool ShouldUse(BehaviorContext context) => true;

    public Task Use(BehaviorContext context)
    {
        if (!context.StartPosition.HasValue) return Task.CompletedTask;

        var leashPos = context.Sector;
        const long leashRange = DistanceHelpers.OneSuInMeters * 5;
        var iAmFar = context.Position.HasValue &&
                    context.Position.Value.Dist(leashPos) > leashRange;
        var targetIsFar = context.GetTargetConstructId().HasValue &&
                          context.TargetPosition.Dist(leashPos) > leashRange;
        var isReturningCooldown = context.Effects.IsEffectActive<ReturningToSectorCooldown>();

        if (iAmFar || targetIsFar)
        {
            context.SetOverrideTargetMovePosition(context.StartPosition.Value);
            context.Effects.Activate<ReturningToSectorCooldown>(TimeSpan.FromMinutes(2));
        }
        else if (!isReturningCooldown)
        {
            context.SetOverrideTargetMovePosition(null);
        }
        
        context.Effects.Activate<LeashCooldown>(TimeSpan.FromSeconds(1));

        return Task.CompletedTask;
    }

    public static LeashSkill Create(SkillItem item)
    {
        return new LeashSkill();
    }
    
    public class ReturningToSectorCooldown : IEffect;
    public class LeashCooldown : IEffect;
}