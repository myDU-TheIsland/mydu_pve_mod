using System.Collections.Generic;
using System.Linq;
using Mod.DynamicEncounters.Common.Interfaces;

namespace Mod.DynamicEncounters.Features.Common.Data;

public class ConstructDamageData(IEnumerable<WeaponItem> weapons) : IOutcome
{
    public IEnumerable<WeaponItem> Weapons { get; } = weapons;

    public WeaponItem? GetBestDamagingWeapon() => Weapons.MaxBy(w => w.BaseDamage);
    public WeaponItem? GetBestRangedWeapon() => Weapons.MaxBy(GetHalfFalloffFiringDistance);

    public double GetHalfFalloffFiringDistance(WeaponItem weaponItem) =>
        weaponItem.BaseOptimalDistance + weaponItem.FalloffDistance / 2;
}