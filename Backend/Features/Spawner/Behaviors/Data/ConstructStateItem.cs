using System;
using Newtonsoft.Json.Linq;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Data;

public class ConstructStateItem
{
    public Guid Id { get; set; }
    public ulong ConstructId { get; set; }
    public string Type { get; set; } = string.Empty;
    public JObject Properties { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}