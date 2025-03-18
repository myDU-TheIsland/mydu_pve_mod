using System;
using System.Threading.Tasks;
using Temporalio.Activities;

namespace Mod.DynamicEncounters.Features.Sector.Temporal.Activities;

public class SectorInstanceActivities
{
    [Activity]
    public async Task<SectorExpirationOutcome> TryExpireSectorAsync(Guid sectorId)
    {
        await Task.Yield();
        
        return new SectorExpirationOutcome
        {
            Expired = true
        };
    }

    [Activity]
    public async Task ForceExpireSectorAsync(Guid sectorId)
    {
        await Task.Yield();
    }
    
    public record SectorExpirationOutcome
    {
        public required bool Expired { get; set; }
    }
}