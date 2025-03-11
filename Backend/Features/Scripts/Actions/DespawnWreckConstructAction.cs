using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Services;
using Mod.DynamicEncounters.Features.Sector.Interfaces;
using Mod.DynamicEncounters.Helpers;
using NQ.Interfaces;

namespace Mod.DynamicEncounters.Features.Scripts.Actions;

[ScriptActionName(ActionName)]
public class DespawnWreckConstructAction(ScriptActionItem actionItem) : IScriptAction
{
    public const string ActionName = "despawn-wreck";
    
    public string GetKey() => Name;

    public string Name => ActionName;

    public async Task<ScriptActionResult> ExecuteAsync(ScriptContext context)
    {
        var provider = ModBase.ServiceProvider;

        var logger = provider.CreateLogger<DespawnWreckConstructAction>();

        ulong? constructId = context.ConstructId ?? actionItem.ConstructId;
        
        if (constructId is null or 0)
        {
            logger.LogError("No construct id on context to execute this action");
            return ScriptActionResult.Failed("No construct id on context to execute this action");
        }
        
        var orleans = provider.GetOrleans();

        var constructHandleRepository = provider.GetRequiredService<IConstructHandleRepository>();
        var sectorInstanceRepository = provider.GetRequiredService<ISectorInstanceRepository>();

        var handleItem = await constructHandleRepository.FindByConstructIdAsync(constructId.Value);
        var isPoi = handleItem != null && handleItem!.JsonProperties.Tags.Contains("poi");
        
        // TODO this is prob not needed anymore
        var sector = await sectorInstanceRepository.FindBySector(context.Sector);
        if (!isPoi && sector is { StartedAt: not null })
        {
            logger.LogWarning("Construct was already discovered: {Construct}. Aborting", constructId);
            return ScriptActionResult.Successful();
        }

        var constructInfoGrain = orleans.GetConstructInfoGrain(constructId.Value);
        var constructInfo = await constructInfoGrain.Get();
        
        if (handleItem == null)
        {
            logger.LogWarning("No handle found for Construct {Construct}. Aborting", constructId.Value);
            return ScriptActionResult.Failed($"No handle found for Construct {constructId.Value}. Aborting");
        }
        
        var owner = constructInfo.mutableData.ownerId;
        if (handleItem.OriginalOwnerPlayerId != owner.playerId || handleItem.OriginalOrganizationId != owner.organizationId)
        {
            logger.LogInformation("Prevented Despawn of NPC - Ownership is different than initial Spawn.");
            return ScriptActionResult.Successful();
        }
        
        try
        {
            var parentingGrain = orleans.GetConstructParentingGrain();
            await parentingGrain.DeleteConstruct(constructId.Value, hardDelete: true);
        
            logger.LogInformation("Deleted Wreck construct {ConstructId}", constructId.Value);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to delete Wreck construct {Construct}", constructId.Value);
            return ScriptActionResult.Failed($"Failed to delete Wreck construct {constructId.Value}");
        }
        
        return ScriptActionResult.Successful();
    }
}