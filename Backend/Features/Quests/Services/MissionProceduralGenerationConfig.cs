using System;

namespace Mod.DynamicEncounters.Features.Quests.Services;

public static class MissionProceduralGenerationConfig
{
    public static readonly TimeSpan TimeFactor = TimeSpan.FromMinutes(30);
    public const float TransportQuantaMultiplier = 1.6f;
    public const float ReverseTransportMultiplier = 2.7f;
    public const float UnsafeMultiplier = 2.5f;
}