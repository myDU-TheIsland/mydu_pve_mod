using System.Threading.Tasks;
using NQ;

namespace Mod.DynamicEncounters.Overrides.Actions;

public interface IModActionHandler
{
    Task HandleActionAsync(ulong playerId, ModAction action);
}