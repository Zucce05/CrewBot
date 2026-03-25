using Discord;
using System;

namespace CrewBot.Classes
{
    public static class VoidMessageScheduler
    {
        public static long ResolveDeleteAtUnixTimeSeconds(IMessage message, int timerSeconds)
        {
            DateTimeOffset createdAt = message?.CreatedAt == default
                ? DateTimeOffset.UtcNow
                : message.CreatedAt;

            return ResolveDeleteAtUnixTimeSeconds(createdAt, timerSeconds);
        }

        public static long ResolveDeleteAtUnixTimeSeconds(DateTimeOffset createdAt, int timerSeconds)
        {
            int normalizedTimerSeconds = Math.Max(timerSeconds, VoidTimingParser.MinimumTimerSeconds);
            return createdAt.AddSeconds(normalizedTimerSeconds).ToUnixTimeSeconds();
        }
    }
}
