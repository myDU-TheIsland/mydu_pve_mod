using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Extensions;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NQ;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Services;

public class WaypointSkill(WaypointSkill.WaypointSkillItem skillItem) : ISkill
{
    public bool WaypointInitialized { get; set; }
    public Queue<Vec3> WaypointQueue { get; set; } = new(skillItem.Waypoints);

    public bool CanUse(BehaviorContext context)
    {
        return true;
    }

    public bool ShouldUse(BehaviorContext context)
    {
        return true;
    }

    public async Task Use(BehaviorContext context)
    {
        if (!context.Position.HasValue) return;

        if (!WaypointInitialized)
        {
            var closestWp = skillItem.Waypoints.MinBy(wp => wp.Dist(context.Position.Value));
            foreach (var wp in WaypointQueue.ToList())
            {
                var distance = wp.Dist(closestWp);
                if (Math.Abs(distance) <= skillItem.WaypointArrivalDistance)
                {
                    break;
                }
                
                WaypointQueue.Dequeue();
            }

            WaypointInitialized = true;
        }

        if (!context.Contacts.IsEmpty && skillItem.InterruptWaypointNavigationOnPlayerContact)
        {
            context.SetOverrideTargetMovePosition(null);
            return;
        }
        
        if (WaypointQueue.Count == 0)
        {
            return;
        }

        var nextWaypoint = WaypointQueue.Peek();
        if (context.Position.Value.Dist(nextWaypoint) < skillItem.WaypointArrivalDistance)
        {
            WaypointQueue.Dequeue();

            var arrivedAtFinalDestination = WaypointQueue.Count == 0;
            if (arrivedAtFinalDestination)
            {
                var scriptAction = context.Provider.GetScriptAction(skillItem.ArrivedAtFinalDestinationScript);

                await scriptAction.ExecuteAsync(new ScriptContext(
                    context.Provider,
                    context.FactionId,
                    context.PlayerIds,
                    context.Sector,
                    context.TerritoryId)
                {
                    ConstructId = context.ConstructId
                });
            }
        }

        context.SetOverrideTargetMovePosition(nextWaypoint);
    }
    
    public static WaypointSkill Create(JObject item)
    {
        return new WaypointSkill(item.ToObject<WaypointSkillItem>());
    }

    public class WaypointSkillItem
    {
        [JsonProperty] public IEnumerable<Vec3> Waypoints { get; set; } = [];
        [JsonProperty] public double WaypointArrivalDistance { get; set; } = 50000;
        [JsonProperty] public IEnumerable<ScriptActionItem> ArrivedAtFinalDestinationScript { get; set; } = [];
        [JsonProperty] public bool InterruptWaypointNavigationOnPlayerContact { get; set; } = true;
    }
}