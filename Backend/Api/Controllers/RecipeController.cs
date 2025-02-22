using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Market.Interfaces;
using Mod.DynamicEncounters.Helpers;
using NQ;
using NQutils.Def;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Mod.DynamicEncounters.Api.Controllers;

[Route("recipe")]
public class RecipeController : Controller
{
    private readonly IServiceProvider _provider = ModBase.ServiceProvider;
    private readonly IGameplayBank _bank = ModBase.ServiceProvider.GetGameplayBank();
    private readonly IRecipes _recipeService = ModBase.ServiceProvider.GetRequiredService<IRecipes>();
    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance) // Use camelCase formatting
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull) // Omit null values
        .Build();
    
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
    public async Task<IActionResult> GetMultiplierRecipes(string groupName, [FromBody] GetMultiplierRecipesRequest request)
    {
        var groupDef = _bank.GetDefinition(groupName);
        if (groupDef == null)
            return NotFound($"Group {groupName} not found.");

        var recipeMap = (await _recipeService.GetAllRecipes())
            .GroupBy(g => g.products.First().itemId)
            .ToDictionary(k => k.Key, v => v.MinBy(r => r.products.First().quantity.value));
        
        var outputRecipes = new List<YamlLikeRecipeItem>();
        await HydrateRecipes(recipeMap, groupDef, request, outputRecipes.Add);

        var yamlDocuments = string.Join("\n---\n", outputRecipes.ConvertAll(item => _serializer.Serialize(item)));

        return Content(yamlDocuments, "application/x-yaml");
    }

    private async Task HydrateRecipes(Dictionary<ulong, Recipe> recipeMap, IGameplayDefinition definition, GetMultiplierRecipesRequest request, Action<YamlLikeRecipeItem> itemFound)
    {
        var items = definition.GetChildren();
        foreach (var def in items)
        {
            var children = def.GetChildren();
            if (children.Any())
            {
                await HydrateRecipes(recipeMap, def, request, itemFound);
            }
            else
            {
                if (!recipeMap.TryGetValue(def.Id, out var recipe))
                    continue;

                itemFound(new YamlLikeRecipeItem
                {
                    id = recipe.id * (ulong)request.Multiplier,
                    time = (long)recipe.time,
                    nanocraftable = recipe.nanocraftable,
                    @in = recipe.ingredients.Select(i => new Dictionary<string, long>
                    {
                        { _bank.GetDefinition(i.itemId)!.Name, GetItemQuantity(i.itemId, i.quantity.quantity) * request.Multiplier }
                    }).ToList(),
                    @out = recipe.products.Select(i => new Dictionary<string, long>
                    {
                        { _bank.GetDefinition(i.itemId)!.Name, GetItemQuantity(i.itemId, i.quantity.quantity) * request.Multiplier }
                    }).ToList(),
                    industries = recipe.producers.Select(p => _bank.GetDefinition(p)!.Name)
                        .ToList()
                });
            }
        }
    }

    private long GetItemQuantity(ulong itemId, long quantity)
    {
        var baseObject = _bank.GetBaseObject<BaseItem>(itemId);
        
        if (baseObject!.InventoryType == "material")
        {
            return quantity >> 24;
        }

        return quantity;
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
        public required List<Dictionary<string, long>> @in { get; init; }
        public required List<Dictionary<string, long>> @out { get; init; }
        public required List<string> industries { get; init; }
    }
}