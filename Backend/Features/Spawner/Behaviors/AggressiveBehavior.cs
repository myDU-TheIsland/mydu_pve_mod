using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Common.Interfaces;
using Mod.DynamicEncounters.Features.Common.Data;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Helpers;
using NQ;
using NQ.Interfaces;
using Orleans;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors;

public class AggressiveBehavior(ulong constructId, IPrefab prefab) : IConstructBehavior
{
    private IClusterClient _orleans;
    private ILogger<AggressiveBehavior> _logger;
    private IConstructService _constructService;

    private ElementId _coreUnitElementId;

    private bool _active = true;
    private IConstructElementsService _constructElementsService;

    public bool IsActive() => _active;

    public BehaviorTaskCategory Category => BehaviorTaskCategory.HighPriority;

    public async Task InitializeAsync(BehaviorContext context)
    {
        var provider = context.ServiceProvider;
        _orleans = provider.GetOrleans();

        _constructElementsService = provider.GetRequiredService<IConstructElementsService>();

        _coreUnitElementId = await _constructElementsService.GetCoreUnit(constructId);

        _constructService = provider.GetRequiredService<IConstructService>();

        context.Properties.TryAdd("CORE_ID", _coreUnitElementId);

        _logger = provider.CreateLogger<AggressiveBehavior>();
    }

    public async Task TickAsync(BehaviorContext context)
    {
        if (!context.IsAlive)
        {
            _active = false;

            return;
        }

        var targetConstructId = context.GetTargetConstructId();

        if (!targetConstructId.HasValue)
        {
            return;
        }

        var provider = context.ServiceProvider;

        var npcShotGrain = _orleans.GetNpcShotGrain();

        var constructInfoOutcome = await _constructService.GetConstructInfoAsync(constructId);
        var constructInfo = constructInfoOutcome.Info;
        if (constructInfo == null)
        {
            return;
        }

        var constructPos = constructInfo.rData.position;

        if (targetConstructId is null or 0)
        {
            return;
        }

        var targetInfoOutcome = await _constructService.GetConstructInfoAsync(targetConstructId.Value);
        var targetInfo = targetInfoOutcome.Info;
        if (targetInfo == null)
        {
            return;
        }

        var targetSize = targetInfo.rData.geometry.size;

        if (targetInfo.mutableData.pilot.HasValue)
        {
            context.PlayerIds.Add(targetInfo.mutableData.pilot.Value);
        }

        var random = provider.GetRandomProvider()
            .GetRandom();

        // var hitPos = random.RandomDirectionVec3() * targetSize / 2;
        var hitPos = random.RandomDirectionVec3() * targetSize / 4;
        var constructSize = (ulong)constructInfo.rData.geometry.size;
        var targetPos = targetInfo.rData.position;

        var damageTrait = context.Damage;
        if (!damageTrait.Weapons.Any())
        {
            return;
        }

        var weapon = random.PickOneAtRandom(damageTrait.Weapons);

        await ShootAndCycleAsync(
            new ShotContext(
                context,
                npcShotGrain,
                weapon,
                constructPos,
                constructSize,
                targetConstructId.Value,
                targetPos,
                hitPos,
                damageTrait.Weapons.Count() // One shot equivalent of all weapons for performance reasons
            )
        );
    }

    public class ShotContext(
        BehaviorContext behaviorContext,
        INpcShotGrain npcShotGrain,
        WeaponItem weaponHandle,
        Vec3 constructPosition,
        ulong constructSize,
        ulong targetConstructId,
        Vec3 targetPosition,
        Vec3 hitPosition,
        int quantityModifier
    )
    {
        public BehaviorContext BehaviorContext { get; set; } = behaviorContext;
        public INpcShotGrain NpcShotGrain { get; set; } = npcShotGrain;
        public WeaponItem WeaponItem { get; set; } = weaponHandle;
        public Vec3 ConstructPosition { get; set; } = constructPosition;
        public ulong ConstructSize { get; set; } = constructSize;
        public ulong TargetConstructId { get; set; } = targetConstructId;
        public Vec3 TargetPosition { get; set; } = targetPosition;
        public Vec3 HitPosition { get; set; } = hitPosition;
        public int QuantityModifier { get; set; } = quantityModifier;
    }

    private const string ShotTotalDeltaTimePropName = $"{nameof(AggressiveBehavior)}_ShotTotalDeltaTime";

