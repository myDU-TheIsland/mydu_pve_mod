using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Scenegraph;
using BotLib.BotClient;
using BotLib.Protocols;
using BotLib.Protocols.Queuing;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Database.Interfaces;
using Mod.DynamicEncounters.Helpers;
using Mod.DynamicEncounters.SDK;
using NQ;
using NQ.Visibility;

namespace Mod.DynamicEncounters.Threads.Handles.Test;

public class NpcManagerActor : Actor
{
    private readonly IServiceProvider _provider = ModBase.ServiceProvider;

    private readonly List<string> _users = [];
    private readonly List<Client> _clients = [];
    public static ConcurrentDictionary<ulong, Properties> PropertiesMap { get; set; } = [];

    public override double FramesPerSecond { get; set; } = 1 / 4d;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = _provider.GetRequiredService<IPostgresConnectionFactory>();
        using var db = factory.Create();
        db.Open();

        var result = (await db.QueryAsync("SELECT * FROM mod_npc_def WHERE active")).ToList();

        var logger = _provider.CreateLogger<NpcManagerActor>();

        foreach (var item in result)
        {
            string playerName = item.name;

            try
            {
                var duClientFactory = _provider.GetRequiredService<IDuClientFactory>();
                var pi1 = LoginInformations.Impersonate(playerName,
                    playerName,
                    Environment.GetEnvironmentVariable("BOT_PASSWORD")!);

                var client = await Client.FromFactory(duClientFactory, pi1, allowExising: true);
                _clients.Add(client);
                _users.Add(playerName);
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
        var logger = _provider.CreateLogger<NpcManagerActor>();

        var i = 0;
        foreach (var client in _clients)
        {
            try
            {
                var sceneGraph = _provider.GetRequiredService<IScenegraph>();
                var location = await sceneGraph.GetPlayerLocation(client.PlayerId);

                var pu = new PlayerUpdate
                {
                    playerId = client.PlayerId,
                    position = location.position,
                    constructId = location.constructId,
                    time = TimePoint.Now(),
                    animationState = 2,
                };

                await _provider.GetRequiredService<Internal.InternalClient>()
                    .PublishGenericEventAsync(new EventLocation
                    {
                        Event = NQutils.Serialization.Grpc.MakeEvent(new NQutils.Messages.PlayerUpdate(pu)),
                        Location = location,
                        VisibilityDistance = 1000,
                    }, cancellationToken: stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to update pos of {User}", _users[i]);
            }

            i++;
        }

        await Task.Yield();
    }

    public class Properties
    {
        [ThirdParty.Json.LitJson.JsonProperty] private ulong AnimationState { get; set; }
    }
}