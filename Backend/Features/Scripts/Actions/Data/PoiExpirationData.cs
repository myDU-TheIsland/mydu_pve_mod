using System;
using Mod.DynamicEncounters.Features.Sector.Data;

namespace Mod.DynamicEncounters.Features.Scripts.Actions.Data;

public class PoiExpirationData
{
    public required ConstructHandleProperties HandleProperties { get; init; }
    public required SectorInstanceProperties SectorInstanceProperties { get; init; }
    public required ulong ConstructId { get; init; }
    public required TimeSpan ExpiresAt { get; init; }
    public DateTime? StartedAt { get; set; }
}