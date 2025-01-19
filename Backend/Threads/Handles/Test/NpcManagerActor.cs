using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Scenegraph;
using BotLib.BotClient;
using BotLib.Generated;
using BotLib.Protocols;
using BotLib.Protocols.Queuing;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Database.Interfaces;
using Mod.DynamicEncounters.Helpers;
using Mod.DynamicEncounters.SDK;
using NQ;
using NQ.Interfaces;

namespace Mod.DynamicEncounters.Threads.Handles.Test;

public class NpcManagerActor(IServiceProvider provider) : Actor
{
    private readonly List<string> _users = [];
    private readonly List<Client> _clients = [];

    public override double FramesPerSecond { get; set; } = 1 / 4d;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = provider.GetRequiredService<IPostgresConnectionFactory>();
        using var db = factory.Create();
        db.Open();
        
        var result = (await db.QueryAsync("SELECT * FROM mod_npc_def WHERE active")).ToList();

        var logger = provider.CreateLogger<NpcManagerActor>();

        foreach (var item in result)
        {
            try
            {
                var duClientFactory = provider.GetRequiredService<IDuClientFactory>();
                var pi1 = LoginInformations.Impersonate(item.name,
                    item.name,
                    Environment.GetEnvironmentVariable("BOT_PASSWORD")!);

                var client = await Client.FromFactory(duClientFactory, pi1, allowExising: true);
                _clients.Add(client);
                _users.Add(item.name);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to auth to {User}", item.name as string);
            }
        }
        
        await base.StartAsync(cancellationToken);
    }

    public override async Task Tick(TimeSpan deltaTime, CancellationToken stoppingToken)
    {
        var logger = provider.CreateLogger<NpcManagerActor>();
        
        var i = 0;
        foreach (var client in _clients)
        {
            try
            {
                var sceneGraph = provider.GetRequiredService<IScenegraph>();
                var location = await sceneGraph.GetPlayerLocation(client.PlayerId);
                
                await client.ImplementationClient.PlayerUpdate(new PlayerUpdate
                {
                    playerId = client.PlayerId,
                    position = location.position,
                    constructId = location.constructId,
                    time = TimePoint.Now(),
                }, stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to update pos of {User}", _users[i]);
            }

            i++;
        }

        await Task.Yield();
    }
}