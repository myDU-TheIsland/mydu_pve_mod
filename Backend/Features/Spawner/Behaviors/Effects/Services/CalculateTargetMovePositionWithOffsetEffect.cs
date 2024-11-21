using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;
using Mod.DynamicEncounters.Helpers;
using Mod.DynamicEncounters.Vector.Helpers;
using NQ;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Services;

public class CalculateTargetMovePositionWithOffsetEffect(IServiceProvider provider) : ICalculateTargetMovePositionEffect
{
    private readonly Random _random = provider.GetRandomProvider().GetRandom();
    
    private DateTime? LastTimeOffsetUpdated { get; set; }
    private Vec3 Offset { get; set; }
    
    public async Task<Vec3?> GetTargetMovePosition(ICalculateTargetMovePositionEffect.Params @params)
    {
        if (!@params.TargetConstructId.HasValue ||
            !@params.InstigatorPosition.HasValue ||
            !@params.InstigatorStartPosition.HasValue)
        {
            return null;
        }

        var constructService = provider.GetRequiredService<IConstructService>();
        var logger = provider.CreateLogger<CalculateTargetMovePositionWithOffsetEffect>();

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

        var distanceGoal = @params.TargetMoveDistance;

        var timeDiff = DateTime.UtcNow - (LastTimeOffsetUpdated ?? DateTime.UtcNow);
        if (LastTimeOffsetUpdated == null || timeDiff > TimeSpan.FromSeconds(30))
        {
            Offset = _random.RandomDirectionVec3() * distanceGoal;
            LastTimeOffsetUpdated = DateTime.UtcNow;
        }

        // var velocities = await constructService.GetConstructVelocities(@params.TargetConstructId.Value);
        // var targetVelocity = velocities.Linear * @params.DeltaTime; // vel per second
        //
        // var futurePosition = VelocityHelper.CalculateFuturePosition(
        //     targetPos,
        //     targetVelocity,
        //     10
        // );

        return targetPos + Offset;
        // return futurePosition;
    }
}