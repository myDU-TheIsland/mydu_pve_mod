using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Common.Interfaces;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Extensions;
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
    public bool Triggered { get; set; }
    public DateTime? LastContactDateTime { get; set; }
    public Vec3? Direction { get; set; } = new Vec3 { z = 1 };
    
    public override bool CanUse(BehaviorContext context)
    {
        return Active && context.IsAlive &&
               !context.Effects.IsEffectActive<CooldownEffect>();
    }

    public override async Task Use(BehaviorContext context)
    {
        var provider = context.Provider;
        var areaScanService = provider.GetRequiredService<IAreaScanService>();
        var dateTimeProvider = provider.GetRequiredService<IDateTimeProvider>();
        
        var targetConstructId = context.GetTargetConstructId();
        if (!context.Position.HasValue) return;

        var contacts = (await areaScanService.ScanForPlayerContacts(
            context.ConstructId, 
            context.Position.Value, 
            skillItem.ScanRange)).ToList();
        
        if (!Triggered && skillItem.TriggerOnPlayerContact)
        {
            if (contacts.Count == 0) return;

            Triggered = true;
        }
        
        if (targetConstructId != null)
        {
            Direction = (context.Position.Value - context.TargetMovePosition).NormalizeSafe();
        }

        context.SetOverrideTargetMovePosition(context.Position + Direction * skillItem.MovePositionDistance);
        context.Effects.Activate<CooldownEffect>(TimeSpan.FromSeconds(skillItem.CooldownSeconds));

        var now = dateTimeProvider.UtcNow();
        
        if (contacts.Count > 0)
        {
            LastContactDateTime = now;
        }

        if (!LastContactDateTime.HasValue) return;
        
        var deltaTime = now - LastContactDateTime.Value;
        if (deltaTime > TimeSpan.FromMinutes(skillItem.EscapeMinTimeMinutes))
        {
            var scriptAction = provider.GetScriptAction(skillItem.OnEscapeScript);
            var scriptContext = context.GetScriptContext();
            await scriptAction.ExecuteAsync(scriptContext);

            Active = false;
        }
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
        [JsonProperty] public double ScanRange { get; set; } = DistanceHelpers.OneSuInMeters * 4;
        [JsonProperty] public double EscapeMinTimeMinutes { get; set; } = 1;
        [JsonProperty] public ScriptActionItem OnEscapeScript { get; set; } = new()
        {
            Type = "delete"
        };
    }
}