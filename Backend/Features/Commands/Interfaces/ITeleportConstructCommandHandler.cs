using System.Threading.Tasks;

namespace Mod.DynamicEncounters.Features.Commands.Interfaces;

public interface ITeleportConstructCommandHandler
{
    Task TeleportConstruct(ulong instigatorPlayerId, string command);
}