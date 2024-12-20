using System;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Data;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Interfaces;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Services;

public class SkillFactory(IServiceProvider provider) : ISkillFactory
{
    public ISkill Create(SkillItem item)
    {
        switch (item.Name)
        {
            case "jammer":
                return JamTargetSkill.Create(provider, item);
            case "stasis":
                return StasisTargetSkill.Create(item);
            case "leash":
                return LeashSkill.Create(item);
            default:
                return new NullSkill();
        }
    }
}