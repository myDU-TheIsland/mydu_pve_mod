﻿using System;
using System.Linq;
using System.Threading.Tasks;
using BotLib.BotClient;
using BotLib.Protocols;
using BotLib.Protocols.Queuing;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Common;
using Mod.DynamicEncounters.Database.Interfaces;
using Mod.DynamicEncounters.Features.Common.Interfaces;
using Mod.DynamicEncounters.Features.Scripts.Actions.Data;
using Mod.DynamicEncounters.Features.Scripts.Actions.Interfaces;
using Mod.DynamicEncounters.Features.TaskQueue.Interfaces;
using Serilog.Events;

namespace Mod.DynamicEncounters.Api.Controllers;

[Route("maintenance")]
public class MaintenanceController : Controller
{
    [HttpDelete]
    [Route("bugged-wrecks")]
    public async Task<IActionResult> CleanupBuggedWrecks()
    {
        var provider = ModBase.ServiceProvider;
        var handleRepository = provider.GetRequiredService<IConstructHandleRepository>();
        var constructService = provider.GetRequiredService<IConstructService>();

        var items = await handleRepository.FindAllBuggedPoiConstructsAsync();

        foreach (var constructId in items)
        {
            await constructService.SoftDeleteAsync(constructId);
        }
        
        return Ok(items);
    }

    [HttpPost]
    [Route("grpc/reconnect")]
    public async Task<IActionResult> ReconnectGrpc()
    {
        var provider = ModBase.ServiceProvider;
        var clientFactory = provider.GetRequiredService<IDuClientFactory>();
        ClientExtensions.UseFactory(clientFactory);

        var pi = LoginInformations.BotLogin(
            Environment.GetEnvironmentVariable("BOT_PREFIX")!,
            Environment.GetEnvironmentVariable("BOT_LOGIN")!,
            Environment.GetEnvironmentVariable("BOT_PASSWORD")!
        );

        await clientFactory.Connect(pi, allowExisting: true);
        
        return Ok();
    }

    [Route("loglevel/{logLevel:int}")]
    [HttpPost]
    public IActionResult ChangeLogLevel(int logLevel)
    {
        LoggingConfiguration.LoggingLevelSwitch.MinimumLevel = (LogEventLevel)logLevel;

        return Ok();
    }

    [Route("constructs/rebuff")]
    [HttpPost]
    public async Task<IActionResult> RebuffConstructs()
    {
        var provider = ModBase.ServiceProvider;
        var factory = provider.GetRequiredService<IPostgresConnectionFactory>();

        using var db = factory.Create();
        db.Open();

        var results = (await db.QueryAsync(
            """
            SELECT id FROM public.construct
            WHERE json_properties->>'kind' = '4' AND
                  deleted_at IS NULL
            """
        )).ToList();

        var constructIds = results.Select(x => (ulong)x.id);

        var taskQueueService = provider.GetRequiredService<ITaskQueueService>();

        var counter = 0;
        
        foreach (var constructId in constructIds)
        {
            await taskQueueService.EnqueueScript(
                new ScriptActionItem
                {
                    Script = "remove-buffs",
                    ConstructId = constructId
                },
                DateTime.UtcNow
            );
            
            await taskQueueService.EnqueueScript(
                new ScriptActionItem
                {
                    Script = "buff",
                    ConstructId = constructId
                },
                DateTime.UtcNow
            );

            counter++;
        }

        return Ok($"Enqueued {counter} Operations");
    }
}