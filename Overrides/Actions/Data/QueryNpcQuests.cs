using System;
using Newtonsoft.Json;

namespace Mod.DynamicEncounters.Overrides.Actions.Data;

public class QueryNpcQuests
{
    [JsonProperty("constructId")]
    public ulong ConstructId { get; set; }
    [JsonProperty("playerId")]
    public ulong PlayerId { get; set; }
    [JsonProperty("factionId")]
    public long FactionId { get; set; }
    [JsonProperty("territoryId")]
    public Guid TerritoryId { get; set; }
    [JsonProperty("seed")]
    public int Seed { get; set; }
}