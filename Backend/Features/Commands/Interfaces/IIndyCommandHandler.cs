using System.Threading.Tasks;

namespace Mod.DynamicEncounters.Features.Commands.Interfaces;

public interface IIndyCommandHandler
{
    Task HandleCommand(ulong instigatorPlayerId, string command);
}