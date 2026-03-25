namespace CrewBot.Classes
{
    public static class BotRuntimeDefaults
    {
        // Timer interval values are in milliseconds.
        public const double ColorSelectionIntervalMs = 100000;
        public const double MessageCacheCleanupIntervalMs = 3600000;
        public const double ConfigRefreshIntervalMs = 5000;
        public const double MessageCacheFlushIntervalMs = 5000;
        public const double VoidMessageDeleteIntervalMs = 5000;
        public const int MessageHistoryCatchupBatchSize = 100;
    }
}
