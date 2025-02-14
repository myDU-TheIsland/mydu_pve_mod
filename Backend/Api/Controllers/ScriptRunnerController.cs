using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.TaskQueue.Interfaces;
using NQ;

namespace Mod.DynamicEncounters.Api.Controllers;

[Route("script")]
public class ScriptRunnerController : Controller
{
    [HttpPost]
    [Route("lua")]
    public async Task<IActionResult> RunLua()
    {
        using var reader = new StreamReader(Request.Body);
        var receivedText = await reader.ReadToEndAsync();

        if (string.IsNullOrEmpty(receivedText))
        {
            return BadRequest();
        }

        await Script.RunLua(receivedText)
            .EnqueueRunAsync();

        return Ok();
    }
    
    [HttpPost]
    [Route("run/{name}")]
    public async Task<IActionResult> RunScript(string name, [FromBody] RunScriptContextRequest request)
    {
        var provider = ModBase.ServiceProvider;

        var scriptService = provider.GetRequiredService<IScriptService>();

        for (var i = 0; i < request.Repeat; i++)
        {
            await scriptService.ExecuteScriptAsync(
                name,
                new ScriptContext(
                    provider,
                    request.FactionId,
                    [..request.PlayerIds],
                    request.Sector,
                    request.TerritoryId
                )
                {
                    ConstructId = request.ConstructId,
                    Properties = new ConcurrentDictionary<string, object>(request.Properties)
                }
            );
        }

        return Ok();
    }

    [HttpPost]
    [Route("action/run")]
    public async Task<IActionResult> RunScriptAction([FromBody] RunScriptActionItemRequest request)
    {
        var provider = ModBase.ServiceProvider;

        var scriptActionFactory = provider.GetRequiredService<IScriptActionFactory>();
        var scriptAction = scriptActionFactory.Create(request.Script);

        var context = request.Context;
        context.ServiceProvider = provider;
        
        var result = await scriptAction.ExecuteAsync(context);

        return Ok(new
        {
            result,
            context
        });
    }
    
    [HttpPost]
    [Route("action/run/v2")]
    public async Task<IActionResult> RunScriptActionV2([FromBody] RunScriptActionItemRequest request)
    {
        var provider = ModBase.ServiceProvider;

        var workflowEnqueueService = provider.GetRequiredService<IWorkflowEnqueueService>();
        await workflowEnqueueService.EnqueueAsync(new IWorkflowEnqueueService.RunScriptCommand
        {
            Script = request.Script,
            Context = new IWorkflowEnqueueService.WorkflowScriptContext
            {
                ConstructId = request.Context.ConstructId,
                PlayerIds = request.Context.PlayerIds,
                Sector = request.Context.Sector,
                TerritoryId = request.Context.TerritoryId,
            }
        });
        
        return Ok();
    }

    public class RunScriptActionItemRequest
    {
        public required ScriptActionItem Script { get; set; }
        public required ScriptContext Context { get; set; }
    }

    public class RunScriptContextRequest
    {
        public List<ulong> PlayerIds { get; set; } = [];
        public Vec3 Sector { get; set; }
        public ulong? ConstructId { get; set; }
        public long FactionId { get; set; } = 1;
        public Guid? TerritoryId { get; set; }
        public Dictionary<string, object> Properties { get; set; } = [];
        public int Repeat { get; set; } = 1;
    }
}