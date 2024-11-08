using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;
using Mod.DynamicEncounters.Helpers;
using NQ;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Services;

public class DefaultCalculateTargetMovePositionEffectEffect(IServiceProvider provider) : ICalculateTargetMovePositionEffect
{
    public async Task<Vec3> GetTargetMovePosition(ICalculateTargetMovePositionEffect.Params @params)
    {
        if (!@params.TargetConstructId.HasValue)
        {
            return new Vec3();
        }

        var constructService = provider.GetRequiredService<IConstructService>();
        var logger = provider.CreateLogger<DefaultCalculateTargetMovePositionEffectEffect>();

        var targetConstructTransformOutcome =
            await constructService.GetConstructTransformAsync(@params.TargetConstructId.Value);
        if (!targetConstructTransformOutcome.ConstructExists)
        {
            logger.LogError(
                "Construct {Construct} Target construct info {Target} is null", 
                @params.InstigatorConstructId,
                @params.TargetConstructId.Value
            );
            return new Vec3();
        }

        var targetPos = targetConstructTransformOutcome.Position;

        var distanceGoal = @params.TargetDistance;
        var offset = new Vec3 { y = distanceGoal };

        return targetPos + offset;
    }
}