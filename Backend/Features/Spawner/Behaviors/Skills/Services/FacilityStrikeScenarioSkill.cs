using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Extensions;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Data;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Data;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Services;

public class FacilityStrikeScenarioSkill(
    FacilityStrikeScenarioSkill.FacilityStrikeScenarioSkillItem skillItem,
    ProduceLootWhenSafeSkill produceLootSkill
) : BaseSkill(skillItem)
{
    public FacilityStrikeState? State { get; set; }

    public override bool CanUse(BehaviorContext context)
    {
        return (State is null or { Finished: false } || !produceLootSkill.Finished) && base.CanUse(context);
    }

    public override bool ShouldUse(BehaviorContext context) =>
        !context.Effects.IsEffectActive<UseCooldownEffect>() && base.ShouldUse(context);

    public override async Task Use(BehaviorContext context)
    {
        var provider = context.Provider;
        var position = context.Position ?? context.Sector;
        
        context.Effects.Activate<UseCooldownEffect>(TimeSpan.FromSeconds(skillItem.CooldownSeconds));

        if (!skillItem.Waves.Any()) return;

        var constructStateService = context.Provider.GetRequiredService<IConstructStateService>();

        await LoadState(context, constructStateService);
        if (produceLootSkill.CanUse(context) && produceLootSkill.ShouldUse(context))
        {
            await produceLootSkill.Use(context);
        }

        var waves = skillItem.Waves.ToList();
        if (waves.Count < State!.CurrentWaveIndex + 1)
        {
            State.Finished = true;
            if (produceLootSkill.Finished)
            {
                await FinishScenario(context);
            }

            await PersistState(context, constructStateService);

            return;
        }

        var wave = waves[State!.CurrentWaveIndex];

        if (context.Effects.IsEffectActive<NextWaveCooldownEffect>()) return;

        if (skillItem.NewWaveOnlyWhenClear)
        {
            var areaScanService = provider.GetRequiredService<IAreaScanService>();
            var contacts = (await areaScanService.ScanForNpcConstructs(position, skillItem.AreScanRange))
                .Select(x => x.ConstructId)
                .ToHashSet();

            contacts.Remove(context.ConstructId);

            if (contacts.Count != 0) return;

            if (!context.Effects.IsEffectActive<NextWaveCooldownEffect>())
            {
                context.Effects.Activate<NextWaveCooldownEffect>(TimeSpan.FromSeconds(wave.NextWaveCooldown));
                return;
            }
        }

        context.Effects.Activate<NextWaveCooldownEffect>(TimeSpan.FromSeconds(wave.NextWaveCooldown));
        State.CurrentWaveIndex++;

        await PersistState(context, constructStateService);

        var scriptAction = context.Provider.GetScriptAction(wave.Script);
        await scriptAction.ExecuteAsync(new ScriptContext(
            context.Provider,
            context.FactionId,
            context.PlayerIds,
            context.Sector,
            context.TerritoryId).WithConstructId(context.ConstructId));
    }

    private async Task PersistState(BehaviorContext context, IConstructStateService constructStateService)
    {
        if (State == null) return;

        await constructStateService.PersistState(new ConstructStateItem
        {
            Properties = JToken.FromObject(State),
            Type = nameof(FacilityStrikeState),
            ConstructId = context.ConstructId
        });
    }

    private async Task FinishScenario(BehaviorContext context)
    {
        await context.Provider.GetScriptAction(skillItem.OnFinishedScript)
            .ExecuteAsync(new ScriptContext(
                context.Provider,
                context.FactionId,
                context.PlayerIds,
                context.Sector,
                context.TerritoryId).WithConstructId(context.ConstructId));
    }

    private async Task LoadState(BehaviorContext context, IConstructStateService constructStateService)
    {
        if (State == null)
        {
            var outcome = await constructStateService.Find(nameof(FacilityStrikeState), context.ConstructId);

            State = new FacilityStrikeState();

            if (outcome.Success)
            {
                State = outcome.StateItem?.Properties?.ToObject<FacilityStrikeState>();
            }
        }
    }

    public static FacilityStrikeScenarioSkill Create(JToken jObj)
    {
        var facilityStrikeSkillItem = jObj.ToObject<FacilityStrikeScenarioSkillItem>();

        return new FacilityStrikeScenarioSkill(
            facilityStrikeSkillItem,
            ProduceLootWhenSafeSkill.Create(jObj[nameof(FacilityStrikeScenarioSkillItem.Production)])
        );
    }

    public class FacilityStrikeState
    {
        [JsonProperty] public bool Finished { get; set; }
        [JsonProperty] public int CurrentWaveIndex { get; set; }
    }

    public class NextWaveCooldownEffect : IEffect;

    public class UseCooldownEffect : IEffect;

    public class FacilityStrikeScenarioSkillItem : SkillItem
    {
        [JsonProperty] public ProduceLootWhenSafeSkill.ProduceLootWhenSafe? Production { get; set; }
        [JsonProperty] public IEnumerable<WaveItem> Waves { get; set; } = [];
        [JsonProperty] public IEnumerable<ScriptActionItem> OnFinishedScript { get; set; } = [];
        [JsonProperty] public double AreScanRange { get; set; } = DistanceHelpers.OneSuInMeters * 3D;
        [JsonProperty] public bool NewWaveOnlyWhenClear { get; set; } = true;

        public class WaveItem
        {
            [JsonProperty] public IEnumerable<ScriptActionItem> Script { get; set; } = [];
            [JsonProperty] public double NextWaveCooldown { get; set; } = 60D;
        }
    }
}