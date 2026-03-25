namespace CrewBot.Classes
{
    public sealed class ScheduledVoidMessage
    {
        public ulong MessageID { get; set; }
        public ulong ChannelID { get; set; }
        public long DeleteAtUnixTimeSeconds { get; set; }
        public int RetryCount { get; set; }
    }
}
