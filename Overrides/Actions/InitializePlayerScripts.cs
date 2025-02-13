using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mod.DynamicEncounters.Overrides.Common;
using Mod.DynamicEncounters.Overrides.Common.Interfaces;
using NQ;
using Timer = System.Timers.Timer;

namespace Mod.DynamicEncounters.Overrides.Actions;

public class InitializePlayerScripts(IServiceProvider provider, IMyDuInjectionService injection) : IModActionHandler
{
    private readonly MemoryCache _activePings = new(new MemoryCacheOptions());
    private readonly ILogger _logger = provider.GetRequiredService<ILogger<InitializePlayerScripts>>();

    public async Task HandleActionAsync(ulong playerId, ModAction action)
    {
        await injection.InjectJs(playerId, Resources.ChangeRecipeSubPanel);

        var timer = (Timer)_activePings.Get(playerId);
        timer?.Stop();
        
        _activePings.Remove(playerId);

        // await Notifications.SimpleNotificationToPlayer(provider, playerId, "Mods Loaded");
        
        _logger.LogInformation("Mods Loaded");
    }

    public Task StartPlayerPingAsync(ulong playerId)
    {
        _activePings.Set(playerId, CreatePingTimer(playerId), TimeSpan.FromMinutes(10));
        
        _logger.LogInformation("Start Player Ping");

        return Task.CompletedTask;
    }

    private Timer CreatePingTimer(ulong playerId)
    {
        var timer = new Timer(2000);
        timer.Elapsed += async (_, _) =>
        {
            await injection.InjectJs(playerId, Resources.CommonJs);
            _logger.LogInformation("Pinging Player ...");
        };
        timer.Start();

        return timer;
    }
}