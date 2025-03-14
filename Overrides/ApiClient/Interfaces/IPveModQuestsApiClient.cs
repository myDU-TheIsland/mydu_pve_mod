using System;
using System.Threading.Tasks;
using Mod.DynamicEncounters.Overrides.ApiClient.Data;
using Newtonsoft.Json.Linq;

namespace Mod.DynamicEncounters.Overrides.ApiClient.Interfaces;

public interface IPveModQuestsApiClient
{
    Task<JToken> GetPlayerQuestsAsync(ulong playerId);
    Task<JToken> GetNpcQuests(ulong playerId, ulong constructId, long factionId, Guid territoryId, int seed);
    Task<BasicOutcome> AcceptQuest(Guid questId, ulong playerId, long factionId, Guid territoryId, int seed);
    Task<BasicOutcome> AbandonQuest(Guid questId, ulong playerId);
}