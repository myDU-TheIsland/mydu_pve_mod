using System.Threading.Tasks;
using Mod.DynamicEncounters.Helpers;
using NQ;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;

public interface ICalculateTargetMovePositionEffect : IEffect
{
    Task<Vec3?> GetTargetMovePosition(Params @params);

    public class Params
    {
        public ulong InstigatorConstructId { get; set; }
        public Vec3? InstigatorStartPosition { get; set; }
        public Vec3? InstigatorPosition { get; set; }
        public ulong? TargetConstructId { get; set; }
        public double TargetDistance { get; set; }
        public double MaxDistanceVisibility { get; set; } = 10 * DistanceHelpers.OneSuInMeters;
        public required double DeltaTime { get; set; }
    }
}