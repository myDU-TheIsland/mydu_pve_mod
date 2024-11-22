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
    
    private IEnumerable<AmmoItem> AmmoItems { get; } = ammoItems;

    public IEnumerable<AmmoItem> GetAmmoItems() => AmmoItems;
}