using NQ;

namespace Mod.DynamicEncounters.Features.Warp.Data;

public class CreateWarpAnchorCommand
{
    public PlayerId PlayerId { get; set; }
    public Vec3? Position { get; set; }
    public string ElementTypeName { get; set; } = "";
}