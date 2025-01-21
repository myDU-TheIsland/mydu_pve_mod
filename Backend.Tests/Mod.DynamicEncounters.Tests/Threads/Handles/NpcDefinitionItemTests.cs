using Mod.DynamicEncounters.Threads.Handles.Test;

namespace Mod.DynamicEncounters.Tests.Threads.Handles;

[TestFixture]
public class NpcDefinitionItemTests
{
    [Test]
    public void Should_Be_Outside_Range()
    {
        var item = new NpcManagerActor.NpcDefinitionItem
        {
            Properties = new NpcManagerActor.Properties
            {
                ConnectAt = TimeSpan.FromHours(7),
                DisconnectAt = TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(14)),
            }
        };

        item.UtcNow = () => new DateTime(2025, 01, 20);
        var now = new DateTime(2025, 01, 20, 7, 15, 0);
        
        Assert.That(item.ShouldDisconnect(now), Is.True);
    }
    
    [Test]
    public void Should_Be_Inside_Range()
    {
        var item = new NpcManagerActor.NpcDefinitionItem
        {
            Properties = new NpcManagerActor.Properties
            {
                ConnectAt = TimeSpan.FromHours(7),
                DisconnectAt = TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(14)),
            },
            UtcNow = () => new DateTime(2025, 01, 20)
        };

        var now = new DateTime(2025, 01, 20, 7, 12, 0);
        
        Assert.That(item.ShouldConnect(now), Is.True);
    }
}