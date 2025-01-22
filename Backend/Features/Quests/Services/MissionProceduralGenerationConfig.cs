using System;

namespace Mod.DynamicEncounters.Features.Quests.Services;

public static class MissionProceduralGenerationConfig
{
    public static readonly TimeSpan TransportMissionTimeFactor = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan OrderMissionTimeFactor = TimeSpan.FromMinutes(15);
}