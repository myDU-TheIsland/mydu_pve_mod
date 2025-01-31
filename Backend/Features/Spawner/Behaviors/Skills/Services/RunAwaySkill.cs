using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Data;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NQ;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Services;

public class RunAwaySkill(RunAwaySkill.RunAwaySkillItem skillItem) : BaseSkill(skillItem)
{
    public override bool CanUse(BehaviorContext context)
    {
        return Active && context.IsAlive &&
               !context.Effects.IsEffectActive<CooldownEffect>();
    }

    public override async Task Use(BehaviorContext context)
    {
        var provider = context.Provider;
        var areaScanService = provider.GetRequiredService<IAreaScanService>();
        
        var targetConstructId = context.GetTargetConstructId();
        if (!context.Position.HasValue) return;

        if (skillItem.TriggerOnPlayerContact)
        {
            var contacts = await areaScanService.ScanForPlayerContacts(
                context.ConstructId, 
                context.Position.Value, 
                skillItem.ScanRange);
            
            if (!contacts.Any()) return;
        }
        
        Vec3 direction;
        if (targetConstructId != null)
        {
            direction = (context.Position.Value - context.TargetMovePosition).NormalizeSafe();
        }
        else
        {
            direction = context.Position.Value.NormalizeSafe();
        }

        context.SetOverrideTargetMovePosition(context.Position + direction * skillItem.MovePositionDistance);
        context.Effects.Activate<CooldownEffect>(TimeSpan.FromSeconds(skillItem.CooldownSeconds));
    }

    public static RunAwaySkill Create(JToken jObj)
    {
        return new RunAwaySkill(jObj.ToObject<RunAwaySkillItem>());
    }

    public class CooldownEffect : IEffect;

    public class RunAwaySkillItem: SkillItem
    {
        [JsonProperty] public double MovePositionDistance { get; set; } = DistanceHelpers.OneSuInMeters * 20;
        [JsonProperty] public bool TriggerOnPlayerContact { get; set; }
        [JsonProperty] public float ScanRange { get; set; } = DistanceHelpers.OneSuInMeters * 4;
    }
}