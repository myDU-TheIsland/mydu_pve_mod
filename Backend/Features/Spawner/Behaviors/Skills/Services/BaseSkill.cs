using System.Threading.Tasks;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Data;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Services;

public abstract class BaseSkill(SkillItem skillItem) : ISkill
{
    protected bool Active { get; set; } = skillItem.Active;
    public virtual bool CanUse(BehaviorContext context) => Active && context.IsAlive;

    public virtual bool ShouldUse(BehaviorContext context) => Active;

    public abstract Task Use(BehaviorContext context);
}