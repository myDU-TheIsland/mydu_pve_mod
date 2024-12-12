namespace Mod.DynamicEncounters.Features.Spawner.Behaviors.Skills.Data;

public class SkillItem
{
    public required string Name { get; set; } = "null";
    public required double CooldownSeconds { get; set; } = 60;
    public required string? ItemTypeName { get; set; }
}