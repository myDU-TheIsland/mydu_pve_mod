using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Helpers;

namespace Mod.DynamicEncounters.Threads.Handles;

public class ReconnectBotWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);
            
            await Tick(cts.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
        }
    }

    private async Task Tick(CancellationToken stoppingToken)
    {
        if (!ConstructBehaviorContextCache.IsBotDisconnected || stoppingToken.IsCancellationRequested)
        {
            return;
        }

        var logger = ModBase.ServiceProvider.CreateLogger<ReconnectBotWorker>();
        
        try
        {
            logger.LogWarning("Reconnecting Bot");
            
            await ModBase.Bot.Reconnect();
            ConstructBehaviorContextCache.RaiseBotReconnected();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to Reconnect BOT");
        }
    }
}