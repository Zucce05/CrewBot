using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CrewBot.Classes
{
    public static class BotStartup
    {
        private static readonly IJsonFileStore JsonStore = new JsonFileStore();
        private static string dataDirectoryOverride;
        public const string TokenEnvironmentVariableName = "CREWBOT_TOKEN";

        public static string DataDirectoryPath => dataDirectoryOverride ?? ResolveDataDirectory();
        public static string BotConfigPath => Path.Combine(DataDirectoryPath, "BotConfig.json");
        public static string ColorChoicesPath => Path.Combine(DataDirectoryPath, "ColorChoices.json");
        public static string TriggerResponsesPath => Path.Combine(DataDirectoryPath, "triggerResponses.json");
        public static string MessageCachePath => Path.Combine(DataDirectoryPath, "messageCache.json");
        public static string IgnoreMessageCachePath => Path.Combine(DataDirectoryPath, "ignoreMessageCache.json");
        public static string VoidChannelsPath => Path.Combine(DataDirectoryPath, "voidChannels.json");
        public static string VoidMessagesPath => Path.Combine(DataDirectoryPath, "voidMessages.json");
        public static string LogsDirectoryPath => Path.Combine(DataDirectoryPath, "logs");

        public static void SetDataDirectoryOverride(string path)
        {
            dataDirectoryOverride = string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path);
        }

        public static void ClearDataDirectoryOverride()
        {
            dataDirectoryOverride = null;
        }

        public static void Startup(
            ref BotConfig bc,
            ref ConcurrentDictionary<ulong, string> colors,
            ref ConcurrentDictionary<string, string> trigger,
            ref ConcurrentDictionary<ulong, LoggedMessage> message,
            ref List<ulong> ignoreMessages,
            ref ConcurrentDictionary<ulong, VoidChannelConfiguration> voidChannels,
            ref ConcurrentDictionary<ulong, ScheduledVoidMessage> voidMessages)
        {
            EnsureDefaultDataFilesExist();

            bc = ApplyEnvironmentOverrides(JsonStore.LoadOrDefault(BotConfigPath, new BotConfig()));
            colors = JsonStore.LoadOrDefault(ColorChoicesPath, new ConcurrentDictionary<ulong, string>());
            trigger = JsonStore.LoadOrDefault(TriggerResponsesPath, new ConcurrentDictionary<string, string>());
            message = JsonStore.LoadOrDefault(MessageCachePath, new ConcurrentDictionary<ulong, LoggedMessage>());
            ignoreMessages = JsonStore.LoadOrDefault(IgnoreMessageCachePath, new List<ulong>());
            voidChannels = JsonStore.LoadOrDefault(VoidChannelsPath, new ConcurrentDictionary<ulong, VoidChannelConfiguration>());
            voidMessages = JsonStore.LoadOrDefault(VoidMessagesPath, new ConcurrentDictionary<ulong, ScheduledVoidMessage>());
        }

        public static BotConfig LoadConfig()
        {
            EnsureDefaultDataFilesExist();
            return ApplyEnvironmentOverrides(JsonStore.LoadOrDefault(BotConfigPath, new BotConfig()));
        }

        public static void SaveConfig(BotConfig config)
        {
            EnsureDefaultDataFilesExist();
            JsonStore.Save(BotConfigPath, PrepareConfigForPersistence(config ?? new BotConfig()));
        }

        public static string GetEnvironmentToken()
        {
            return Environment.GetEnvironmentVariable(TokenEnvironmentVariableName);
        }

        private static void EnsureDefaultDataFilesExist()
        {
            Directory.CreateDirectory(DataDirectoryPath);
            Directory.CreateDirectory(LogsDirectoryPath);
            EnsureDefaultConfigFileExists();
            EnsureDataFileExists(ColorChoicesPath, new ConcurrentDictionary<ulong, string>());
            EnsureDataFileExists(TriggerResponsesPath, new ConcurrentDictionary<string, string>());
            EnsureDataFileExists(MessageCachePath, new ConcurrentDictionary<ulong, LoggedMessage>());
            EnsureDataFileExists(IgnoreMessageCachePath, new List<ulong>());
            EnsureDataFileExists(VoidChannelsPath, new ConcurrentDictionary<ulong, VoidChannelConfiguration>());
            EnsureDataFileExists(VoidMessagesPath, new ConcurrentDictionary<ulong, ScheduledVoidMessage>());
        }

        private static void EnsureDefaultConfigFileExists()
        {
            if (File.Exists(BotConfigPath))
            {
                return;
            }

            foreach (string candidatePath in GetBotConfigSeedPaths())
            {
                if (!File.Exists(candidatePath))
                {
                    continue;
                }

                string sourceText = File.ReadAllText(candidatePath);
                string cleanedText = candidatePath.EndsWith("BotConfig - Copy.json", StringComparison.OrdinalIgnoreCase)
                    ? RemoveJsonCommentLines(sourceText)
                    : sourceText;

                try
                {
                    BotConfig config = JsonConvert.DeserializeObject<BotConfig>(cleanedText);
                    JsonStore.Save(BotConfigPath, config ?? new BotConfig());
                    return;
                }
                catch (Exception)
                {
                    // Try the next candidate before falling back to defaults.
                }
            }

            JsonStore.Save(BotConfigPath, new BotConfig());
        }

        private static BotConfig ApplyEnvironmentOverrides(BotConfig config)
        {
            BotConfig resolvedConfig = CloneConfig(config ?? new BotConfig());
            string environmentToken = GetEnvironmentToken();
            if (!string.IsNullOrWhiteSpace(environmentToken))
            {
                resolvedConfig.Token = environmentToken;
            }

            return resolvedConfig;
        }

        private static BotConfig PrepareConfigForPersistence(BotConfig config)
        {
            BotConfig persistedConfig = CloneConfig(config ?? new BotConfig());
            if (!string.IsNullOrWhiteSpace(GetEnvironmentToken()))
            {
                persistedConfig.Token = null;
            }

            return persistedConfig;
        }

        private static BotConfig CloneConfig(BotConfig source)
        {
            return new BotConfig
            {
                BotEnabled = source.BotEnabled,
                SettingsAdminEnabled = source.SettingsAdminEnabled,
                Token = source.Token,
                GuildID = source.GuildID,
                LogChannelID = source.LogChannelID,
                DMChannelID = source.DMChannelID,
                BotID = source.BotID,
                AdminID = source.AdminID,
                AdditionalOwnerID = source.AdditionalOwnerID,
                GeneralChannelID = source.GeneralChannelID,
                ColorChannelID = source.ColorChannelID,
                Prefix = source.Prefix,
                ColorTimerSeconds = source.ColorTimerSeconds,
                ColorRoleID = source.ColorRoleID,
                ColorEnabled = source.ColorEnabled,
                ColorAdminEnabled = source.ColorAdminEnabled,
                TriggerEnabled = source.TriggerEnabled,
                TriggerAdminEnabled = source.TriggerAdminEnabled
            };
        }

        private static IEnumerable<string> GetBotConfigSeedPaths()
        {
            yield return Path.Combine(DataDirectoryPath, "BotConfig - Copy.json");

            string legacyDataDirectory = TryGetLegacyRootDataDirectory();
            if (!string.IsNullOrWhiteSpace(legacyDataDirectory))
            {
                yield return Path.Combine(legacyDataDirectory, "BotConfig.json");
                yield return Path.Combine(legacyDataDirectory, "BotConfig - Copy.json");
            }

            string projectTemplate = TryGetProjectTemplatePath();
            if (!string.IsNullOrWhiteSpace(projectTemplate))
            {
                yield return projectTemplate;
            }
        }

        private static void EnsureDataFileExists<T>(string canonicalPath, T defaultValue)
        {
            if (File.Exists(canonicalPath))
            {
                return;
            }

            string fileName = Path.GetFileName(canonicalPath);
            string legacyDataDirectory = TryGetLegacyRootDataDirectory();
            if (!string.IsNullOrWhiteSpace(legacyDataDirectory))
            {
                string legacyPath = Path.Combine(legacyDataDirectory, fileName);
                if (File.Exists(legacyPath))
                {
                    try
                    {
                        T legacyValue = JsonConvert.DeserializeObject<T>(File.ReadAllText(legacyPath));
                        JsonStore.Save(canonicalPath, legacyValue == null ? defaultValue : legacyValue);
                        return;
                    }
                    catch (Exception)
                    {
                        // Fall through to default creation below.
                    }
                }
            }

            JsonStore.Save(canonicalPath, defaultValue);
        }

        private static string ResolveDataDirectory()
        {
            foreach (string probeRoot in GetProbeRoots())
            {
                string dataDirectory = FindRepositoryDataDirectory(probeRoot);
                if (!string.IsNullOrWhiteSpace(dataDirectory))
                {
                    return dataDirectory;
                }
            }

            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "json"));
        }

        private static IEnumerable<string> GetProbeRoots()
        {
            yield return Directory.GetCurrentDirectory();
            yield return AppContext.BaseDirectory;
        }

        private static string FindRepositoryDataDirectory(string startPath)
        {
            if (string.IsNullOrWhiteSpace(startPath))
            {
                return null;
            }

            DirectoryInfo current = new DirectoryInfo(Path.GetFullPath(startPath));
            while (current != null)
            {
                string solutionPath = Path.Combine(current.FullName, "CrewBot.sln");
                string projectDataDirectory = Path.Combine(current.FullName, "CrewBot", "json");
                if (File.Exists(solutionPath) && Directory.Exists(projectDataDirectory))
                {
                    return projectDataDirectory;
                }

                current = current.Parent;
            }

            return null;
        }

        private static string TryGetLegacyRootDataDirectory()
        {
            DirectoryInfo dataDirectory = new DirectoryInfo(DataDirectoryPath);
            DirectoryInfo repositoryRoot = dataDirectory.Parent?.Parent;
            if (repositoryRoot == null)
            {
                return null;
            }

            string legacyDirectory = Path.Combine(repositoryRoot.FullName, "json");
            if (!Directory.Exists(legacyDirectory))
            {
                return null;
            }

            return string.Equals(Path.GetFullPath(legacyDirectory), Path.GetFullPath(DataDirectoryPath), StringComparison.OrdinalIgnoreCase)
                ? null
                : legacyDirectory;
        }

        private static string TryGetProjectTemplatePath()
        {
            string projectTemplatePath = Path.Combine(AppContext.BaseDirectory, "json", "BotConfig - Copy.json");
            if (File.Exists(projectTemplatePath))
            {
                return projectTemplatePath;
            }

            string repositoryDataDirectory = FindRepositoryDataDirectory(AppContext.BaseDirectory);
            if (string.IsNullOrWhiteSpace(repositoryDataDirectory))
            {
                return null;
            }

            string repositoryTemplatePath = Path.Combine(repositoryDataDirectory, "BotConfig - Copy.json");
            return File.Exists(repositoryTemplatePath) ? repositoryTemplatePath : null;
        }

        private static string RemoveJsonCommentLines(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            string[] lines = raw.Replace("\r", string.Empty).Split('\n');
            List<string> cleanedLines = new List<string>();

            foreach (string line in lines)
            {
                string trimmed = line.TrimStart();
                if (trimmed.StartsWith("//"))
                {
                    continue;
                }

                cleanedLines.Add(line);
            }

            return string.Join("\n", cleanedLines);
        }
    }
}
