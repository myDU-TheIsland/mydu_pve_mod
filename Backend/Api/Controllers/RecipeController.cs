using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Market.Interfaces;

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
}