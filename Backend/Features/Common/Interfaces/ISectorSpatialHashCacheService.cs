using System.Collections.Concurrent;
using System.Threading.Tasks;
using Mod.DynamicEncounters.Vector.Data;

namespace Mod.DynamicEncounters.Features.Common.Interfaces;

public interface ISectorSpatialHashCacheService
{
    Task<ConcurrentDictionary<LongVector3, ConcurrentBag<ulong>>> GetPlayerConstructsSectorMapAsync();
}