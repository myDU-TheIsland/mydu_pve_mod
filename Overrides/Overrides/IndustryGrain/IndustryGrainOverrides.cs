using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NQ;
using NQ.Grains.Core;
using Orleans;
using Orleans.Runtime;

namespace Mod.DynamicEncounters.Overrides.Overrides.IndustryGrain;

public class IndustryGrainOverrides(IServiceProvider provider)
{
    private readonly ILogger _logger = provider.GetRequiredService<ILoggerFactory>()
        .CreateLogger<IndustryGrainOverrides>();
    
    public void RegisterHooks(IHookCallManager hookCallManager)
    {
        hookCallManager.Register(
            "IndustryUnitGrain.OnActivateAsync",
            HookMode.Replace,
            this,
            nameof(OnActivateAsync)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.Recipe",
            HookMode.Replace,
            this,
            nameof(Recipe)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.Recipe",
            HookMode.Replace,
            this,
            nameof(Recipe)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.SetRecipe",
            HookMode.Replace,
            this,
            nameof(SetRecipe)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.GetRecipe",
            HookMode.Replace,
            this,
            nameof(GetRecipe)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.StopHard",
            HookMode.Replace,
            this,
            nameof(StopHard)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.StopSoft",
            HookMode.Replace,
            this,
            nameof(StopSoft)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.UpdateReminder",
            HookMode.Replace,
            this,
            nameof(UpdateReminder)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.StopAndRefund",
            HookMode.Replace,
            this,
            nameof(StopAndRefund)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.TryRun",
            HookMode.Replace,
            this,
            nameof(TryRun)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.Start",
            HookMode.Replace,
            this,
            nameof(Start)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.Status",
            HookMode.Replace,
            this,
            nameof(Status)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.SetOutputContainer",
            HookMode.Replace,
            this,
            nameof(SetOutputContainer)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.AddInputContainer",
            HookMode.Replace,
            this,
            nameof(AddInputContainer)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.RemoveInputContainer",
            HookMode.Replace,
            this,
            nameof(RemoveInputContainer)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.RemoveInputContainer",
            HookMode.Replace,
            this,
            nameof(RemoveInputContainer)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.Statistics",
            HookMode.Replace,
            this,
            nameof(Statistics)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.SetClaimProducts",
            HookMode.Replace,
            this,
            nameof(SetClaimProducts)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.Destroy",
            HookMode.Replace,
            this,
            nameof(Destroy)
        );
        
        hookCallManager.Register(
            "IndustryUnitGrain.ReceiveReminder",
            HookMode.Replace,
            this,
            nameof(ReceiveReminder)
        );
    }

    public async Task OnActivateAsync(IIncomingGrainCallContext context)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(OnActivateAsync));
        
        await context.Invoke();
    }

    public async Task<Recipe> Recipe(IIncomingGrainCallContext context)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(Recipe));
        
        await context.Invoke();

        return (Recipe)context.Result;
    }

    public async Task SetRecipe(IIncomingGrainCallContext context, ulong rid, ulong playerId)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(SetRecipe));
        
        await context.Invoke();
    }

    public async Task<(Recipe recipe, ulong batchSize)> GetRecipe(IIncomingGrainCallContext context, ulong recipeId, ulong playerId)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(GetRecipe));
        
        await context.Invoke();

        return ((Recipe recipe, ulong batchSize))context.Result;
    }

    public async Task StopHard(IIncomingGrainCallContext context, bool allowIngredientLoss)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(StopHard));
        
        await context.Invoke();
    }

    public async Task StopSoft(IIncomingGrainCallContext context)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(StopSoft));
        
        await context.Invoke();
    }

    public async Task UpdateReminder(IIncomingGrainCallContext context)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(UpdateReminder));
        
        await context.Invoke();
    }

    public async Task<bool> StopAndRefund(IIncomingGrainCallContext context, List<Ingredient> ingredients, bool allowIngredientLoss)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(StopAndRefund));
        
        await context.Invoke();

        return (bool)context.Result;
    }

    public async Task TryRun(IIncomingGrainCallContext context)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(TryRun));
        
        await context.Invoke();
    }

    public async Task Start(IIncomingGrainCallContext context, ulong playerId, IndustryStart parameters)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(Start));
        
        await context.Invoke();
    }

    public async Task<IndustryStatus> Status(IIncomingGrainCallContext context)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(Status));
        
        await context.Invoke();

        var status = (IndustryStatus)context.Result;
        
        return status;
    }

    public async Task SetOutputContainer(IIncomingGrainCallContext context, ulong id, bool fromContainer)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(SetOutputContainer));
        
        await context.Invoke();
    }

    public async Task AddInputContainer(IIncomingGrainCallContext context, ulong id)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(AddInputContainer));
        
        await context.Invoke();
    }

    public async Task RemoveInputContainer(IIncomingGrainCallContext context, ulong id, bool fromContainer)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(RemoveInputContainer));
        
        await context.Invoke();
    }

    public async Task<IndustryUnitStats> Statistics(IIncomingGrainCallContext context)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(Statistics));
        
        await context.Invoke();

        return (IndustryUnitStats)context.Result;
    }

    public async Task SetClaimProducts(IIncomingGrainCallContext context, bool cp)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(SetClaimProducts));
        
        await context.Invoke();
    }

    public async Task Destroy(IIncomingGrainCallContext context)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(Destroy));
        
        await context.Invoke();
    }

    public async Task ReceiveReminder(IIncomingGrainCallContext context, string reminderName, TickStatus status)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(ReceiveReminder));
        
        await context.Invoke();
    }
}