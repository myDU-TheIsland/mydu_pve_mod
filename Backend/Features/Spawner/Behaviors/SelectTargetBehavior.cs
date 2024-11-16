using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Features.Common.Data;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Sector.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Effects.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Features.Spawner.Extensions;
using Mod.DynamicEncounters.Helpers;
using NQ;
using NQ.Interfaces;
using Orleans;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors;

public class SelectTargetBehavior(ulong constructId, IPrefab prefab) : IConstructBehavior
{
    private bool _active = true;
    private IClusterClient _orleans;
    private ILogger<SelectTargetBehavior> _logger;
    private IConstructGrain _constructGrain;
    private IConstructService _constructService;
    private ISectorPoolManager _sectorPoolManager;
    private INpcRadarService _npcRadarService;
    private Random _random;

    public bool IsActive() => _active;

    public BehaviorTaskCategory Category => BehaviorTaskCategory.MediumPriority;

    public Task InitializeAsync(BehaviorContext context)
    {
        var provider = context.ServiceProvider;

        _orleans = provider.GetOrleans();
        _logger = provider.CreateLogger<SelectTargetBehavior>();
        _constructGrain = _orleans.GetConstructGrain(constructId);
        _constructService = provider.GetRequiredService<IConstructService>();
        _sectorPoolManager = provider.GetRequiredService<ISectorPoolManager>();
        _npcRadarService = provider.GetRequiredService<INpcRadarService>();
        _random = provider.GetRandomProvider().GetRandom();

        return Task.CompletedTask;
    }

    public async Task TickAsync(BehaviorContext context)
    {
        if (!context.IsAlive)
        {
            _active = false;
            return;
        }

        var targetSpan = DateTime.UtcNow - context.TargetSelectedTime;
        if (context.IsMoveModeDefault() && targetSpan < TimeSpan.FromSeconds(5))
        {
            var position = await GetTargetMovePosition(context);
            if (!position.HasValue) return;

            context.SetAutoTargetMovePosition(position.Value);

            return;
        }

        var sw = new Stopwatch();
        sw.Start();

        _logger.LogInformation("Construct {Construct} Selecting a new Target", constructId);

        if (!context.Position.HasValue)
        {
            return;
        }

        IList<NpcRadarContact> radarContacts = [];

        if (context.Position.HasValue)
        {
            var spatialQuerySw = new StopWatch();
            spatialQuerySw.Start();

            radarContacts = (await _npcRadarService.ScanForPlayerContacts(
                    constructId,
                    context.Position.Value,
                    DistanceHelpers.OneSuInMeters * 5
                ))
                .ToList();

            StatsRecorder.Record("NPC_Radar", sw.ElapsedMilliseconds);
        }

        context.UpdateRadarContacts(radarContacts);

        if (context.HasAnyRadarContact())
        {
            context.RefreshIdleSince();
        }

        var selectTargetEffect = context.Effects.GetOrNull<ISelectRadarTargetEffect>();

        var targetDistance = prefab.DefinitionItem.TargetDistance;
        if (context.Damage.Weapons.Any())
        {
            targetDistance = _random.PickOneAtRandom(context.Damage.Weapons)
                .BaseOptimalDistance * prefab.DefinitionItem.Mods.Weapon.OptimalDistance;
        }
        
        var selectedTarget = selectTargetEffect?.GetTarget(
            new ISelectRadarTargetEffect.Params
            {
                DecisionTimeSeconds = prefab.DefinitionItem.TargetDecisionTimeSeconds, 
                TargetDistance = targetDistance,
                Contacts = radarContacts,
                Context = context
            }
        );

        if (selectedTarget == null)
        {
            return;
        }

        var targetId = selectedTarget.ConstructId;

        context.SetAutoTargetConstructId(targetId);

        var movePos = await GetTargetMovePosition(context);
        if (!movePos.HasValue) return;

        context.SetAutoTargetMovePosition(movePos.Value);

        await _sectorPoolManager.SetExpirationFromNow(context.Sector, TimeSpan.FromHours(1));

        try
        {
            var npcConstructInfoOutcome = await _constructService.GetConstructInfoAsync(constructId);
            var npcConstructInfo = npcConstructInfoOutcome.Info;
            if (npcConstructInfo == null)
            {
                return;
            }

            var targetConstructExists = await _constructService.Exists(targetId);
            if (!targetConstructExists)
            {
                return;
            }

            var targeting = new TargetingConstructData
            {
                constructId = constructId,
                ownerId = new EntityId { playerId = prefab.DefinitionItem.OwnerId },
                constructName = npcConstructInfo.rData.name
            };

            await _constructService.SendIdentificationNotification(
                targetId,
                targeting
            );

            await _constructService.SendAttackingNotification(
                targetId,
                targeting
            );
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to Identity Target");
        }

        try
        {
            await PilotingTakeOverAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to Takeover Ship");
        }
    }

    private async Task PilotingTakeOverAsync()
    {
        if (!await _constructService.IsBeingControlled(constructId))
        {
            await _constructGrain.PilotingTakeOver(ModBase.Bot.PlayerId, true);
        }
    }

    private async Task<Vec3?> GetTargetMovePosition(BehaviorContext context)
    {
        var effect = context.Effects.GetOrNull<ICalculateTargetMovePositionEffect>();
        if (effect == null)
        {
            return new Vec3();
        }

        return await effect.GetTargetMovePosition(new ICalculateTargetMovePositionEffect.Params
        {
            InstigatorConstructId = constructId,
            InstigatorStartPosition = context.StartPosition,
            InstigatorPosition = context.Position,
            TargetDistance = prefab.DefinitionItem.TargetDistance,
            TargetConstructId = context.GetTargetConstructId()
        });
    }
}