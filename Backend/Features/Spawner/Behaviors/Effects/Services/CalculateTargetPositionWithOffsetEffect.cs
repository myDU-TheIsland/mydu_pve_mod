using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Common.Interfaces;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;
using Mod.DynamicEncounters.Helpers;
using NQ;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Services;

public class CalculateTargetPositionWithOffsetEffect(IServiceProvider provider) : ICalculateTargetMovePositionEffect
{
    public async Task<Vec3?> GetTargetMovePosition(ICalculateTargetMovePositionEffect.Params @params)
    {
        if (!@params.TargetConstructId.HasValue ||
            !@params.InstigatorPosition.HasValue ||
            !@params.InstigatorStartPosition.HasValue)
        {
            return null;
        }

        var constructService = provider.GetRequiredService<IConstructService>();
        var logger = provider.CreateLogger<CalculateTargetPositionWithOffsetEffect>();

        var targetConstructTransformOutcome =
            await constructService.GetConstructTransformAsync(@params.TargetConstructId.Value);
        if (!targetConstructTransformOutcome.ConstructExists)
        {
            logger.LogError(
                "Construct {Construct} Target construct info {Target} is null",
                @params.InstigatorConstructId,
                @params.TargetConstructId.Value
            );

            return @params.InstigatorStartPosition.Value;
        }

        var targetPos = targetConstructTransformOutcome.Position;

        var distanceFromTarget = (targetPos - @params.InstigatorPosition.Value).Size();
        if (distanceFromTarget > @params.MaxDistanceVisibility)
        {
            return @params.InstigatorStartPosition.Value;
        }

        var distanceGoal = @params.TargetDistance;

        var random = provider.GetRequiredService<IRandomProvider>().GetRandom();
        var offset = random.RandomDirectionVec3() * distanceGoal;

        return targetPos + offset;
    }
}