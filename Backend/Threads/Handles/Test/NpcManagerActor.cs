using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BotLib.BotClient;
using BotLib.Generated;
using BotLib.Protocols;
using BotLib.Protocols.Queuing;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Database.Interfaces;
using Mod.DynamicEncounters.SDK;
using NQ;

namespace Mod.DynamicEncounters.Threads.Handles.Test;

public class NpcManagerActor(IServiceProvider provider) : Actor
{
    private readonly List<Client> _clients = [];

    public override double FramesPerSecond { get; set; } = 1 / 4d;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = provider.GetRequiredService<IPostgresConnectionFactory>();
        using var db = factory.Create();
        db.Open();
        
        var result = (await db.QueryAsync("SELECT * FROM mod_npc_def")).ToList();

        foreach (var item in result)
        {
            var duClientFactory = provider.GetRequiredService<IDuClientFactory>();
            var pi1 = LoginInformations.Impersonate(item.name,
                item.name,
                Environment.GetEnvironmentVariable("BOT_PASSWORD")!);

            var client = await Client.FromFactory(duClientFactory, pi1, allowExising: true);
            _clients.Add(client);
        }
        
        await base.StartAsync(cancellationToken);
    }

    public override async Task Tick(TimeSpan deltaTime, CancellationToken stoppingToken)
    {
        foreach (var client in _clients)
        {
            await client.ImplementationClient.PlayerUpdate(new PlayerUpdate
            {
                playerId = client.PlayerId,
                position = new Vec3(),
                constructId = 0,
                time = TimePoint.Now(),
            }, stoppingToken);
        }

        await Task.Yield();
    }
}