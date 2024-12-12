using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Data;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Interfaces;

public interface ISkillFactory
{
    ISkill Create(SkillItem item);
}