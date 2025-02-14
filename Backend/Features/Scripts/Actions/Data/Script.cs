using System.Collections.Generic;

namespace Mod.DynamicEncounters.Features.Scripts.Actions.Data;

public static class Script
{
    public static ScriptActionItem DeleteConstruct(ulong constructId)
        => new() { Type = DeleteConstructAction.ActionName, ConstructId = constructId };

    public static ScriptActionItem DeleteAsteroid(ulong asteroidId)
        => new() { Type = DeleteAsteroidAction.ActionName, ConstructId = asteroidId };

    public static ScriptActionItem ReloadConstruct(ulong constructId)
        => new() { Type = ReloadConstructAction.ActionName, ConstructId = constructId };
    
    public static ScriptActionItem GiveQuantaToPlayers(ulong[] playerIds, long quanta, string? reason = null)
        => new()
        {
            Type = GiveQuantaToPlayer.ActionName,
            Value = quanta,
            Properties =
                
            {
                { "PlayerIds", playerIds },
                { "Reason", reason }
            }
        };

    public static ScriptActionItem SpawnAsteroidMarker(string prefab, string name)
        => new()
        {
            Type = SpawnScriptAction.ActionName,
            Area = new ScriptActionAreaItem{Type = "null"},
            Prefab = prefab,
            Override = new ScriptActionOverrides
            {
                ConstructName = name
            },
            Properties = new Dictionary<string, object>
            {
                { "AddConstructHandle", false }
            }
        };
}