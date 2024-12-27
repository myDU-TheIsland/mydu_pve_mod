namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Interfaces;

public interface ISkillFactory
{
    ISkill Create(object item);
}