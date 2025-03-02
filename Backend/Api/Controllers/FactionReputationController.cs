using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Mod.DynamicEncounters.Features.Faction.Interfaces;

namespace Mod.DynamicEncounters.Api.Controllers;

[Route("faction-reputation")]
public class FactionReputationController : Controller
{
    private readonly IServiceProvider _provider = ModBase.ServiceProvider;
    
    [HttpGet]
    [Route("{playerId:long}")]
    public async Task<IActionResult> GetPlayerReputationReport(ulong playerId)
    {
        var factionReputationRepository = _provider.GetRequiredService<IFactionReputationRepository>();
        var report = await factionReputationRepository.GetPlayerFactionReputationAsync(playerId);

        return Ok(report);
    }

    [HttpPost]
    [Route("")]
    public async Task<IActionResult> AddFactionReputation([FromBody]AddFactionReputationRequest request)
    {
        var factionReputationRepository = _provider.GetRequiredService<IFactionReputationRepository>();
        await factionReputationRepository.AddFactionReputationAsync(request.PlayerId, request.FactionId, request.Reputation);
        
        return Ok(await GetPlayerReputationReport(request.PlayerId));
    }

    public class AddFactionReputationRequest
    {
        public ulong PlayerId { get; set; }
        public long FactionId { get; set; }
        public long Reputation { get; set; }
    }
}