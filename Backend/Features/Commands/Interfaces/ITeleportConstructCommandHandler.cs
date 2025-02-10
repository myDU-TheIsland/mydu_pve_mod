using System.Threading.Tasks;

namespace Mod.DynamicEncounters.Features.Commands.Interfaces;

public interface ITeleportConstructCommandHandler
{
    Task Teleport(ulong instigatorPlayerId, string command);
}