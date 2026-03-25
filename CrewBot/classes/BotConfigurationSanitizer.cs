using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CrewBot.Classes
{
    public sealed class BotConfigurationSanitizerResult
    {
        public bool ConfigChanged { get; set; }
        public bool ColorChoicesChanged { get; set; }
        public bool VoidChannelsChanged { get; set; }
        public List<string> Warnings { get; } = new List<string>();
    }

    public static class BotConfigurationSanitizer
    {
        public static BotConfigurationSanitizerResult Sanitize(
            BotConfig config,
            IEnumerable<ulong> availableChannelIds,
            IEnumerable<ulong> availableRoleIds,
            ConcurrentDictionary<ulong, string> colorChoices,
            ConcurrentDictionary<ulong, VoidChannelConfiguration> voidChannels = null)
        {
            var result = new BotConfigurationSanitizerResult();
            if (config == null)
            {
                return result;
            }

            HashSet<ulong> channels = new HashSet<ulong>(availableChannelIds ?? Enumerable.Empty<ulong>());
            HashSet<ulong> roles = new HashSet<ulong>(availableRoleIds ?? Enumerable.Empty<ulong>());

            ValidateChannel("log channel", config.LogChannelID, channels, () => config.LogChannelID = 0, result);
            ValidateChannel("DM relay channel", config.DMChannelID, channels, () => config.DMChannelID = 0, result);
            ValidateChannel("general channel", config.GeneralChannelID, channels, () => config.GeneralChannelID = 0, result);
            ValidateChannel("color channel", config.ColorChannelID, channels, () => config.ColorChannelID = 0, result);

            if (config.ColorRoleID != 0 && !roles.Contains(config.ColorRoleID))
            {
                result.ConfigChanged = true;
                result.Warnings.Add($"Configured rotating color role {config.ColorRoleID} is missing; ColorRoleID was unset.");
                config.ColorRoleID = 0;
            }

            if (colorChoices != null)
            {
                List<ulong> invalidColorChoices = colorChoices.Keys.Where(id => !roles.Contains(id)).ToList();
                foreach (ulong invalidChoice in invalidColorChoices)
                {
                    result.ColorChoicesChanged = colorChoices.TryRemove(invalidChoice, out _) || result.ColorChoicesChanged;
                }

                if (invalidColorChoices.Count > 0)
                {
                    result.Warnings.Add($"Removed {invalidColorChoices.Count} missing color choice role(s) from configuration.");
                }
            }

            if (voidChannels != null)
            {
                List<ulong> invalidVoidChannels = voidChannels.Keys.Where(id => !channels.Contains(id)).ToList();
                foreach (ulong invalidVoidChannel in invalidVoidChannels)
                {
                    result.VoidChannelsChanged = voidChannels.TryRemove(invalidVoidChannel, out _) || result.VoidChannelsChanged;
                }

                if (invalidVoidChannels.Count > 0)
                {
                    result.Warnings.Add($"Removed {invalidVoidChannels.Count} missing void channel configuration(s).");
                }
            }

            return result;
        }

        private static void ValidateChannel(
            string label,
            ulong configuredId,
            HashSet<ulong> availableChannelIds,
            System.Action clearAction,
            BotConfigurationSanitizerResult result)
        {
            if (configuredId == 0 || availableChannelIds.Contains(configuredId))
            {
                return;
            }

            clearAction();
            result.ConfigChanged = true;
            result.Warnings.Add($"Configured {label} {configuredId} is missing; it was unset.");
        }
    }
}
