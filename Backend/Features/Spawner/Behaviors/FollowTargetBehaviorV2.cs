using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using BotLib.Generated;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Common.Vector;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Features.Spawner.Extensions;
using Mod.DynamicEncounters.Helpers;
using NQ;
using NQutils.Exceptions;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors;

public class FollowTargetBehaviorV2(ulong constructId, IPrefab prefab) : IConstructBehavior
{
    private TimePoint _timePoint = new();

    private IConstructService _constructService;
    private ILogger<FollowTargetBehaviorV2> _logger;

    public BehaviorTaskCategory Category => BehaviorTaskCategory.MovementPriority;

    public Task InitializeAsync(BehaviorContext context)
    {
        var provider = context.ServiceProvider;
        
        _logger = provider.CreateLogger<FollowTargetBehaviorV2>();
        _constructService = provider.GetRequiredService<IConstructService>();

        return Task.CompletedTask;
    }

    public async Task TickAsync(BehaviorContext context)
    {
        if (!context.IsAlive)
        {
            return;
        }

        // first time initialize position
        if (!context.Position.HasValue)
        {
            var npcConstructTransformOutcome = await _constructService.GetConstructTransformAsync(constructId);
            if (!npcConstructTransformOutcome.ConstructExists)
            {
                return;
            }
            
            context.Position = npcConstructTransformOutcome.Position;
        }

        var npcPos = context.Position.Value;
        var targetMovePos = context.GetTargetMovePosition();

        var forward = VectorMathUtils.GetForward(context.Rotation.ToQuat())
            .ToNqVec3()
            .NormalizeSafe();
        var moveDirection = (targetMovePos - npcPos).NormalizeSafe();

        var velocityDirection = context.Velocity.NormalizeSafe();
        var velToTargetDot = velocityDirection.Dot(forward);

        context.TryGetProperty(BehaviorContext.EnginePowerProperty, out double enginePower, 1);
        
        if (enginePower <= 0)
        {
            var cUpdate = new ConstructUpdate
            {
                pilotId = ModBase.Bot.PlayerId,
                constructId = constructId,
                rotation = context.Rotation,
                position = npcPos,
                worldAbsoluteVelocity = new Vec3(),
                worldRelativeVelocity = new Vec3(),
                time = TimePoint.Now(),
                grounded = false,
            };

            _ = Task.Run(async () =>
            {
                var sw = new Stopwatch();
                sw.Start();
                
                try
                {
                    await ModBase.Bot.Req.ConstructUpdate(cUpdate);
                }
                catch (Exception e)
                {
                    ModBase.ServiceProvider.CreateLogger<FollowTargetBehaviorV2>()
                        .LogError(e, "Failed to send Construct Update Fire-And-Forget");
                }
                
                StatsRecorder.Record("ConstructUpdate", sw.ElapsedMilliseconds);
            });
            
            return;
        }
        
        var acceleration = prefab.DefinitionItem.AccelerationG * 9.81f * enginePower;
        
        if (velToTargetDot < 0)
        {
            acceleration *= 1 + Math.Abs(velToTargetDot);
        }
        
        // var brakingDistance = VelocityHelper.CalculateBrakingDistance(
        //     context.Velocity.Size(), 
        //     acceleration
        // );

        var accelForward = forward * acceleration * context.RealismFactor;
        var accelMove = moveDirection * acceleration * (1 - context.RealismFactor);

        var accelV = accelForward + accelMove;
        
        if (context.IsMoveModeWaypoint())
        {
            var shouldBrake = VelocityHelper.ShouldStartBraking(
                context.Position.Value.ToVector3(),
                context.GetTargetMovePosition().ToVector3(),
                context.Velocity.ToVector3(),
                acceleration
            );

            if (shouldBrake)
            {
                context.SetBraking(true);
            }
        }

        if (context.IsBraking())
        {
            // reverse accel to brake
            accelV = (context.Velocity.NormalizeSafe() * acceleration).Reverse();
        }

        var velocity = context.Velocity;
        
        var position = VelocityHelper.LinearInterpolateWithVelocity(
            npcPos,
            targetMovePos,
            ref velocity,
            accelV,
            prefab.DefinitionItem.MaxSpeedKph / 3.6d,
            context.DeltaTime
        );

        context.TryGetProperty(BehaviorContext.V0Property, out var v0, velocity);

        var deltaV = velocity - v0;
        var maxDeltaV = acceleration * context.DeltaTime;

        if (deltaV.Size() > maxDeltaV)
        {
            deltaV = deltaV.NormalizeSafe() * maxDeltaV;
            velocity = v0 + deltaV;
            position = npcPos + velocity * context.DeltaTime;
        }
        
        context.SetProperty(BehaviorContext.V0Property, velocity);
        
        context.Velocity = velocity;
        
        // context.Velocity = velocity;
        // Make the ship point to where it's accelerating
        var accelerationFuturePos = npcPos + moveDirection * 200000;

        var currentRotation = context.Rotation;
        var targetRotation = VectorMathUtils.SetRotationToMatchDirection(
            npcPos.ToVector3(),
            accelerationFuturePos.ToVector3()
        );

        var rotation = Quaternion.Slerp(
            currentRotation.ToQuat(),
            targetRotation,
            (float)(prefab.DefinitionItem.RotationSpeed * context.DeltaTime)
        );

        context.Rotation = rotation.ToNqQuat();

        _timePoint = TimePoint.Now();

        var velocityDisplay = (position - npcPos) / context.DeltaTime;
        
        try
        {
            context.Position = position;
            var cUpdate = new ConstructUpdate
            {
                pilotId = ModBase.Bot.PlayerId,
                constructId = constructId,
                rotation = context.Rotation,
                position = position,
                worldAbsoluteVelocity = velocityDisplay,
                worldRelativeVelocity = velocityDisplay,
                // worldAbsoluteAngVelocity = relativeAngularVel,
                // worldRelativeAngVelocity = relativeAngularVel,
                time = _timePoint,
                grounded = false,
            };
            
            _ = Task.Run(async () =>
            {
                var sw = new Stopwatch();
                sw.Start();

                try
                {
                    await ModBase.Bot.Req.ConstructUpdate(cUpdate);
                }
                catch (BusinessException bex)
                {
                    if (bex.error.code == ErrorCode.InvalidSession)
                    {
                        ConstructBehaviorContextCache.RaiseBotDisconnected();
                        ModBase.ServiceProvider
                            .CreateLogger<FollowTargetBehaviorV2>()
                            .LogError(bex, "Need to reconnect the Bot");
                    }
                }
                catch (Exception e)
                {
                    ModBase.ServiceProvider.CreateLogger<FollowTargetBehaviorV2>()
                        .LogError(e, "Failed to send Construct Update Fire-And-Forget");
                }
                
                StatsRecorder.Record("ConstructUpdate", sw.ElapsedMilliseconds);
            });
        }
        catch (BusinessException be)
        {
            _logger.LogError(be, "Failed to update construct transform. Attempting a restart of the bot connection.");

            try
            {
                await ModBase.Bot.Reconnect();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to Reconnect");
            }
        }
    }
}