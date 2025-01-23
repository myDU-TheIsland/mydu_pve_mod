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
using TimeZoneConverter;

namespace Mod.DynamicEncounters.Threads.Handles.Test;

public class NpcManagerActor : Actor
{
    private readonly IServiceProvider _provider = ModBase.ServiceProvider;
    private DateTime _lastNpcListUpdate = DateTime.UtcNow;

    private static readonly ConcurrentDictionary<string, NpcDefinitionItem> NpcDefinitionItems = [];
    private static readonly ConcurrentDictionary<string, Client> Clients = [];
    private static readonly ConcurrentDictionary<string, bool> Disconnected = [];
    public static ConcurrentDictionary<ulong, Properties> PropertiesMap { get; set; } = [];

    public override double FramesPerSecond { get; set; } = 10;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var result = await GetNpcs();

        foreach (var item in result)
        {
            var playerName = item.Name;
            NpcDefinitionItems.TryAdd(playerName, item);

            if (item.ShouldConnect())
            {
                Disconnected.TryAdd(playerName, true);
            }
        }

        await base.StartAsync(cancellationToken);
    }

    public override async Task Tick(TimeSpan deltaTime, CancellationToken stoppingToken)
    {
        var logger = _provider.CreateLogger<NpcManagerActor>();

        foreach (var (playerName, client) in Clients)
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
                logger.LogError(bex, "NPC Business Exception: {User} - {Message}", playerName, bex.Message);
                Disconnected.TryAdd(playerName, true);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to update pos of {User}", playerName);
                Disconnected.TryAdd(playerName, true);
            }
        }

        foreach (var (dcPlayerName, _) in Disconnected)
        {
            if (NpcDefinitionItems.TryGetValue(dcPlayerName, out var item) && item.ShouldConnect())
            {
                await ConnectPlayer(dcPlayerName);
            }
        }
        
        foreach (var item in NpcDefinitionItems.Values)
        {
            if (item.ShouldConnect())
            {
                if (!Clients.TryGetValue(item.Name, out _))
                {
                    Disconnected.TryAdd(item.Name, true);
                }
            }
        }

        foreach (var item in NpcDefinitionItems.Values)
        {
            if (item.ShouldDisconnect())
            {
                if (Clients.TryGetValue(item.Name, out var client))
                {
                    await client.Disconnect();
                    Clients.TryRemove(item.Name, out _);
                    Disconnected.TryRemove(item.Name, out _);
                }
            }
        }

        if (DateTime.UtcNow - _lastNpcListUpdate > TimeSpan.FromSeconds(10))
        {
            _lastNpcListUpdate = DateTime.UtcNow;
            var npcList = await GetNpcs();
            NpcDefinitionItems.Clear();
            
            foreach (var npc in npcList)
            {
                NpcDefinitionItems.TryAdd(npc.Name, npc);
            }
        }

        await Task.Yield();
    }

    public static async Task RemoveNpc(string name)
    {
        if (Clients.TryRemove(name, out var client))
        {
            await client.Disconnect();
            Disconnected.TryRemove(name, out _);
        }
    }

    public static Task AddNpc(string name)
    {
        Disconnected.TryAdd(name, true);
        return Task.CompletedTask;
    }

    private async Task ConnectPlayer(string dcPlayerName)
    {
        var duClientFactory = _provider.GetRequiredService<IDuClientFactory>();
        var pi1 = LoginInformations.Impersonate(dcPlayerName,
            dcPlayerName,
            Environment.GetEnvironmentVariable("BOT_PASSWORD")!);

        var client = await Client.FromFactory(duClientFactory, pi1, allowExising: true);

        if (!Clients.TryAdd(dcPlayerName, client))
        {
            Clients[dcPlayerName] = client;
        }

        Disconnected.TryRemove(dcPlayerName, out _);
    }

    private async Task<IEnumerable<NpcDefinitionItem>> GetNpcs()
    {
        var factory = _provider.GetRequiredService<IPostgresConnectionFactory>();
        using var db = factory.Create();
        db.Open();

        return (await db.QueryAsync<DbRow>("SELECT * FROM mod_npc_def"))
            .Select(MapToModel)
            .ToList();
    }

    private NpcDefinitionItem MapToModel(DbRow row)
    {
        return new NpcDefinitionItem
        {
            Active = row.active,
            Properties = JsonConvert.DeserializeObject<Properties>(row.json_properties),
            FactionId = row.faction_id,
            Name = row.name
        };
    }

    public class Properties
    {
        [JsonProperty] public ulong AnimationState { get; set; }
        [JsonProperty] public TimeSpan? StartAt { get; set; }
        [JsonProperty] public TimeSpan? EndAt { get; set; }
        [JsonProperty] public string TimeZone { get; set; } = string.Empty;
    }

    public struct DbRow
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public int faction_id { get; set; }
        public bool active { get; set; }
        public string json_properties { get; set; }
    }

    public class NpcDefinitionItem
    {
        public string Name { get; set; } = string.Empty;
        public int FactionId { get; set; }
        public bool Active { get; set; }
        public Properties Properties { get; set; } = new();
        
        public Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;

        public bool ShouldConnect() => Active && IsDateInsideRange();
        public bool ShouldDisconnect() => !Active || !IsDateInsideRange();

        private bool IsDateInsideRange()
        {
            if (!Properties.StartAt.HasValue) return true;
            if (!Properties.EndAt.HasValue) return true;

            var tz = TZConvert.GetTimeZoneInfo(Properties.TimeZone);
            var localDate = TimeZoneInfo.ConvertTimeFromUtc(UtcNow(), tz);
            var localStart = localDate.Date + Properties.StartAt.Value;
            var localEnd = localDate.Date + Properties.EndAt.Value;
            
            return localDate > localStart && localDate < localEnd;
        }
    }
}