﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Services;
using Mod.DynamicEncounters.Helpers;
using NQ.Interfaces;

namespace Mod.DynamicEncounters.Features.Scripts.Actions;

[ScriptActionName(ActionName)]
public class DeleteConstructAction(ScriptActionItem actionItem) : IScriptAction
{
    public const string ActionName = "delete";
    
    public string GetKey() => Name;

    public string Name => ActionName;
    
    public async Task<ScriptActionResult> ExecuteAsync(ScriptContext context)
    {
        var provider = ModBase.ServiceProvider;

        var logger = provider.CreateLogger<DeleteConstructAction>();

        context.ConstructId ??= actionItem.ConstructId;
        
        if (!context.ConstructId.HasValue)
        {
            logger.LogError("No construct id on context to execute this action");
            return ScriptActionResult.Failed();
        }
        
        var orleans = provider.GetOrleans();

        try
        {
            var parentingGrain = orleans.GetConstructParentingGrain();
            await parentingGrain.DeleteConstruct(context.ConstructId.Value, hardDelete: true);
        
            logger.LogInformation("Deleted construct {ConstructId}", context.ConstructId.Value);
        }
        catch (Exception e)
        {
            logger.LogInformation(e, "Failed to delete construct {Construct}", context.ConstructId.Value);
            return ScriptActionResult.Failed();
        }
        
        return ScriptActionResult.Successful();
    }
}