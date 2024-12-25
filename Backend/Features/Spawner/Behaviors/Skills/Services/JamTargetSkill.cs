using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Data;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Services;

public class JamTargetSkill : ISkill
{
    public required TimeSpan Cooldown { get; set; }
    public required IJamTargetService JamTargetService { get; set; }

    public bool CanUse(BehaviorContext context)
    {
        return !context.Effects.IsEffectActive<CooldownEffect>();
    }

    public bool ShouldUse(BehaviorContext context)
    {
        return context.HasTargetConstruct();
    }

    public async Task Use(BehaviorContext context)
    {
        if (!context.HasTargetConstruct()) return;

        await JamTargetService.JamAsync(new JamConstructCommand
        {
            InstigatorConstructId = context.ConstructId,
            TargetConstructId = context.GetTargetConstructId()!.Value
        });

        context.Effects.Activate<CooldownEffect>(Cooldown);
    }

    public static JamTargetSkill Create(IServiceProvider provider, SkillItem item)
    {
        return new JamTargetSkill
        {
            Cooldown = TimeSpan.FromSeconds(item.CooldownSeconds),
            JamTargetService = provider.GetRequiredService<IJamTargetService>(),
        };
    }

    public class CooldownEffect : IEffect;
}