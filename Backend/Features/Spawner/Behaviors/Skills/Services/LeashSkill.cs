using System.Threading.Tasks;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Data;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Features.Spawner.Extensions;
using Mod.DynamicEncounters.Helpers;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Services;

public class LeashSkill : ISkill
{
    public bool CanUse(BehaviorContext context)
    {
        return true;
    }

    public bool ShouldUse(BehaviorContext context)
    {
        return context.Position.HasValue && 
               context.Position.Value.Dist(context.Sector) > DistanceHelpers.OneSuInMeters * 5;
    }

    public Task Use(BehaviorContext context)
    {
        if (!context.StartPosition.HasValue) return Task.CompletedTask;
        
        context.SetAutoTargetMovePosition(context.StartPosition.Value);
        
        return Task.CompletedTask;
    }
    
    public static LeashSkill Create(SkillItem item)
    {
        return new LeashSkill();
    }
}