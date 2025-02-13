namespace Mod.DynamicEncounters.Features.Scripts.Actions.Data;

public static class Script
{
    public static ScriptActionItem DeleteConstruct(ulong constructId)
        => new() { Type = DeleteConstructAction.ActionName, ConstructId = constructId };

    public static ScriptActionItem DeleteAsteroid(ulong constructId)
        => new() { Type = DeleteAsteroidAction.ActionName, ConstructId = constructId };

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
}