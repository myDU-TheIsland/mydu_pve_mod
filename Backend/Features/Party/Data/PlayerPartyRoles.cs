using System.Collections.Generic;

namespace Mod.DynamicEncounters.Features.Party.Data;

public static class PlayerPartyRoles
{
    public const string None = "";
    public const string Missile = "missile";
    public const string Cannon = "cannon";
    public const string Lasers = "laser";
    public const string Railgun = "railgun";

    public static HashSet<string> All =>
    [
        Missile,
        Cannon,
        Lasers,
        Railgun
    ];
}