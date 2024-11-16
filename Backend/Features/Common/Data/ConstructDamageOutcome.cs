using System.Collections.Generic;
using Mod.DynamicEncounters.Common.Interfaces;

namespace Mod.DynamicEncounters.Features.Common.Data;

public class ConstructDamageOutcome(IEnumerable<WeaponItem> weapons) : IOutcome
{
    public IEnumerable<WeaponItem> Weapons { get; } = weapons;
}