using System.Collections.Concurrent;
using System.Collections.Generic;
using Mod.DynamicEncounters.Vector.Data;

namespace Mod.DynamicEncounters.Features.Common.Services;

public static class SectorGridConstructCache
{
    public static ConcurrentDictionary<LongVector3, ConcurrentBag<ulong>> Data { get; set; } = new();

    public static IEnumerable<LongVector3> GetOffsets(long gridSnap, int radius = 1)
    {
        var offsets = new List<LongVector3>();

        for (long x = -radius; x <= radius; x++)
        {
            for (long y = -radius; y <= radius; y++)
            {
                for (long z = -radius; z <= radius; z++)
                {
                    offsets.Add(new LongVector3(x * gridSnap, y * gridSnap, z * gridSnap));
                }
            }
        }

        return offsets;
    }
}