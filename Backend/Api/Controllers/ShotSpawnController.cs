using System;
using System.Threading.Tasks;
using Backend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Common.Interfaces;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Spawner.Data;
using Mod.DynamicEncounters.Helpers;
using NQ;
using NQ.Interfaces;
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

        var targetPos = targetConstructInfo.rData.position;

        var damageTrait = await constructDamageService.GetConstructDamage(shooterConstructId);

        var constructBbox = provider.GetRequiredService<IConstructBoundingBox>();
        var bbox = await constructBbox.GetConstructBoundingBox(targetConstructId);
        
        for (var i = 0; i < request.Iterations; i++)
        {
            var random = provider.GetRequiredService<IRandomProvider>().GetRandom();
            var randomDirection = random.RandomDirectionVec3() * 30000;

            var shootPos = randomDirection + targetPos;
            
            var w = random.PickOneAtRandom(damageTrait.Weapons);

            var weaponMod = request.WeaponModifiers;
            var point = GetRandomPointOnSurface(bbox);

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
                    aoeRange = 10,
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
                8000,
                point
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
    
    public static Vec3 GetRandomPointOnSurface(BoundingBox box)
    {
        if (box == null)
            throw new ArgumentNullException(nameof(box));

        Random random = new Random();

        // Randomly select one of the six faces
        int faceIndex = random.Next(0, 6);

        Vec3 point = new Vec3();

        switch (faceIndex)
        {
            case 0: // x = min.x (left face)
                point.x = box.min.x;
                point.y = box.min.y + (box.max.y - box.min.y) * random.NextDouble();
                point.z = box.min.z + (box.max.z - box.min.z) * random.NextDouble();
                break;
            case 1: // x = max.x (right face)
                point.x = box.max.x;
                point.y = box.min.y + (box.max.y - box.min.y) * random.NextDouble();
                point.z = box.min.z + (box.max.z - box.min.z) * random.NextDouble();
                break;
            case 2: // y = min.y (front face)
                point.x = box.min.x + (box.max.x - box.min.x) * random.NextDouble();
                point.y = box.min.y;
                point.z = box.min.z + (box.max.z - box.min.z) * random.NextDouble();
                break;
            case 3: // y = max.y (back face)
                point.x = box.min.x + (box.max.x - box.min.x) * random.NextDouble();
                point.y = box.max.y;
                point.z = box.min.z + (box.max.z - box.min.z) * random.NextDouble();
                break;
            case 4: // z = min.z (bottom face)
                point.x = box.min.x + (box.max.x - box.min.x) * random.NextDouble();
                point.y = box.min.y + (box.max.y - box.min.y) * random.NextDouble();
                point.z = box.min.z;
                break;
            case 5: // z = max.z (top face)
                point.x = box.min.x + (box.max.x - box.min.x) * random.NextDouble();
                point.y = box.min.y + (box.max.y - box.min.y) * random.NextDouble();
                point.z = box.max.z;
                break;
        }

        return point;
    }
    
    public static Vec3 GetRandomPointInBoundingBox(BoundingBox box)
    {
        if (box == null)
            throw new ArgumentNullException(nameof(box));

        Random random = new Random();

        double randomX = box.min.x + (box.max.x - box.min.x) * random.NextDouble();
        double randomY = box.min.y + (box.max.y - box.min.y) * random.NextDouble();
        double randomZ = box.min.z + (box.max.z - box.min.z) * random.NextDouble();

        return new Vec3
        {
            x = randomX,
            y = randomY,
            z = randomZ
        };
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