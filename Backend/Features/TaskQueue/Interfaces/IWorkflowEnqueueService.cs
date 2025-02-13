using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using NQ;

namespace Mod.DynamicEncounters.Features.TaskQueue.Interfaces;

public interface IWorkflowEnqueueService
{
    Task EnqueueAsync(RunScriptCommand command);

    public class RunScriptCommand
    {
        public required ScriptActionItem Script { get; set; }
        public WorkflowScriptContext Context { get; set; } = new();
        public DateTime? StartAt { get; set; }
    }

    public class WorkflowScriptContext
    {
        public long? FactionId { get; }
        public HashSet<ulong> PlayerIds { get; set; } = [];
        public Vec3 Sector { get; set; } = new();
        public ulong? ConstructId { get; set; }
        public Guid? TerritoryId { get; set; }
        public int RetryCount { get; set; } = 1;
    }
}