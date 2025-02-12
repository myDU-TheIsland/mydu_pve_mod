using System.Threading.Tasks;

namespace Mod.DynamicEncounters.Features.Commands.Interfaces;

public interface IReloadConstructCommandHandler
{
    Task ReloadConstruct(ulong instigatorPlayerId, string command);
}