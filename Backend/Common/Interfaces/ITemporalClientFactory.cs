using System.Threading.Tasks;
using Temporalio.Client;

namespace Mod.DynamicEncounters.Common.Interfaces;

public interface ITemporalClientFactory
{
    Task<ITemporalClient> CreateAsync();
}