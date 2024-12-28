using System;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Data;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Interfaces;
using Newtonsoft.Json.Linq;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Services;

public class SkillFactory(IServiceProvider provider) : ISkillFactory
{
    public ISkill Create(object item)
    {
        var jObj = JObject.FromObject(item);
        var si = jObj.ToObject<SkillItem>();
        
        switch (si.Name)
        {
            case "jammer":
                return JamTargetSkill.Create(provider, si);
            case "stasis":
                return StasisTargetSkill.Create(si);
            case "leash":
                return LeashSkill.Create(si);
            case "script":
                return RunScriptSkill.Create(jObj);
            case "wave":
                return WaveScriptSkill.Create(jObj);
            case "waypoint":
                return WaypointSkill.Create(jObj);
            case "roam-asteroids":
                return AsteroidRoamSkill.Create(jObj);
            default:
                return new NullSkill();
        }
    }
}