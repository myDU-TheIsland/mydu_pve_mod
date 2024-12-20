using System.Threading.Tasks;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Data;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Helpers;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Services;

public class LeashSkill : ISkill
{
    public bool CanUse(BehaviorContext context) => true;

    public bool ShouldUse(BehaviorContext context) => true;

    public Task Use(BehaviorContext context)
    {
        if (!context.StartPosition.HasValue) return Task.CompletedTask;

        var isFar = context.Position.HasValue &&
                    context.Position.Value.Dist(context.Sector) > DistanceHelpers.OneSuInMeters * 5;

        if (isFar)
        {
            context.SetOverrideTargetMovePosition(context.StartPosition.Value);
        }
        else
        {
            context.SetOverrideTargetMovePosition(null);
        }

        return Task.CompletedTask;
    }

    public static LeashSkill Create(SkillItem item)
    {
        return new LeashSkill();
    }
}