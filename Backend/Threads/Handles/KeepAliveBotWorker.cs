using System;
using System.Threading;
using System.Threading.Tasks;
using BotLib.Generated;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Helpers;
using NQ;

namespace Mod.DynamicEncounters.Threads.Handles;

public class KeepAliveBotWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);

                await Tick(cts.Token);
                await Task.Delay(TimeSpan.FromMilliseconds(2000), stoppingToken);
            }
            catch (Exception e)
            {
                ModBase.ServiceProvider.CreateLogger<ReconnectBotWorker>()
                    .LogError(e, "{Type} Exception: {Message}", GetType().Name, e.Message);
            }
        }
    }

    private static async Task Tick(CancellationToken stoppingToken)
    {
        if (ConstructBehaviorContextCache.IsBotDisconnected || stoppingToken.IsCancellationRequested)
        {
            return;
        }

        await ModBase.Bot.ImplementationClient.PlayerUpdate(new PlayerUpdate
        {
            playerId = ModBase.Bot.PlayerId,
            position = new Vec3(),
            rotation = Quat.Identity,
            time = TimePoint.Now()
        }, stoppingToken);
    }
}