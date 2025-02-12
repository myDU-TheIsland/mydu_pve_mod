using System.Linq;
using System.Threading.Tasks;
using Backend.Scenegraph;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Commands.Interfaces;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Helpers;
using NQ;
using NQ.Interfaces;

namespace Mod.DynamicEncounters.Features.Commands.Services;

public class ReloadConstructCommandHandler : IReloadConstructCommandHandler
{
    public async Task ReloadConstruct(ulong instigatorPlayerId, string command)
    {
        var provider = ModBase.ServiceProvider;
        var safeZoneService = provider.GetRequiredService<ISafeZoneService>();
        var orleans = provider.GetOrleans();
        var sceneGraph = provider.GetRequiredService<IScenegraph>();
        
        var (local, _) = await sceneGraph.GetPlayerWorldPosition(instigatorPlayerId);

        if (local.constructId <= 0)
        {
            return;
        }

        var position = await sceneGraph.GetConstructCenterWorldPosition(local.constructId);
        var safeZones = await safeZoneService.GetSafeZones();
        var isInSafeZone = safeZones.Any(sz => sz.IsPointInside(position));
        if (!isInSafeZone)
        {
            return;
        }
        
        var constructInfoGrain = orleans.GetConstructInfoGrain(local.constructId);
        var constructInfo = await constructInfoGrain.Get();

        ConstructKind[] allowedKinds = [ConstructKind.STATIC, ConstructKind.SPACE];
        if (!allowedKinds.Contains(constructInfo.kind))
        {
            return;
        }

        var constructParentingGrain = orleans.GetConstructParentingGrain();
        await constructParentingGrain.ReloadConstruct(local.constructId, true);
    }
}