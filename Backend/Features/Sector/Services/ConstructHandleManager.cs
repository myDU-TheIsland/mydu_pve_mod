﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Sector.Interfaces;
using Mod.DynamicEncounters.Helpers;
using NQ;

namespace Mod.DynamicEncounters.Features.Sector.Services;

public class ConstructHandleManager(IServiceProvider provider) : IConstructHandleManager
{
    private readonly IConstructHandleRepository _repository =
        provider.GetRequiredService<IConstructHandleRepository>();

    private readonly ILogger<ConstructHandleManager> _logger =
        provider.CreateLogger<ConstructHandleManager>();

    public async Task CleanupConstructHandlesInSectorAsync(Vec3 sector)
    {
        var expiredHandles = (await _repository.FindInSectorAsync(sector)).ToList();

        await CleanupHandles(expiredHandles);
    }

    private async Task CleanupHandles(IEnumerable<ConstructHandleItem> handleItems)
    {
        foreach (var handle in handleItems)
        {
            if (string.IsNullOrEmpty(handle.OnCleanupScript))
            {
                continue;
            }

            var script = new ScriptActionItem
            {
                ConstructId = handle.ConstructId,
                Type = handle.OnCleanupScript
            };

            var context = new ScriptContext(
                handle.FactionId,
                [],
                handle.Sector,
                null // TODO TerritoryId
            )
            {
                ConstructId = handle.ConstructId,
            };
            
            try
            {
                await script.ToScriptAction().ExecuteAsync(context);
                
                _logger.LogInformation("Construct {ConstructId} removed from tracking", handle.ConstructId);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    "Failed to delete construct {ConstructId} on Orleans. Ignoring it",
                    handle.ConstructId
                );

                await script.EnqueueRunAsync(context, DateTime.UtcNow.AddMinutes(30));
                ConstructsPendingDelete.Data.Enqueue(handle.ConstructId);
            }

            try
            {
                await _repository.DeleteByConstructId(handle.ConstructId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to Delete Construct {Construct} Handle from DB", handle.ConstructId);
                ConstructsPendingDelete.Data.Enqueue(handle.ConstructId);
            }
        }
    }

    public async Task CleanupConstructsThatFailedSectorCleanupAsync()
    {
        var scriptActionFactory = provider.GetRequiredService<IScriptActionFactory>();

        var constructIds = await _repository.FindAllBuggedPoiConstructsAsync();

        foreach (var constructId in constructIds)
        {
            var scriptAction = scriptActionFactory.Create(
                new ScriptActionItem
                {
                    Type = "delete",
                    ConstructId = constructId
                }
            );

            try
            {
                await scriptAction.ExecuteAsync(
                    new ScriptContext(null, [], new Vec3(), null)
                        .WithConstructId(constructId)
                );
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to cleanup bugged construct {Construct}", constructId);
                ConstructsPendingDelete.Data.Enqueue(constructId);
            }
        }
    }

    public Task TagAsDeletedConstructHandlesThatAreDeletedConstructs()
    {
        return _repository.TagAsDeletedConstructHandledThatAreDeletedConstructs();
    }

    public Task<int> GetActiveCount()
    {
        return _repository.GetActiveCount();
    }
}