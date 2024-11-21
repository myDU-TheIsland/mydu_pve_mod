using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Services;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Helpers;
using NQ;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Services;

public class PIDMovementEffect : IMovementEffect
{
    public static double Kp { get; set; } = 0.2d; 
    public static double Kd { get; set; } = 0.3d; 
    public static double Ki { get; set; } = 0d; 
    
    public IMovementEffect.Outcome Move(IMovementEffect.Params @params, BehaviorContext context)
    {
        var deltaTime = @params.DeltaTime;
        var npcVelocity = @params.Velocity;
        var npcPosition = @params.Position;
        
        var pid = new PIDController(Kp, Ki, Kd);

        // Compute desired acceleration using PID
        var desiredAcceleration = pid.Compute(@params.Position, @params.TargetPosition, deltaTime);

        // Clamp the acceleration to the max allowable value
        desiredAcceleration = desiredAcceleration.ClampToSize(@params.MaxAcceleration);

        // Update NPC velocity based on acceleration
        npcVelocity = new Vec3
        {
            x = npcVelocity.x + desiredAcceleration.x * deltaTime,
            y = npcVelocity.y + desiredAcceleration.y * deltaTime,
            z = npcVelocity.z + desiredAcceleration.z * deltaTime
        };

        // Clamp velocity to the maximum speed
        npcVelocity = npcVelocity.ClampToSize(@params.MaxVelocity);

        // Update NPC position based on velocity
        npcPosition = new Vec3
        {
            x = npcPosition.x + npcVelocity.x * deltaTime,
            y = npcPosition.y + npcVelocity.y * deltaTime,
            z = npcPosition.z + npcVelocity.z * deltaTime
        };

        return new IMovementEffect.Outcome
        {
            Position = npcPosition,
            Velocity = npcVelocity
        };
    }
}