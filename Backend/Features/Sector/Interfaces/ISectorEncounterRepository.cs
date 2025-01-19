using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mod.DynamicEncounters.Common.Interfaces;
using Mod.DynamicEncounters.Features.Sector.Data;

namespace Mod.DynamicEncounters.Features.Sector.Interfaces;

public interface ISectorEncounterRepository : IRepository<SectorEncounterItem>
{
    public Task<IEnumerable<SectorEncounterItem>> FindActiveByFactionAsync(long factionId);
    public Task<IEnumerable<SectorEncounterItem>> FindActiveByFactionTerritoryAsync(long factionId, Guid territoryId);
}