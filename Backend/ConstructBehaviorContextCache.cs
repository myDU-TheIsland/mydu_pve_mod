using System;
using Mod.DynamicEncounters.Features.Common.Services;
using Mod.DynamicEncounters.Features.Spawner.Data;

namespace Mod.DynamicEncounters;

public static class ConstructBehaviorContextCache
{
    public static TemporaryMemoryCache<ulong, BehaviorContext> Data { get; set; } = new(nameof(ConstructBehaviorContextCache), TimeSpan.FromHours(3));

    private static readonly object Lock = new();
    private static bool BotDisconnected { get; set; }
    private static DateTime? LastTimeBotDisconnected { get; set; }

    public static void RaiseBotDisconnected()
    {
        lock (Lock)
        {
            var now = DateTime.UtcNow;
            
            if (LastTimeBotDisconnected != null && now - LastTimeBotDisconnected > TimeSpan.FromSeconds(5))
            {
                BotDisconnected = true;
                LastTimeBotDisconnected = DateTime.UtcNow;

                ModBase.Bot.Reconnect()
                    .ContinueWith(_ => BotDisconnected = false);
            }
        }
    }
}