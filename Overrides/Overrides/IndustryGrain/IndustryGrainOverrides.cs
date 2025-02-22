using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Business;
using Backend.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NQ;
using NQ.Grains.Core;
using NQ.Interfaces;
using NQutils.Def;
using NQutils.Exceptions;
using NQutils.Sql;
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

        // hookCallManager.Register(
        //     "IndustryUnitGrain.TryRun",
        //     HookMode.Replace,
        //     this,
        //     nameof(TryRun)
        // );

        // hookCallManager.Register(
        //     "IndustryUnitGrain.Start",
        //     HookMode.Replace,
        //     this,
        //     nameof(Start)
        // );

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

    public async Task<(Recipe recipe, ulong batchSize)> GetRecipe(IIncomingGrainCallContext context, ulong recipeId,
        ulong playerId)
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

    public async Task<bool> StopAndRefund(IIncomingGrainCallContext context, List<Ingredient> ingredients,
        bool allowIngredientLoss)
    {
        _logger.LogInformation("IndustryOverride {Method}", nameof(StopAndRefund));

        await context.Invoke();

        return (bool)context.Result;
    }

    // public async Task TryRun(IIncomingGrainCallContext context)
    // {
    //     _logger.LogInformation("IndustryOverride {Method}", nameof(TryRun));
    //
    //     var industryUnitGrain = context.Grain.AsReference<IIndustryUnitGrain>();
    //     var state = await industryUnitGrain.Status();
    //     
    //     industryUnitGrain.NQUnregisterTimer("DelayOpTimer");
    //     if ((long)state.recipeId != (long)state.nextRecipeId)
    //     {
    //         state.recipeId = state.nextRecipeId;
    //         state.unitsProduced = 0UL;
    //         state.activationTime = DateTime.UtcNow.ToNQTimePoint();
    //         industryUnitGrain.stateChanged = true;
    //     }
    //
    //     state.playerId = state.nextPlayerId;
    //     // ISSUE: explicit non-virtual call
    //     (Recipe recipe, ulong num1) = await (industryUnitGrain.GetRecipe(state.recipeId, state.playerId));
    //     if (recipe == null)
    //     {
    //         await SetState(industryUnitGrain, state, IndustryState.STOPPED);
    //         await industryUnitGrain.UpdateReminder();
    //         await industryUnitGrain.WriteStateAsync();
    //         throw new BusinessException(NQ.ErrorCode.RecipeUnknown, $"Unknown recipe id {state.recipeId}");
    //     }
    //
    //     state.recipe = recipe;
    //     if ((long)state.currentBatchSize != (long)num1)
    //     {
    //         state.currentBatchSize = num1;
    //         await industryUnitGrain.PublishBatchSize();
    //     }
    //
    //     industryUnitGrain.stateChanged = true;
    //     if (state.maintainProductAmount != 0UL && industryUnitGrain.outputContainer != 0UL)
    //     {
    //         ItemReference itemReference = industryUnitGrain.MainProduct();
    //         if (state.claimProducts)
    //             itemReference =
    //                 new ItemReference(itemReference.Type, itemReference.Id, industryUnitGrain.constructOwner);
    //         var num2 = await provider.GetRequiredService<IClusterClient>()
    //             .GetContainerGrain((ElementId)industryUnitGrain.outputContainer).AmountOf(itemReference);
    //         industryUnitGrain.currentProductAmount = num2;
    //         if (num2 >= state.maintainProductAmount)
    //         {
    //             await SetState(industryUnitGrain, state, IndustryState.PENDING);
    //             await industryUnitGrain.UpdateReminder();
    //             return;
    //         }
    //     }
    //
    //     (bool flag1, bool flag2) = await industryUnitGrain.TryTake(recipe.ingredients);
    //     if (flag1)
    //     {
    //         await SetState(industryUnitGrain, state, IndustryState.RUNNING);
    //         state.start = DateTime.UtcNow.ToNQTimePoint();
    //         state.end = DateTime.UtcNow.Add(TimeSpan.FromSeconds(recipe.time)).ToNQTimePoint();
    //     }
    //     else if (flag2)
    //         await SetState(industryUnitGrain, state, IndustryState.JAMMED_MISSING_SCHEMATIC);
    //     else
    //         await SetState(industryUnitGrain, state, IndustryState.JAMMED_MISSING_INGREDIENT);
    //
    //     await industryUnitGrain.UpdateReminder();
    // }
    
    // private async Task SetState(IIndustryUnitGrain industryUnitGrain, IndustryStatus state, IndustryState ns)
    // {
    //     if (ns == state.state)
    //         return;
    //     
    //     state.state = ns;
    //     
    //     try
    //     {
    //         var dataAccessor = provider.GetRequiredService<IDataAccessor>();
    //         await dataAccessor.SetDynamicProperty(state.constructId, state.elementId, IndustryUnit.d_status, ns);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Failed to Set State");
    //     }
    //     
    //     industryUnitGrain.stateChanged = true;
    // }

    // public async Task Start(IIncomingGrainCallContext context, ulong playerId, IndustryStart parameters)
    // {
    //     _logger.LogInformation("IndustryOverride {Method}", nameof(Start));
    //
    //     var industryUnitGrain = context.Grain.AsReference<IIndustryUnitGrain>();
    //     var state = await industryUnitGrain.Status();
    //
    //     state.batchesRequested = parameters.numBatches;
    //     if (state.state != IndustryState.STOPPED)
    //     {
    //         state.stopRequested = false;
    //         state.batchesRemaining = parameters.numBatches;
    //         if (state.batchesRemaining == 0UL)
    //             state.batchesRemaining = IndustryUnitState.NO_LIMIT;
    //         state.maintainProductAmount = parameters.maintainProductAmount;
    //         industryUnitGrain.stateChanged = true;
    //         if (state.state == IndustryState.PENDING)
    //             await TryRun(context);
    //         // await industryUnitGrain.CallEnd();
    //     }
    //     else
    //     {
    //         if (state.nextRecipeId == 0UL)
    //             throw new BusinessException(NQ.ErrorCode.IndustryIncompatibleRecipe, "Recipe not set");
    //
    //         state.batchesRemaining = parameters.numBatches;
    //         if (state.batchesRemaining == 0UL)
    //             state.batchesRemaining = IndustryUnitState.NO_LIMIT;
    //         state.maintainProductAmount = parameters.maintainProductAmount;
    //         state.activationTime = DateTime.UtcNow.ToNQTimePoint();
    //         state.unitsProduced = 0UL;
    //         await SetProduced(parameters.elementId, state.unitsProduced);
    //         state.stopRequested = false;
    //
    //         var stats = await industryUnitGrain.Statistics();
    //
    //         ++stats.started;
    //         state.nextPlayerId = playerId;
    //         industryUnitGrain.stateChanged = true;
    //         await TryRun(context);
    //         // await industryUnitGrain.CallEnd();
    //     }
    // }

    private async Task SetProduced(ulong elementId, ulong produced)
    {
        try
        {
            var sql = provider.GetRequiredService<ISql>();
            var elementInfo = await sql.GetElement(elementId, fetchLinks: false);

            var dataAccessor = provider.GetRequiredService<IDataAccessor>();

            await dataAccessor.SetDynamicProperty(
                elementInfo.constructId,
                elementId,
                IndustryUnit.d_produced,
                (long)produced);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to Set Produced");
        }
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