    private double GetShootTotalDeltaTime(BehaviorContext context)
    {
        if (context.Properties.TryGetValue(ShotTotalDeltaTimePropName, out var value))
        {
            return (double)value;
        }

        return 0;
    }

    private void SetShootTotalDeltaTime(BehaviorContext context, double value)
    {
        if (!context.Properties.TryAdd(ShotTotalDeltaTimePropName, value))
        {
            context.Properties[ShotTotalDeltaTimePropName] = value;
        }
    }

    private async Task ShootAndCycleAsync(ShotContext context)
    {
        var distance = (context.TargetPosition - context.ConstructPosition).Size();

        if (distance > 2 * DistanceHelpers.OneSuInMeters)
        {
            return;
        }

        var functionalWeaponCount = await _constructElementsService.GetFunctionalDamageWeaponCount(constructId);
        if (functionalWeaponCount <= 0)
        {
            return;
        }

        _logger.LogDebug("Construct {Construct} Functional Weapon Count {Count}", constructId, functionalWeaponCount);

        context.QuantityModifier = functionalWeaponCount;
        context.QuantityModifier = Math.Clamp(context.QuantityModifier, 0, prefab.DefinitionItem.MaxWeaponCount);

        var random = context.BehaviorContext.ServiceProvider.GetRequiredService<IRandomProvider>()
            .GetRandom();

        var totalDeltaTime = GetShootTotalDeltaTime(context.BehaviorContext);
        totalDeltaTime += context.BehaviorContext.DeltaTime;

        SetShootTotalDeltaTime(context.BehaviorContext, totalDeltaTime);

        var w = context.WeaponItem;
        var ammoType = w.AmmoItems
            .Where(x => x.Level == prefab.DefinitionItem.AmmoTier && x.ItemTypeName.Contains(prefab.DefinitionItem.AmmoVariant, StringComparison.CurrentCultureIgnoreCase))
            .Select(x => x.ItemTypeName)
            .ToList();
        
        if (ammoType.Count == 0)
        {
            ammoType = ["AmmoMissileLarge4"];
        }

        var ammoItem = random.PickOneAtRandom(ammoType);
        
        var mod = prefab.DefinitionItem.Mods;
        var cycleTime = w.BaseCycleTime * mod.Weapon.CycleTime;

        if (totalDeltaTime < cycleTime)
        {
            return;
        }

        var isInSafeZone = await _constructService.IsInSafeZone(constructId);
        if (isInSafeZone)
        {
            return;
        }

        if (context.TargetConstructId > 0)
        {
            var targetInSafeZone = await _constructService.NoCache().IsInSafeZone(context.TargetConstructId);
            if (targetInSafeZone)
            {
                return;
            }
        }

        SetShootTotalDeltaTime(context.BehaviorContext, 0);

        var sw = new Stopwatch();
        sw.Start();

        await context.NpcShotGrain.Fire(
            w.DisplayName,
            context.ConstructPosition,
            constructId,
            context.ConstructSize,
            context.TargetConstructId,
            context.TargetPosition,
            new SentinelWeapon
            {
                aoe = true,
                damage = w.BaseDamage * mod.Weapon.Damage * context.QuantityModifier,
                range = 400000,
                aoeRange = 100000,
                baseAccuracy = w.BaseAccuracy * mod.Weapon.Accuracy,
                effectDuration = 10,
                effectStrength = 10,
                falloffDistance = w.FalloffDistance * mod.Weapon.FalloffDistance,
                falloffTracking = w.FalloffTracking * mod.Weapon.FalloffTracking,
                fireCooldown = cycleTime,
                baseOptimalDistance = w.BaseOptimalDistance * mod.Weapon.OptimalDistance,
                falloffAimingCone = w.FalloffAimingCone * mod.Weapon.FalloffAimingCone,
                baseOptimalTracking = w.BaseOptimalTracking * mod.Weapon.OptimalTracking,
                baseOptimalAimingCone = w.BaseOptimalAimingCone * mod.Weapon.OptimalAimingCone,
                optimalCrossSectionDiameter = w.OptimalCrossSectionDiameter,
                ammoItem = ammoItem,
                weaponItem = w.ItemTypeName
            },
            5,
            context.HitPosition
        );

        _logger.LogInformation("Construct {Construct} Shot Weapon. Took: {Time}ms {Weapon} / {Ammo}",
            constructId,
            sw.Elapsed.TotalMilliseconds,
            w.ItemTypeName,
            ammoItem
        );
    }
}