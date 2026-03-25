namespace CrewBot.Classes
{
    public sealed class VoidChannelConfiguration
    {
        public ulong ChannelID { get; set; }
        public int DefaultTimerSeconds { get; set; } = 86400;
    }
}
