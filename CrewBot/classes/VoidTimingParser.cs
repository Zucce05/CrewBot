using System;
using System.Text.RegularExpressions;

namespace CrewBot.Classes
{
    public static class VoidTimingParser
    {
        public const int MinimumTimerSeconds = 1;
        public const int MaximumTimerSeconds = 604800;
        private static readonly Regex DurationRegex = new Regex(@"^(?<value>\d{1,7})(?<unit>[smhd])$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool TryParseDuration(string raw, out int seconds)
        {
            seconds = 0;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            Match match = DurationRegex.Match(raw.Trim());
            if (!match.Success)
            {
                return false;
            }

            if (!int.TryParse(match.Groups["value"].Value, out int value) || value <= 0)
            {
                return false;
            }

            char unit = char.ToLowerInvariant(match.Groups["unit"].Value[0]);
            long convertedSeconds = unit switch
            {
                's' => value,
                'm' => value * 60L,
                'h' => value * 3600L,
                'd' => value * 86400L,
                _ => 0L
            };

            if (convertedSeconds < MinimumTimerSeconds)
            {
                convertedSeconds = MinimumTimerSeconds;
            }

            if (convertedSeconds > MaximumTimerSeconds)
            {
                convertedSeconds = MaximumTimerSeconds;
            }

            seconds = (int)convertedSeconds;
            return true;
        }

        public static bool TryParsePrefixedDurationToken(string content, string prefix, out int seconds)
        {
            seconds = 0;
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            string normalizedPrefix = Classes.FactoryClasses.MessageCommandParser.NormalizePrefix(prefix);
            string[] blocks = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (blocks.Length == 0)
            {
                return false;
            }

            string token = blocks[0];
            if (!token.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string durationText = token.Substring(normalizedPrefix.Length);
            return TryParseDuration(durationText, out seconds);
        }
    }
}
