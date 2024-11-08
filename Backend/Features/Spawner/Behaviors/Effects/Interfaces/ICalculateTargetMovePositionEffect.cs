using System.Threading.Tasks;
using NQ;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;

public interface ICalculateTargetMovePositionEffect : IEffect
{
    Task<Vec3> GetTargetMovePosition(Params @params);

    public class Params
    {
        public ulong InstigatorConstructId { get; set; }
        public ulong? TargetConstructId { get; set; }
        public double TargetDistance { get; set; }
    }
}