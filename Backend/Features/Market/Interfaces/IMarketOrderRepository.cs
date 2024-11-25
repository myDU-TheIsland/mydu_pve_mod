using System.Threading.Tasks;

namespace Mod.DynamicEncounters.Features.Market.Interfaces;

public interface IMarketOrderRepository
{
    Task<double> GetAveragePriceOfItemAsync(ulong itemTypeId);
}