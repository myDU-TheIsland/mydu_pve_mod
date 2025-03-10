﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Common.Data;
using Mod.DynamicEncounters.Common.Helpers;
using Mod.DynamicEncounters.Features.Loot.Data;
using Mod.DynamicEncounters.Features.Loot.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Services;
using Mod.DynamicEncounters.Helpers;
using NQ;
using NQutils.Exceptions;

namespace Mod.DynamicEncounters.Features.Scripts.Actions;

[ScriptActionName(ActionName, Description = Description)]
public class SpawnLootForConstruct(ScriptActionItem actionItem) : IScriptAction
{
    public const string ActionName = "spawn-loot";
    public const string Description = "Spawns Loot for the construct in context";
    
    public string Name => ActionName;
    public string GetKey() => Name;

    public async Task<ScriptActionResult> ExecuteAsync(ScriptContext context)
    {
        if (!context.ConstructId.HasValue)
        {
            return ScriptActionResult.Failed();
        }
        
        var provider = ModBase.ServiceProvider;
        var logger = provider.CreateLogger<SpawnLootForConstruct>();

        var lootGeneratorService = provider.GetRequiredService<ILootGeneratorService>();
        var itemBagData = await lootGeneratorService.GenerateAsync(
            new LootGenerationArgs
            {
                Tags = actionItem.Tags,
                MaxBudget = actionItem.Value
            }
        );

        var itemSpawnerService = provider.GetRequiredService<IItemSpawnerService>();
        await itemSpawnerService.SpawnItems(
            new SpawnItemOnRandomContainersCommand(context.ConstructId.Value, itemBagData)
        );
        
        logger.LogInformation("Spawned Loot for Construct {Construct}", context.ConstructId);

        var retryOptions = RetryOptions.Default(logger);
        retryOptions.ShouldRetryOnException =
            ex => ex is BusinessException bex && bex.error.code == ErrorCode.InvalidSession;

        var elementReplacer = provider.GetRequiredService<IElementReplacerService>();
        foreach (var replace in itemBagData.ElementsToReplace)
        {
            for (var i = 0; i < replace.Quantity; i++)
            {
                try
                {
                    await elementReplacer.ReplaceSingleElementAsync(
                        context.ConstructId.Value,
                        replace.ElementName,
                        replace.ReplaceElementName
                    ).WithRetry(retryOptions);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to replace element {El} to {El2}", replace.ElementName, replace.ReplaceElementName);
                }
            }
        }
        
        logger.LogInformation("Processed Element Replacements");

        await itemSpawnerService.SpawnSpaceFuel(
            new SpawnFuelCommand(context.ConstructId.Value)
        );
        
        return ScriptActionResult.Successful();
    }
}