using System;
using System.Collections.Generic;
using NQutils.Def;

namespace Mod.DynamicEncounters.Features.Common.Data;

public class WeaponItem(ulong itemTypeId, string itemTypeName, WeaponUnit weaponUnit, IEnumerable<AmmoItem> ammoItems)
{
    public ulong ItemTypeId { get; set; } = itemTypeId;
    public string ItemTypeName { get; set; } = itemTypeName;
    public string DisplayName { get; set; } = weaponUnit.DisplayName;
    public double BaseDamage { get; set; } = weaponUnit.BaseDamage;
    public double BaseAccuracy { get; set; } = weaponUnit.BaseAccuracy;
    public double FalloffDistance { get; set; } = weaponUnit.FalloffDistance;
    public double FalloffTracking { get; set; } = weaponUnit.FalloffTracking;
    public double FalloffAimingCone { get; set; } = weaponUnit.FalloffAimingCone;
    public double BaseOptimalTracking { get; set; } = weaponUnit.BaseOptimalTracking;
    public double BaseOptimalDistance { get; set; } = weaponUnit.BaseOptimalDistance;
    public double BaseOptimalAimingCone { get; set; } = weaponUnit.BaseOptimalAimingCone;
    public double OptimalCrossSectionDiameter { get; set; } = weaponUnit.OptimalCrossSectionDiameter;
    public double BaseCycleTime { get; set; } = weaponUnit.BaseCycleTime;
    public double BaseReloadTime { get; set; } = weaponUnit.BaseReloadTime;
    public double MagazineVolume { get; set; } = weaponUnit.MagazineVolume;

    public int GetNumberOfShotsInMagazine(
        AmmoItem ammoItem,
        double magazineBuffFactor = 1.5d
    ) => (int)Math.Ceiling(MagazineVolume * magazineBuffFactor / ammoItem.UnitVolume);

    public double GetTimeToEmpty(
        AmmoItem ammoItem,
        double magazineBuffFactor = 1.5d,
        double cycleTimeBuffFactor = 1 / 1.5625d
    ) => GetNumberOfShotsInMagazine(ammoItem, magazineBuffFactor) /
         Math.Clamp(BaseCycleTime * Math.Clamp(cycleTimeBuffFactor, 0.05d, 5d), 0.5d, BaseCycleTime);

    public double GetReloadTime(double reloadTimeBuffFactor) => BaseReloadTime * reloadTimeBuffFactor;

    public double GetTotalCycleTime(
        AmmoItem ammoItem,
        double magazineBuffFactor = 1.5d,
        double cycleTimeBuffFactor = 1 / 1.5625d,
        double reloadTimeBuffFactor = 1 / 1.5625d
    ) => GetTimeToEmpty(ammoItem, magazineBuffFactor, cycleTimeBuffFactor) + GetReloadTime(reloadTimeBuffFactor);

    public double GetSustainedRateOfFire(
        AmmoItem ammoItem,
        double magazineBuffFactor = 1.5d,
        double cycleTimeBuffFactor = 1 / 1.5625d,
        double reloadTimeBuffFactor = 1 / 1.5625d
    ) => GetNumberOfShotsInMagazine(ammoItem, magazineBuffFactor) /
         Math.Clamp(
             GetTotalCycleTime(ammoItem, magazineBuffFactor, cycleTimeBuffFactor, reloadTimeBuffFactor),
             0.1,
             60
         );

    public double GetShotWaitTime(
        AmmoItem ammoItem,
        double magazineBuffFactor = 1.5d,
        double cycleTimeBuffFactor = 1 / 1.5625d,
        double reloadTimeBuffFactor = 1 / 1.5625d
    )
    {
        cycleTimeBuffFactor = Math.Clamp(cycleTimeBuffFactor, 0.1, 5);
        reloadTimeBuffFactor = Math.Clamp(reloadTimeBuffFactor, 0.1, 5);
        magazineBuffFactor = Math.Clamp(magazineBuffFactor, 0.1, 5);
        
        return 1 / Math.Clamp(
            GetSustainedRateOfFire(ammoItem, magazineBuffFactor, cycleTimeBuffFactor, reloadTimeBuffFactor),
            0.1d,
            60
        );
    }

    private IEnumerable<AmmoItem> AmmoItems { get; } = ammoItems;

    public IEnumerable<AmmoItem> GetAmmoItems() => AmmoItems;
}