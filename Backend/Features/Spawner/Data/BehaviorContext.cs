using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Mod.DynamicEncounters.Features.Common.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Services;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Extensions;
using Mod.DynamicEncounters.Helpers;
using Mod.DynamicEncounters.Vector.Helpers;
using NQ;

namespace Mod.DynamicEncounters.Features.Spawner.Data;

public class BehaviorContext(
    ulong constructId,
    long factionId,
    Guid? territoryId,
    Vec3 sector,
    IServiceProvider serviceProvider,
    IPrefab prefab
) : BaseContext
{
    private double _deltaTime;

    public double DeltaTime
    {
        get => _deltaTime;
        set => _deltaTime = Math.Clamp(value, 1 / 60f, 1);
    }

    public const string AutoTargetMovePositionEnabledProperty = "AutoTargetMovePositionEnabled";
    public const string AutoSelectAttackTargetConstructProperty = "AutoSelectAttackTargetConstruct";
    public const string EnginePowerProperty = "EnginePower";
    public const string IdleSinceProperty = "IdleSince";
    public const string V0Property = "V0";
    public const string BrakingProperty = "Braking";
    public const string MoveModeProperty = "MoveMode";
    public const string ContactListProperty = "ContactList";

    public DateTime StartedAt { get; } = DateTime.UtcNow;
    public Vec3 Velocity { get; set; }
    public Vec3? Position { get; private set; }
    public Vec3? StartPosition { get; private set; }
    public Quat Rotation { get; set; }
    public float TargetRotationPositionMultiplier { get; set; } = 1;
    public HashSet<ulong> PlayerIds { get; set; } = [];
    public ulong ConstructId { get; } = constructId;
    public long FactionId { get; } = factionId;
    public Guid? TerritoryId { get; } = territoryId;
    public Vec3 Sector { get; } = sector;
    public Vec3 TargetPosition { get; set; }
    public ulong? TargetConstructId { get; set; }
    public double TargetDistance { get; set; }
    public Vec3 TargetLinearVelocity { get; private set; }
    public double VelocityWithTargetDotProduct { get; private set; }
    public bool IsApproaching { get; set; }
    public DateTime? LastApproachingUpdate { get; set; }
    public double TargetMoveDistance { get; set; }
    public ConstructDamageData DamageData { get; set; } = new([]);
    public ConcurrentDictionary<ulong, ConstructDamageData> TargetDamageData { get; set; } = new();
    public IServiceProvider ServiceProvider { get; init; } = serviceProvider;
    public readonly ConcurrentDictionary<string, bool> PublishedEvents = [];
    public ConcurrentDictionary<string, TimerPropertyValue> PropertyOverrides { get; } = [];
    public IEffectHandler Effects { get; set; } = new EffectHandler(serviceProvider);

    [Newtonsoft.Json.JsonIgnore]
    [JsonIgnore]
    public IPrefab Prefab { get; set; } = prefab;

    public DateTime? TargetSelectedTime { get; set; }

    public bool IsAlive { get; set; } = true;

    public bool IsActiveWreck { get; set; }

    public double RealismFactor { get; set; } = prefab.DefinitionItem.RealismFactor;
    public bool HasShield { get; set; }
    public double ShieldPercent { get; set; } = 0;
    public bool IsShieldActive { get; set; }
    public bool IsShieldVenting { get; set; }

    public void UpdateShieldState(ConstructInfo constructInfo)
    {
        HasShield = constructInfo.mutableData.shieldState.hasShield;
        ShieldPercent = constructInfo.mutableData.shieldState.shieldHpRatio;
        IsShieldActive = constructInfo.mutableData.shieldState.isActive;
        IsShieldVenting = constructInfo.mutableData.shieldState.isVenting;
    }

    public Task NotifyEvent(string @event, BehaviorEventArgs eventArgs)
    {
        // TODO for custom events
        return Task.CompletedTask;
    }

    public void Deactivate<T>() where T : IConstructBehavior
    {
        var name = typeof(T).FullName;
        var key = $"{name}_FINISHED";

        if (!Properties.TryAdd(key, false))
        {
            Properties[key] = false;
        }
    }

    public bool IsBehaviorActive<T>() where T : IConstructBehavior
    {
        return IsBehaviorActive(typeof(T));
    }

    public bool IsBehaviorActive(Type type)
    {
        var name = type.FullName;
        var key = $"{name}_FINISHED";

        if (Properties.TryGetValue(key, out var finished) && finished is bool finishedBool)
        {
            return !finishedBool;
        }

        return true;
    }

    public void SetPosition(Vec3 position)
    {
        if (!StartPosition.HasValue)
        {
            StartPosition = position;
        }

        Position = position;
    }
    
    public void SetTargetMovePosition(Vec3 position)
    {
        Properties.Set(nameof(DynamicProperties.TargetMovePosition), position);
    }

    public void SetTargetLinearVelocity(Vec3 linear)
    {
        TargetLinearVelocity = linear;

        var targetDirection = TargetLinearVelocity.NormalizeSafe();
        var npcDirection = Velocity.NormalizeSafe();

        VelocityWithTargetDotProduct = npcDirection.Dot(targetDirection);
    }

    public void SetIsApproachingTarget(double previousDistance, double currentDistance)
    {
        IsApproaching = previousDistance > currentDistance;
    }

    public bool IsApproachingTarget()
    {
        return IsApproaching;
    }

    public void SetTargetPosition(Vec3 targetPosition)
    {
        TargetPosition = targetPosition;
    }

    public bool IsInsideOptimalRange() => TargetDistance <= GetBestWeaponOptimalRange();
    public bool IsOutsideOptimalRange() => TargetDistance > GetBestWeaponOptimalRange();

    public double GetBestWeaponOptimalRange()
    {
        if (!Position.HasValue) return 0;

        var weaponItem = DamageData.GetBestWeaponByTargetDistance(
            TargetPosition.Dist(Position.Value)
        );

        if (weaponItem == null) return 0;

        return DamageData.GetHalfFalloffFiringDistance(weaponItem);
    }

    public void SetTargetDistance(double distance)
    {
        SetIsApproachingTarget(TargetDistance, distance);

        TargetDistance = distance;
    }

    public void SetTargetMoveDistance(double distance)
    {
        TargetMoveDistance = distance;
    }

    public Vec3 GetTargetMovePosition()
    {
        return this.GetOverrideOrDefault(
            nameof(DynamicProperties.TargetMovePosition),
            new Vec3()
        );
    }

    public Vec3 GetTargetPosition() => TargetPosition;

    public double GetTargetMoveDistance() => TargetMoveDistance;

    public ulong? GetTargetConstructId() => this.TargetConstructId;

    public void SetTargetConstructId(ulong? constructId)
    {
        // can't target itself
        if (constructId == ConstructId)
        {
            return;
        }

        TargetConstructId = constructId;
        TargetSelectedTime = DateTime.UtcNow;
    }

    public void SetWaypointList(IEnumerable<Waypoint> waypoints)
    {
        Properties.Set(nameof(DynamicProperties.WaypointList), waypoints.ToList());
    }

    public Waypoint? GetNextNotVisited()
    {
        return GetWaypointList().FirstOrDefault(x => !x.Visited);
    }

    public object GetUnparsedWaypointList()
    {
        TryGetProperty(
            nameof(DynamicProperties.WaypointList),
            out object waypointList,
            new List<Waypoint>()
        );

        return waypointList;
    }

    public void UpdateRadarContacts(IList<ScanContact> contacts)
    {
        SetProperty(
            ContactListProperty,
            contacts.ToList()
        );
    }

    public bool HasAnyRadarContact() =>
        TryGetProperty<IEnumerable<ScanContact>>(ContactListProperty, out var contacts, []) && contacts.Any();

    public void RefreshIdleSince()
    {
        SetProperty(IdleSinceProperty, DateTime.UtcNow);
    }

    public void SetTargetDamageData(ulong constructId, ConstructDamageData data)
    {
        TargetDamageData.TryAdd(constructId, data);
    }

    public double CalculateBrakingDistance()
    {
        return VelocityHelper.CalculateBrakingDistance(
            Velocity.Size(),
            Prefab.DefinitionItem.AccelerationG * 3.6d
        );
    }
    
    public double CalculateBrakingTime()
    {
        return VelocityHelper.CalculateBrakingTime(
            Velocity.Size(),
            Prefab.DefinitionItem.AccelerationG * 3.6d
        );
    }
    
    public double CalculateAccelerationToTargetSpeedTime(double fromVelocity)
    {
        return VelocityHelper.CalculateTimeToReachVelocity(
            fromVelocity,
            TargetLinearVelocity.Size(),
            Prefab.DefinitionItem.AccelerationG * 3.6d
        );
    }

    public double CalculateTimeToMergeToDistance(double distance)
    {
        if (!Position.HasValue) return double.PositiveInfinity;

        return VelocityHelper.CalculateTimeToReachDistance(
            Position.Value.ToVector3(),
            Velocity.ToVector3(),
            TargetPosition.ToVector3(),
            TargetLinearVelocity.ToVector3(),
            distance
        );
    }

    public IEnumerable<Waypoint> GetWaypointList()
    {
        return this.GetOverrideOrDefault(
            nameof(DynamicProperties.WaypointList),
            (List<Waypoint>?) []
        );
    }

    public bool IsWaypointListInitialized()
    {
        TryGetProperty(
            nameof(DynamicProperties.WaypointListInitialized),
            out var initDone,
            false
        );

        return initDone;
    }

    public void TagWaypointListInitialized()
    {
        SetProperty(
            nameof(DynamicProperties.WaypointListInitialized),
            true
        );
    }

    public void ClearExpiredTimerProperties()
    {
        var expiredList = PropertyOverrides
            .Where(kvp => kvp.Value.IsExpired(DateTime.UtcNow))
            .ToList();

        foreach (var kvp in expiredList)
        {
            PropertyOverrides.TryRemove(kvp.Key, out _);
        }
    }

    private static class DynamicProperties
    {
        public const byte TargetMovePosition = 1;
        public const byte WaypointList = 4;
        public const byte WaypointListInitialized = 5;
    }

    public class TimerPropertyValue(DateTime expiresAt, object? value)
    {
        public DateTime ExpiresAt { get; } = expiresAt;
        public object? Value { get; } = value;

        public bool IsExpired(DateTime now)
        {
            return now > ExpiresAt;
        }
    }
}