using System.Collections.Generic;
using System.Threading.Tasks;
using Mod.DynamicEncounters.Features.Faction.Data;
using NQ;

namespace Mod.DynamicEncounters.Features.Faction.Interfaces;

public interface IFactionReputationRepository
{
    Task AddFactionReputationAsync(PlayerId playerId, FactionId factionId, long reputation);
    Task<long?> GetFactionReputationAsync(PlayerId playerId, FactionId factionId);
    Task<IEnumerable<FactionReputationItem>> GetPlayerFactionReputationAsync(PlayerId playerId);
}