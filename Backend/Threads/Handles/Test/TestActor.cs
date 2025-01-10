using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BotLib.BotClient;
using BotLib.Generated;
using BotLib.Protocols;
using BotLib.Protocols.Queuing;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.SDK;
using NQ;

namespace Mod.DynamicEncounters.Threads.Handles.Test;

public class TestActor(IServiceProvider provider) : Actor
{
    private readonly List<Client> _clients = [];

    public override double FramesPerSecond { get; set; } = 1 / 4d;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        for (var i = 2; i <= 3; i++)
        {
            var duClientFactory = provider.GetRequiredService<IDuClientFactory>();
            var pi1 = LoginInformations.Impersonate($"PVE{i}",
                $"PVE{i}",
                "Test12345!!!");

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