using Mod.DynamicEncounters.Common.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using NQ;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;

public interface IMovementEffect : IEffect
{
    Outcome Move(Params @params, BehaviorContext context);

    public class Params
    {
        public Vec3 Position { get; init; }
        public Vec3 TargetPosition { get; init; }
        public Vec3 Velocity { get; init; }
        public Vec3 Acceleration { get; init; }
        public double MaxVelocity { get; init; }
        public double DeltaTime { get; init; }
    }
    
    public class Outcome : IOutcome
    {
        public Vec3 Position { get; init; }
        public Vec3 Velocity { get; init; }
    }
}