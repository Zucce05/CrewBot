using CrewBot.Classes;
using System;

namespace CrewBot.Tests;

public class VoidMessageSchedulerTests
{
    [Fact]
    public void ResolveDeleteAtUnixTimeSeconds_UsesOriginalMessageTimestamp()
    {
        DateTimeOffset createdAt = new DateTimeOffset(2026, 3, 24, 0, 0, 0, TimeSpan.Zero);

        long deleteAt = VoidMessageScheduler.ResolveDeleteAtUnixTimeSeconds(createdAt, 30);

        Assert.Equal(createdAt.AddSeconds(30).ToUnixTimeSeconds(), deleteAt);
    }

    [Fact]
    public void ResolveDeleteAtUnixTimeSeconds_ClampsToMinimumTimer()
    {
        DateTimeOffset createdAt = new DateTimeOffset(2026, 3, 24, 0, 0, 0, TimeSpan.Zero);

        long deleteAt = VoidMessageScheduler.ResolveDeleteAtUnixTimeSeconds(createdAt, 0);

        Assert.Equal(createdAt.AddSeconds(VoidTimingParser.MinimumTimerSeconds).ToUnixTimeSeconds(), deleteAt);
    }
}
