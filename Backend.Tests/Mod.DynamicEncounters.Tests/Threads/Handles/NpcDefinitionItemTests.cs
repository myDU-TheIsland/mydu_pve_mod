using Mod.DynamicEncounters.Threads.Handles.Test;
using TimeZoneConverter;

namespace Mod.DynamicEncounters.Tests.Threads.Handles;

/// <summary>
/// For reporting
/// SELECT P.id, P.display_name, N.json_properties FROM public.player P
/// INNER JOIN public.mod_npc_def N ON N.name = P.display_name
/// WHERE P.connected AND P.is_bot
/// </summary>
[TestFixture]
public class NpcDefinitionItemTests
{
    [Test]
    public void Should_Be_Outside_Range()
    {
        var tz = TZConvert.GetTimeZoneInfo("America/Los_Angeles");
        var localNow = new DateTime(2025, 01, 20, 7, 15, 0, DateTimeKind.Unspecified);
        var utcNow = TimeZoneInfo.ConvertTimeToUtc(localNow, tz);
        
        var item = new NpcManagerActor.NpcDefinitionItem
        {
            Properties = new NpcManagerActor.Properties
            {
                StartAt = TimeSpan.FromHours(7),
                EndAt = TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(14)),
                TimeZone = "America/Los_Angeles"
            },
            UtcNow = () => utcNow
        };

        Assert.That(item.ShouldDisconnect(), Is.True);
    }
    
    [Test]
    public void Should_Be_Inside_Range()
    {
        var tz = TZConvert.GetTimeZoneInfo("America/Los_Angeles");
        var localNow = new DateTime(2025, 01, 20, 7, 13, 0, DateTimeKind.Unspecified);
        var utcNow = TimeZoneInfo.ConvertTimeToUtc(localNow, tz);
        
        var item = new NpcManagerActor.NpcDefinitionItem
        {
            Properties = new NpcManagerActor.Properties
            {
                StartAt = TimeSpan.FromHours(7),
                EndAt = TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(14)),
                TimeZone = "America/Los_Angeles"
            },
            UtcNow = () => utcNow
        };

        Assert.That(item.ShouldConnect(), Is.True);
    }
}