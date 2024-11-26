using NQ;

namespace Mod.DynamicEncounters.Features.Warp.Data;

public class CreateWarpAnchorForwardCommand
{
    public PlayerId PlayerId { get; set; }
    public double Distance { get; set; }
}