using System.Threading.Tasks;

namespace Mod.DynamicEncounters.Features.Commands.Interfaces;

public interface IFactionCommandHandler
{
    Task HandleCommand(ulong instigatorPlayerId, string command);
}