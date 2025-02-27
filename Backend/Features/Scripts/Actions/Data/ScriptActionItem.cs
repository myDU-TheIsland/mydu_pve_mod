using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Scripts.Actions.Extensions;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.TaskQueue.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NQ;

namespace Mod.DynamicEncounters.Features.Scripts.Actions.Data;

/// <summary>
/// Class with all possible properties of actions
/// It will be made into a polymorphic type later on 
/// </summary>
public class ScriptActionItem
{
    [JsonIgnore]
    public Guid Id { get; set; }
    public string? Name { get; set; }
    
    /// <summary>
    /// Type of script action
    /// </summary>
    public string? Type { get; set; }
    
    /// <summary>
    /// A ConstructDefinition
    /// </summary>
    public string Prefab { get; set; }
    public int MinQuantity { get; set; } = 1;
    public int MaxQuantity { get; set; } = 1;
    public string Message { get; set; }
    public string Script { get; set; }
    public ulong ConstructId { get; set; }
    public Vec3? Position { get; set; } = new();
    public Vec3? Sector { get; set; } = new();
    public Guid? TerritoryId { get; set; }
    public TimeSpan TimeSpan { get; set; } = TimeSpan.Zero;
    public double Value { get; set; }
    public long? FactionId { get; set; }
    public ScriptActionOverrides Override { get; set; } = new();
    
    /// <summary>
    /// Use this to tag constructs that are spawned to control their behaviors later using the tags.
    /// The tags are going to be persisted on the construct handle table.
    /// later on you can raise scripts that use the tags to affect one or more constructs. Ie: Despawn all tagged "poi"
    /// </summary>
    public List<string> Tags { get; set; } = [];

    public ScriptActionAreaItem Area { get; set; } = new() { Type = "sphere", Radius = 1 };
    public List<ScriptActionItem> Actions { get; set; } = new();

    public ScriptActionEvents Events { get; set; } = new();

    public Dictionary<string, object> Properties { get; set; } = new();
    
    public T GetProperties<T>()
    {
        return JObject.FromObject(Properties).ToObject<T>();
    }

    public IScriptAction ToScriptAction()
    {
        return ModBase.ServiceProvider.GetScriptAction(this);
    }

    public async Task EnqueueRunAsync(ScriptContext? context = null, DateTime? startAt = null)
    {
        var workflowEnqueueService = ModBase.ServiceProvider.GetRequiredService<IWorkflowEnqueueService>();

        var workflowScriptContext = new IWorkflowEnqueueService.WorkflowScriptContext();
        if (context != null)
        {
            workflowScriptContext = new IWorkflowEnqueueService.WorkflowScriptContext
            {
                ConstructId = context.ConstructId,
                PlayerIds = context.PlayerIds,
                Sector = context.Sector,
                TerritoryId = context.TerritoryId,
            };
        }

        await workflowEnqueueService.EnqueueAsync(new IWorkflowEnqueueService.RunScriptCommand
        {
            Script = this,
            Context = workflowScriptContext,
            StartAt = startAt
        });
    }
}