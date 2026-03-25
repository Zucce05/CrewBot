using System;

namespace CrewBot.Classes.FactoryClasses
{
    public static class MessageCommandParser
    {
        public static string NormalizePrefix(string prefix)
        {
            return string.IsNullOrWhiteSpace(prefix) ? "+" : prefix;
        }

        public static string[] GetBlocks(string content)
        {
            return (content ?? string.Empty)
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        public static bool IsPrefixed(string content, string prefix)
        {
            string normalizedPrefix = NormalizePrefix(prefix);
            return (content ?? string.Empty).StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static string ExtractCommand(string content, string prefix)
        {
            string[] blocks = GetBlocks(content);
            if (blocks.Length == 0)
            {
                return string.Empty;
            }

            string command = blocks[0];
            string normalizedPrefix = NormalizePrefix(prefix);
            if (command.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                command = command.Substring(normalizedPrefix.Length);
            }

            return command;
        }
    }
}