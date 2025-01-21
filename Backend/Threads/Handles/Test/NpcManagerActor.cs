using System;
using System.Collections.Concurrent;
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
using Newtonsoft.Json;
using NQ;
using NQ.Visibility;
using NQutils.Exceptions;

namespace Mod.DynamicEncounters.Threads.Handles.Test;

public class NpcManagerActor : Actor
{
    private readonly IServiceProvider _provider = ModBase.ServiceProvider;

    private readonly List<string> _users = [];
    private readonly List<Client> _clients = [];
    private readonly HashSet<ulong> _disconnected = [];
    private readonly Dictionary<ulong, string> _playerMap = [];
    public static ConcurrentDictionary<ulong, Properties> PropertiesMap { get; set; } = [];
    
    public override double FramesPerSecond { get; set; } = 10;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var result = await GetNpcs();

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
                _playerMap.TryAdd(client.PlayerId, playerName);
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
            var properties = new Properties();
            if (PropertiesMap.TryGetValue(client.PlayerId, out var props))
            {
                properties = props;
            }

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
                    animationState = properties.AnimationState,
                };

                var taskClientUpdate = client.ImplementationClient.PlayerUpdate(pu, stoppingToken);
                var taskEvent = _provider.GetRequiredService<Internal.InternalClient>()
                    .PublishGenericEventAsync(new EventLocation
                    {
                        Event = NQutils.Serialization.Grpc.MakeEvent(new NQutils.Messages.PlayerUpdate(pu)),
                        Location = location,
                        VisibilityDistance = 1000,
                    }, cancellationToken: stoppingToken).ResponseAsync;

                await Task.WhenAll(taskClientUpdate, taskEvent);
            }
            catch (BusinessException bex)
            {
                logger.LogError(bex, "NPC Business Exception: {User} - {Message}", _users[i], bex.Message);
                _disconnected.Add(client.PlayerId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to update pos of {User}", _users[i]);
            }

            i++;
        }

        foreach (var dcPlayerId in _disconnected)
        {
            if (!_playerMap.TryGetValue(dcPlayerId, out var dcPlayerName))
            {
                logger.LogError("Can't find data on map for {Id}", dcPlayerId);
                continue;
            }

            var client = await ConnectPlayer(dcPlayerName);

            var clients = _clients.Where(x => x.PlayerId != dcPlayerId)
                .ToList();
            clients.Add(client);
            _disconnected.Remove(dcPlayerId);

            _clients.Clear();
            _clients.AddRange(clients);
        }

        await Task.Yield();
    }

    private async Task<Client> ConnectPlayer(string dcPlayerName)
    {
        var duClientFactory = _provider.GetRequiredService<IDuClientFactory>();
        var pi1 = LoginInformations.Impersonate(dcPlayerName,
            dcPlayerName,
            Environment.GetEnvironmentVariable("BOT_PASSWORD")!);

        return await Client.FromFactory(duClientFactory, pi1, allowExising: true);
    }

    private async Task<List<dynamic>> GetNpcs()
    {
        var factory = _provider.GetRequiredService<IPostgresConnectionFactory>();
        using var db = factory.Create();
        db.Open();

        return (await db.QueryAsync("SELECT * FROM mod_npc_def WHERE active")).ToList();
    }

    public class Properties
    {
        [JsonProperty] public ulong AnimationState { get; set; }
    }
}