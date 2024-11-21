using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Behaviors.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Features.Spawner.Extensions;
using Mod.DynamicEncounters.Helpers;
using Mod.DynamicEncounters.Helpers.DU;

namespace Mod.DynamicEncounters.Features.Spawner.Behaviors;

public class RetreatBehavior(ulong constructId, IPrefab prefab) : IConstructBehavior
{
    private readonly IPrefab _prefab = prefab;
    private ILogger<RetreatBehavior> _logger;
    private IConstructService _constructService;

    public BehaviorTaskCategory Category => BehaviorTaskCategory.HighPriority;

    public Task InitializeAsync(BehaviorContext context)
    {
        var provider = context.ServiceProvider;
        _constructService = provider.GetRequiredService<IConstructService>();
        _logger = provider.CreateLogger<RetreatBehavior>();

        return Task.CompletedTask;
    }

    public async Task TickAsync(BehaviorContext context)
    {
        if (!context.IsAlive)
        {
            return;
        }

        var targetConstructId = context.GetTargetConstructId();
        
        var npcInfoOutcome = await _constructService.GetConstructInfoAsync(constructId);
        var npcInfo = npcInfoOutcome.Info;

        if (npcInfo == null)
        {
            return;
        }

        // first time initialize position
        if (!context.Position.HasValue)
        {
            context.Position = npcInfo.rData.position;
        }
        
        var npcPos = context.Position.Value;
        
        if (!targetConstructId.HasValue)
        {
            // _logger.LogError("Construct {Construct} Target Construct Id NULL", constructId);
            context.SetAutoTargetMovePosition(context.Sector);
            return;
        }
        
        var targetConstructInfoOutcome = await _constructService.GetConstructInfoAsync(targetConstructId.Value);
        var targetConstructInfo = targetConstructInfoOutcome.Info;
        if (targetConstructInfo == null)
        {
            // _logger.LogError("Construct {Construct} Target Construct Info {Target} NULL", constructId, targetConstructId.Value);
            context.SetAutoTargetMovePosition(context.Sector);
            return;
        }
        
        var targetPos = targetConstructInfo.rData.position;

        if (!npcInfo.HasShield())
        {
            return;
        }

        context.UpdateShieldState(npcInfo);
        
        var isPrettyFar = Math.Abs(targetPos.Dist(npcPos)) > 1.7 * DistanceHelpers.OneSuInMeters;
        
        var shouldVentShields = context.IsShieldDown() || 
                                (context.IsShieldLowerThan25() && isPrettyFar) ||
                                (context.IsShieldLowerThanHalf() && isPrettyFar);

        context.TryGetProperty("ShieldVentTimer", out var shieldVentTimer, 0d);
        shieldVentTimer += context.DeltaTime;
        
        if (shouldVentShields)
        {
            if (shieldVentTimer > 5)
            {
                await _constructService.TryVentShieldsAsync(constructId);
                shieldVentTimer = 0;
            }

            context.SetProperty("ShieldVentTimer", shieldVentTimer);
        }
    }
}