using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NQ;

namespace Mod.DynamicEncounters.Features.Spawner.Data;

public class SpawnAsteroidCommand
{
    [JsonProperty] public required int Tier { get; set; } = 3;
    [JsonProperty] public required string Model { get; set; } = string.Empty;
    [JsonProperty] public required Vec3 Position { get; set; }
    [JsonProperty] public required ulong Planet { get; set; } = 2;
    [JsonProperty] public required string Prefix { get; set; } = "A-";
    [JsonProperty] public required bool RegisterAsteroid { get; set; } = true;
    [JsonProperty] public required JToken Data { get; set; } = string.Empty;
}