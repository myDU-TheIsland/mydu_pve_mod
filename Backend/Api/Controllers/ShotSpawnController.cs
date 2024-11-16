using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Common.Interfaces;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Helpers;
using NQ;
using NQ.Interfaces;
using NQutils.Def;
using Swashbuckle.AspNetCore.Annotations;

namespace Mod.DynamicEncounters.Api.Controllers;

[Route("shot")]
public class ShotSpawnController : Controller
{
    [SwaggerOperation("Spawns shots on a construct. Useful to make wrecks")]
    [HttpPut]
    [Route("shooter/{shooterConstructId:long}/target/{targetConstructId:long}")]
    public async Task<IActionResult> Shoot(
        ulong shooterConstructId,
        ulong targetConstructId,
        [FromBody] ShotRequest request
    )
    {
        var provider = ModBase.ServiceProvider;
        var orleans = provider.GetOrleans();
        var npcShotGrain = orleans.GetNpcShotGrain();
        var targetConstructInfoGrain = orleans.GetConstructInfoGrain(targetConstructId);
        var targetConstructInfo = await targetConstructInfoGrain.Get();
        var constructDamageService = provider.GetRequiredService<IConstructDamageService>();

        var targetConstructElementsGrain = orleans.GetConstructElementsGrain(targetConstructId);
        var elements = await targetConstructElementsGrain.GetElementsOfType<ConstructElement>();

        var targetPos = targetConstructInfo.rData.position;

        var damageTrait = await constructDamageService.GetConstructDamage(shooterConstructId);
        
        for (var i = 0; i < request.Iterations; i++)
        {
            var random = provider.GetRequiredService<IRandomProvider>().GetRandom();
            var randomDirection = random.RandomDirectionVec3() * 30000;

            var shootPos = randomDirection + targetPos;
            
            var w = random.PickOneAtRandom(damageTrait.Weapons);

            var weaponMod = request.WeaponModifiers;
            var targetElement = random.PickOneAtRandom(elements);
            var targetElementInfo = await targetConstructElementsGrain.GetElement(targetElement.elementId);

            await npcShotGrain.Fire(
                "Random",
                shootPos,
                shooterConstructId,
                (ulong)targetConstructInfo.rData.geometry.size,
                targetConstructId,
                targetPos,
                new SentinelWeapon
                {
                    aoe = true,
                    damage = w.BaseDamage * weaponMod.Damage,
                    range = 400000,
                    aoeRange = 100000,
                    baseAccuracy = w.BaseAccuracy * weaponMod.Accuracy,
                    effectDuration = 1,
                    effectStrength = 1,
                    falloffDistance = w.FalloffDistance * weaponMod.FalloffDistance,
                    falloffTracking = w.FalloffTracking * weaponMod.FalloffTracking,
                    fireCooldown = 1,
                    baseOptimalDistance = w.BaseOptimalDistance * weaponMod.OptimalDistance,
                    falloffAimingCone = w.FalloffAimingCone * weaponMod.FalloffAimingCone,
                    baseOptimalTracking = w.BaseOptimalTracking * weaponMod.OptimalTracking,
                    baseOptimalAimingCone = w.BaseOptimalAimingCone * weaponMod.OptimalAimingCone,
                    optimalCrossSectionDiameter = w.OptimalCrossSectionDiameter,
                    ammoItem = request.AmmoItem,
                    weaponItem = request.WeaponItem
                },
                5,
                targetElementInfo.position
            );

            await Task.Delay((int)Math.Clamp(request.Wait, 1, 1000));
        }

        return Ok();
    }
    
    [Route("stasis/{constructId:long}")]
    [HttpPost]
    public async Task<IActionResult> ApplyStasisToConstruct(long constructId, [FromBody] StasisRequest request)
    {
        var provider = ModBase.ServiceProvider;
        var orleans = provider.GetOrleans();

        var constructInfoGrain = orleans.GetConstructInfoGrain((ulong)constructId);
        await constructInfoGrain.Update(new ConstructInfoUpdate
        {
            additionalMaxSpeedDebuf = new MaxSpeedDebuf
            {
                until = (DateTime.Now + request.DurationSpan).ToNQTimePoint(),
                value = Math.Clamp(request.Value, 0d, 1d)
            }
        });

        return Ok();
    }

    public class StasisRequest
    {
        public TimeSpan DurationSpan { get; set; }
        public double Value { get; set; }
    }

    public class ShotRequest
    {
        public double Wait { get; set; } = 500;
        public int Iterations { get; set; } = 10;
        public string AmmoItem { get; set; } = "AmmoCannonSmallKineticAdvancedPrecision";
        public string WeaponItem { get; set; } = "WeaponCannonSmallPrecision5";
        public BehaviorModifiers.WeaponModifiers WeaponModifiers { get; set; } = new();
    }
}