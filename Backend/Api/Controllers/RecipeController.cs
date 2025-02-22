using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Market.Interfaces;
using Mod.DynamicEncounters.Helpers;

namespace Mod.DynamicEncounters.Api.Controllers;

[Route("recipe")]
public class RecipeController : Controller
{
    [Route("{itemName}")]
    [HttpGet]
    public async Task<IActionResult> GetRecipePriceMap(string itemName)
    {
        var provider = ModBase.ServiceProvider;
        var recipePriceCalculator = provider.GetRequiredService<IRecipePriceCalculator>();

        var recipeMap = await recipePriceCalculator.GetItemPriceMap();

        if (recipeMap.TryGetValue(itemName, out var result))
        {
            return Ok(result);
        }

        return NotFound();
    }

    [HttpPost]
    [Route("filter/by/{groupName}")]
    public async Task<IActionResult> GetMultiplierRecipes(string groupName, GetMultiplierRecipesRequest request)
    {
        var provider = ModBase.ServiceProvider;

        var bank = provider.GetGameplayBank();
        var recipesService = provider.GetRequiredService<IRecipes>();
        var recipes = await recipesService.GetAllRecipes();

        var outputRecipes = new List<YamlLikeRecipeItem>();

        foreach (var recipe in recipes)
        {
            outputRecipes.Add(new YamlLikeRecipeItem
            {
                id = recipe.id * (ulong)request.Multiplier,
                time = (long)recipe.time,
                nanocraftable = recipe.nanocraftable,
                @in = recipe.ingredients.Select(i => new KeyValuePair<string, long>(
                    bank.GetDefinition(i.itemId)!.Name,
                    i.quantity.value * request.Multiplier
                )).ToList(),
                @out = recipe.products.Select(i => new KeyValuePair<string, long>(
                    bank.GetDefinition(i.itemId)!.Name,
                    i.quantity.value * request.Multiplier
                )).ToList(),
                industries = recipe.producers.Select(p => bank.GetDefinition(p)!.Name)
                    .ToList()
            });
        }

        return Ok(outputRecipes);
    }

    public class GetMultiplierRecipesRequest
    {
        public int Multiplier { get; set; }
    }
    
    public readonly struct YamlLikeRecipeItem
    {
        public required ulong id { get; init; }
        public required long time { get; init; }
        public required bool nanocraftable { get; init; }
        public required List<KeyValuePair<string, long>> @in { get; init; }
        public required List<KeyValuePair<string, long>> @out { get; init; }
        public required List<string> industries { get; init; }
    }
}