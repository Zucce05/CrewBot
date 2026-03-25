using CrewBot.Classes;
using CrewBot.Classes.FactoryClasses;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace CrewBot
{
    class Program
    {
        public static Timer colorTimer;
        public static Timer messageDeleteTimer;
        public static Timer configRefreshTimer;
        public static Timer messageCacheFlushTimer;
        public static Timer voidMessageDeleteTimer;

        public static SocketGuild crewGuild;

        private static readonly Logging logging = new Logging();
        private static readonly IJsonFileStore JsonStore = new JsonFileStore();
        private static readonly GatewayIntents ConfiguredGatewayIntents =
            GatewayIntents.Guilds |
            GatewayIntents.GuildMembers |
            GatewayIntents.GuildPresences |
            GatewayIntents.GuildMessages |
            GatewayIntents.DirectMessages |
            GatewayIntents.MessageContent;

        public static DiscordSocketClient client;
        public static ConcurrentDictionary<ulong, string> colorChoices = new ConcurrentDictionary<ulong, string>();
        public static ConcurrentDictionary<string, string> triggerResponses = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<ulong, LoggedMessage> messageCache = new ConcurrentDictionary<ulong, LoggedMessage>();
        public static ConcurrentDictionary<ulong, VoidChannelConfiguration> voidChannels = new ConcurrentDictionary<ulong, VoidChannelConfiguration>();
        public static ConcurrentDictionary<ulong, ScheduledVoidMessage> voidMessages = new ConcurrentDictionary<ulong, ScheduledVoidMessage>();
        public static List<ulong> ignoreMessagesCache = new List<ulong>();
        private static int messageCacheDirty;
        private static int voidMessagesDirty;
        private static int historyCatchupInProgress;

        private readonly System.Threading.SemaphoreSlim runtimeStateLock = new System.Threading.SemaphoreSlim(1, 1);

        public BotConfig botConfig = new BotConfig();

        public static void Main()
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug,
                GatewayIntents = ConfiguredGatewayIntents
            });

            colorTimer = new Timer
            {
                Interval = BotRuntimeDefaults.ColorSelectionIntervalMs,
                AutoReset = true,
                Enabled = false
            };

            messageDeleteTimer = new Timer
            {
                Interval = BotRuntimeDefaults.MessageCacheCleanupIntervalMs,
                AutoReset = true,
                Enabled = false
            };

            configRefreshTimer = new Timer
            {
                Interval = BotRuntimeDefaults.ConfigRefreshIntervalMs,
                AutoReset = true,
                Enabled = false
            };

            messageCacheFlushTimer = new Timer
            {
                Interval = BotRuntimeDefaults.MessageCacheFlushIntervalMs,
                AutoReset = true,
                Enabled = false
            };
            voidMessageDeleteTimer = new Timer
            {
                Interval = BotRuntimeDefaults.VoidMessageDeleteIntervalMs,
                AutoReset = true,
                Enabled = false
            };

            BotStartup.Startup(ref botConfig, ref colorChoices, ref triggerResponses, ref messageCache, ref ignoreMessagesCache, ref voidChannels, ref voidMessages);

            client.Log += Log;
            client.Ready += Ready;
            client.MessageDeleted += MessageDeleted;
            client.MessageReceived += MessageReceived;
            client.SlashCommandExecuted += SlashCommandExecuted;
            client.UserJoined += UserJoined;
            client.UserLeft += UserLeft;

            colorTimer.Elapsed += ColorChangeSelection;
            messageDeleteTimer.Elapsed += ClearMessageCache;
            configRefreshTimer.Elapsed += RefreshRuntimeConfig;
            messageCacheFlushTimer.Elapsed += FlushMessageCache;
            voidMessageDeleteTimer.Elapsed += DeleteVoidMessages;

            messageDeleteTimer.Enabled = true;
            configRefreshTimer.Enabled = true;
            messageCacheFlushTimer.Enabled = true;
            voidMessageDeleteTimer.Enabled = true;

            await Log(new LogMessage(LogSeverity.Info, "Program", $"Using data directory: {BotStartup.DataDirectoryPath}"));
            await ApplyRuntimeConfigAsync(botConfig, forceReconnect: false);
            await Task.Delay(-1);
        }
        private async void RefreshRuntimeConfig(object sender, ElapsedEventArgs e)
        {
            await ApplyRuntimeConfigAsync(BotStartup.LoadConfig(), forceReconnect: false);
        }

        private async Task ApplyRuntimeConfigAsync(BotConfig latestConfig, bool forceReconnect)
        {
            await runtimeStateLock.WaitAsync();
            try
            {
                BotConfig nextConfig = latestConfig ?? new BotConfig();
                string previousToken = botConfig?.Token;
                bool tokenChanged = !string.Equals(previousToken, nextConfig.Token, StringComparison.Ordinal);
                double nextColorInterval = ResolveColorTimerInterval(nextConfig.ColorTimerSeconds);

                botConfig = nextConfig;
                if (Math.Abs(colorTimer.Interval - nextColorInterval) > double.Epsilon)
                {
                    colorTimer.Interval = nextColorInterval;
                    await Log(new LogMessage(LogSeverity.Info, "ColorRotation", $"Timer interval updated to {colorTimer.Interval} ms"));
                }

                if (!botConfig.BotEnabled)
                {
                    await EnsureClientStoppedAsync();
                    return;
                }

                if (string.IsNullOrWhiteSpace(botConfig.Token))
                {
                    await Log(new LogMessage(LogSeverity.Warning, "Program", $"BotEnabled is true but Token is not configured in {BotStartup.BotConfigPath}."));
                    await EnsureClientStoppedAsync();
                    return;
                }

                if (tokenChanged && client.LoginState == LoginState.LoggedIn)
                {
                    await EnsureClientStoppedAsync();
                }

                if (client.LoginState != LoginState.LoggedIn)
                {
                    await client.LoginAsync(TokenType.Bot, botConfig.Token);
                }

                if (forceReconnect || client.ConnectionState != ConnectionState.Connected)
                {
                    await client.StartAsync();
                }

                UpdateCrewGuildObject(botConfig);
                await ValidateRuntimeConfigurationAsync();
            }
            catch (Exception exception)
            {
                await Log(new LogMessage(LogSeverity.Error, "Program", $"ApplyRuntimeConfigAsync: {exception.Message}"));
            }
            finally
            {
                runtimeStateLock.Release();
            }
        }

        private async Task EnsureClientStoppedAsync()
        {
            colorTimer.Enabled = false;
            crewGuild = null;
            FlushMessageCacheToDisk();
            FlushVoidMessagesToDisk();

            try
            {
                if (client != null && client.ConnectionState != ConnectionState.Disconnected)
                {
                    await client.StopAsync();
                }
            }
            catch (Exception exception)
            {
                await Log(new LogMessage(LogSeverity.Warning, "Program", $"StopAsync failed: {exception.Message}"));
            }

            try
            {
                if (client != null && client.LoginState != LoginState.LoggedOut)
                {
                    await client.LogoutAsync();
                }
            }
            catch (Exception exception)
            {
                await Log(new LogMessage(LogSeverity.Warning, "Program", $"LogoutAsync failed: {exception.Message}"));
            }
        }

        private static double ResolveColorTimerInterval(int configuredSeconds)
        {
            if (configuredSeconds <= 0)
            {
                return BotRuntimeDefaults.ColorSelectionIntervalMs;
            }

            return TimeSpan.FromSeconds(configuredSeconds).TotalMilliseconds;
        }

        private async Task Ready()
        {
            UpdateCrewGuildObject(botConfig);
            await LogRuntimeDiagnosticsAsync();
            await RegisterSlashCommandsAsync();
            await ValidateRuntimeConfigurationAsync();
            _ = CatchUpMissedMessagesAsync();
        }

        private async Task SlashCommandExecuted(SocketSlashCommand command)
        {
            try
            {
                await SlashCommandFactory.HandleAsync(command, botConfig, crewGuild, triggerResponses, colorChoices, voidChannels);
            }
            catch (Exception exception)
            {
                await Log(new LogMessage(LogSeverity.Error, "SlashCommand", exception.Message));
                if (command != null && !command.HasResponded)
                {
                    await command.RespondAsync("Something went wrong while processing that slash command.", ephemeral: true);
                }
            }
        }

        private void UpdateFeatureTimers()
        {
            bool shouldEnableColorTimer =
                botConfig.BotEnabled &&
                botConfig.ColorEnabled &&
                botConfig.ColorRoleID != 0 &&
                colorChoices.Count > 0 &&
                crewGuild != null &&
                client.ConnectionState == ConnectionState.Connected;

            colorTimer.Enabled = shouldEnableColorTimer;
        }

        private static void UpdateCrewGuildObject(BotConfig bc)
        {
            _ = Log(new LogMessage(LogSeverity.Verbose, "Program", "UpdateCrewGuildObject"));
            crewGuild = bc == null || bc.GuildID == 0 ? null : client.GetGuild(bc.GuildID);
        }

        private async Task ValidateRuntimeConfigurationAsync()
        {
            if (crewGuild == null)
            {
                UpdateFeatureTimers();
                return;
            }

            BotConfigurationSanitizerResult result = BotConfigurationSanitizer.Sanitize(
                botConfig,
                crewGuild.TextChannels.Select(channel => channel.Id),
                crewGuild.Roles.Select(role => role.Id),
                colorChoices,
                voidChannels);

            foreach (string warning in result.Warnings)
            {
                await Log(new LogMessage(LogSeverity.Warning, "Program", warning));
            }

            if (result.ConfigChanged)
            {
                BotStartup.SaveConfig(botConfig);
            }

            if (result.ColorChoicesChanged)
            {
                JsonStore.Save(BotStartup.ColorChoicesPath, colorChoices);
            }

            if (result.VoidChannelsChanged)
            {
                JsonStore.Save(BotStartup.VoidChannelsPath, voidChannels);
            }

            UpdateFeatureTimers();
        }

        private async Task RegisterSlashCommandsAsync()
        {
            try
            {
                await SlashCommandFactory.RegisterGuildCommandsAsync(crewGuild);
                if (crewGuild != null)
                {
                    await Log(new LogMessage(LogSeverity.Info, "SlashCommand", $"Registered guild slash commands for guild {crewGuild.Id}"));
                }
            }
            catch (Exception exception)
            {
                await Log(new LogMessage(LogSeverity.Error, "SlashCommand", $"RegisterSlashCommandsAsync: {exception.Message}"));
            }
        }

        private static SocketTextChannel GetConfiguredTextChannel(ulong channelId)
        {
            if (crewGuild == null || channelId == 0)
            {
                return null;
            }

            return crewGuild.GetTextChannel(channelId);
        }

        private async void ClearMessageCache(object sender, ElapsedEventArgs e)
        {
            bool removedAny = false;
            DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddDays(-14);

            foreach (LoggedMessage message in messageCache.Values)
            {
                if (message.messageDateTimeOffset < cutoff)
                {
                    removedAny = messageCache.TryRemove(message.messageID, out _) || removedAny;
                }
            }

            if (removedAny)
            {
                MarkMessageCacheDirty();
                FlushMessageCacheToDisk();
                await Log(new LogMessage(LogSeverity.Verbose, "Program", "Expired messages cleared from messageCache"));
            }
        }

        private async void FlushMessageCache(object sender, ElapsedEventArgs e)
        {
            try
            {
                bool flushedMessageCache = FlushMessageCacheToDisk();
                bool flushedVoidMessages = FlushVoidMessagesToDisk();
                if (!flushedMessageCache && !flushedVoidMessages)
                {
                    return;
                }

                if (flushedMessageCache)
                {
                    await Log(new LogMessage(LogSeverity.Verbose, "Program", "messageCache.json flushed to disk"));
                }

                if (flushedVoidMessages)
                {
                    await Log(new LogMessage(LogSeverity.Verbose, "VoidMessage", "voidMessages.json flushed to disk"));
                }
            }
            catch (Exception exception)
            {
                await Log(new LogMessage(LogSeverity.Error, "Program", $"FlushMessageCache: {exception.Message}"));
            }
        }

        private static void MarkMessageCacheDirty()
        {
            System.Threading.Interlocked.Exchange(ref messageCacheDirty, 1);
        }

        private static void MarkVoidMessagesDirty()
        {
            System.Threading.Interlocked.Exchange(ref voidMessagesDirty, 1);
        }

        private static bool FlushMessageCacheToDisk()
        {
            if (System.Threading.Interlocked.Exchange(ref messageCacheDirty, 0) == 0)
            {
                return false;
            }

            try
            {
                JsonStore.Save(BotStartup.MessageCachePath, messageCache);
                return true;
            }
            catch
            {
                System.Threading.Interlocked.Exchange(ref messageCacheDirty, 1);
                throw;
            }
        }

        private static bool FlushVoidMessagesToDisk()
        {
            if (System.Threading.Interlocked.Exchange(ref voidMessagesDirty, 0) == 0)
            {
                return false;
            }

            try
            {
                JsonStore.Save(BotStartup.VoidMessagesPath, voidMessages);
                return true;
            }
            catch
            {
                System.Threading.Interlocked.Exchange(ref voidMessagesDirty, 1);
                throw;
            }
        }

        private async Task UserJoined(SocketGuildUser user)
        {
            SocketTextChannel generalChannel = GetConfiguredTextChannel(botConfig.GeneralChannelID);
            if (generalChannel == null)
            {
                return;
            }

            string colorInstructions = botConfig.ColorChannelID != 0
                ? $" Visit <#{botConfig.ColorChannelID}> to pick your color and unlock channels!"
                : " Pick your color to unlock channels!";

            string msg = $"Welcome <@{user.Id}>!{colorInstructions}\nWe hope you enjoy it here!";
            await generalChannel.SendMessageAsync(msg);
        }

        private async Task UserLeft(SocketGuild guild, SocketUser user)
        {
            SocketTextChannel generalChannel = GetConfiguredTextChannel(botConfig.GeneralChannelID);
            if (generalChannel == null)
            {
                return;
            }

            string msg = $"{user.Username} just left.\nhttps://media.giphy.com/media/2ept7eRuyq98s/giphy.gif";
            await generalChannel.SendMessageAsync(msg);
        }

        private async Task MessageDeleted(Cacheable<IMessage, ulong> deletedMessage, Cacheable<IMessageChannel, ulong> channelCache)
        {
            IMessageChannel channel = await channelCache.GetOrDownloadAsync();
            string channelName = channel?.Name ?? "Unknown Channel";

            EmbedBuilder embedBuilder = new EmbedBuilder
            {
                Title = "Message Deleted",
                Description = $"{channelName}\n**Message ID:** {deletedMessage.Id}",
                ThumbnailUrl = crewGuild?.IconUrl
            };

            if (messageCache.TryRemove(deletedMessage.Id, out LoggedMessage message))
            {
                embedBuilder.Description += $"\n**Content:** {message.messageContent}\n**Author:** {message.messageAuthorUsername}";
                embedBuilder.ThumbnailUrl = message.messageAvatarURL;
                MarkMessageCacheDirty();
            }

            await DiscordLogMessage(embedBuilder);
        }

        private async Task DiscordLogMessage(EmbedBuilder builder)
        {
            SocketTextChannel logChannel = GetConfiguredTextChannel(botConfig.LogChannelID);
            if (logChannel == null)
            {
                return;
            }

            await logChannel.SendMessageAsync(string.Empty, false, builder.Build());
        }

        private async Task AddMessageToLog(SocketMessage message)
        {
            _ = Log(new LogMessage(LogSeverity.Verbose, "Program", "MessageReceived"));
            _ = LogMessageDiagnosticsAsync(message);

            await ObservePassiveMessageAsync(message, isReplay: false);

            SocketTextChannel dmChannel = GetConfiguredTextChannel(botConfig.DMChannelID);
            if (message.Channel is IDMChannel && message.Author.MutualGuilds.Count > 0 && dmChannel != null)
            {
                EmbedBuilder builder = new EmbedBuilder
                {
                    ThumbnailUrl = message.Author.GetAvatarUrl(),
                    Title = $"From: {message.Author.Username}",
                    Description = message.Content,
                };

                await dmChannel.SendMessageAsync(string.Empty, false, builder.Build());
            }

            if (dmChannel != null && message.MentionedUsers.Count > 0)
            {
                foreach (SocketUser user in message.MentionedUsers)
                {
                    if (user.Id == botConfig.BotID)
                    {
                        EmbedBuilder builder = new EmbedBuilder
                        {
                            ThumbnailUrl = message.Author.GetAvatarUrl(),
                            Title = $"From: {message.Author.Username} in {message.Channel.Name}",
                            Description = message.Content,
                        };

                        await dmChannel.SendMessageAsync(message.GetJumpUrl(), false, builder.Build());
                        break;
                    }
                }
            }
            else if (dmChannel != null && message.MentionedRoles.Count > 0)
            {
                foreach (SocketRole role in message.MentionedRoles)
                {
                    if (role.Id == botConfig.AdminID)
                    {
                        EmbedBuilder builder = new EmbedBuilder
                        {
                            ThumbnailUrl = message.Author.GetAvatarUrl(),
                            Title = $"From: {message.Author.Username} in {message.Channel.Name}",
                            Description = message.Content,
                        };

                        await dmChannel.SendMessageAsync(message.GetJumpUrl(), false, builder.Build());
                        break;
                    }
                }
            }

            try
            {
                await MessageCommandFactory.MessageCommand(message, botConfig, crewGuild, triggerResponses, colorChoices);
            }
            catch (NullReferenceException e)
            {
                await Log(new LogMessage(LogSeverity.Error, "MessageCommand", e.Message));
            }
        }

        private async Task MessageReceived(SocketMessage message)
        {
            await AddMessageToLog(message);
        }

        private async Task<(bool CacheAdded, bool VoidQueued)> ObservePassiveMessageAsync(IMessage message, bool isReplay)
        {
            bool cacheAdded = false;
            bool voidQueued = false;

            if (message == null)
            {
                return (false, false);
            }

            try
            {
                if (ignoreMessagesCache != null && !ignoreMessagesCache.Contains(message.Channel.Id))
                {
                    LoggedMessage messageToLog = new LoggedMessage(message);
                    if (messageCache.TryAdd(message.Id, messageToLog))
                    {
                        MarkMessageCacheDirty();
                        cacheAdded = true;
                    }
                }
            }
            catch (Exception e)
            {
                await Log(new LogMessage(LogSeverity.Error, "Program", $"ObservePassiveMessageAsync cache error: {e.Message}"));
            }

            try
            {
                voidQueued = await QueueVoidMessageAsync(message, isReplay);
            }
            catch (Exception exception)
            {
                await Log(new LogMessage(LogSeverity.Error, "VoidMessage", $"QueueVoidMessageAsync: {exception.Message}"));
            }

            return (cacheAdded, voidQueued);
        }

        private async Task CatchUpMissedMessagesAsync()
        {
            if (crewGuild == null || client.ConnectionState != ConnectionState.Connected)
            {
                return;
            }

            if (System.Threading.Interlocked.Exchange(ref historyCatchupInProgress, 1) == 1)
            {
                await Log(new LogMessage(LogSeverity.Verbose, "HistoryCatchup", "Skipped starting message catch-up because another pass is already running"));
                return;
            }

            try
            {
                List<SocketTextChannel> channelsToReplay = crewGuild.TextChannels
                    .Where(ShouldReplayChannel)
                    .OrderBy(channel => channel.Position)
                    .ToList();

                int channelCount = 0;
                int fetchedCount = 0;
                int cachedCount = 0;
                int voidQueuedCount = 0;
                int anchoredChannelCount = 0;

                await Log(new LogMessage(LogSeverity.Info, "HistoryCatchup", $"Starting startup message catch-up across {channelsToReplay.Count} channel(s)"));

                foreach (SocketTextChannel channel in channelsToReplay)
                {
                    (int fetched, int cached, int voidQueued, bool foundAnchor) = await CatchUpChannelAsync(channel);
                    channelCount += 1;
                    fetchedCount += fetched;
                    cachedCount += cached;
                    voidQueuedCount += voidQueued;
                    if (foundAnchor)
                    {
                        anchoredChannelCount += 1;
                    }
                }

                FlushMessageCacheToDisk();
                FlushVoidMessagesToDisk();

                await Log(new LogMessage(
                    LogSeverity.Info,
                    "HistoryCatchup",
                    $"Completed startup catch-up. Channels={channelCount}; MessagesFetched={fetchedCount}; MessagesCached={cachedCount}; VoidQueued={voidQueuedCount}; ChannelsAnchored={anchoredChannelCount}"));
            }
            catch (Exception exception)
            {
                await Log(new LogMessage(LogSeverity.Error, "HistoryCatchup", $"CatchUpMissedMessagesAsync: {exception.Message}"));
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref historyCatchupInProgress, 0);
            }
        }

        private bool ShouldReplayChannel(SocketTextChannel channel)
        {
            return channel != null && voidChannels.ContainsKey(channel.Id);
        }

        private async Task<(int Fetched, int Cached, int VoidQueued, bool FoundAnchor)> CatchUpChannelAsync(SocketTextChannel channel)
        {
            int fetchedCount = 0;
            int cachedCount = 0;
            int voidQueuedCount = 0;
            bool foundAnchor = false;
            ulong? beforeMessageId = null;

            try
            {
                while (true)
                {
                    IEnumerable<IMessage> fetchedBatch = beforeMessageId.HasValue
                        ? await channel.GetMessagesAsync(beforeMessageId.Value, Direction.Before, BotRuntimeDefaults.MessageHistoryCatchupBatchSize).FlattenAsync()
                        : await channel.GetMessagesAsync(BotRuntimeDefaults.MessageHistoryCatchupBatchSize).FlattenAsync();

                    List<IMessage> messages = fetchedBatch
                        .Where(message => message != null)
                        .OrderByDescending(message => message.Id)
                        .ToList();

                    if (messages.Count == 0)
                    {
                        break;
                    }

                    fetchedCount += messages.Count;

                    foreach (IMessage historicMessage in messages)
                    {
                        if (messageCache.ContainsKey(historicMessage.Id))
                        {
                            foundAnchor = true;
                            break;
                        }

                        (bool cacheAdded, bool voidQueued) = await ObservePassiveMessageAsync(historicMessage, isReplay: true);
                        if (cacheAdded)
                        {
                            cachedCount += 1;
                        }

                        if (voidQueued)
                        {
                            voidQueuedCount += 1;
                        }
                    }

                    if (foundAnchor || messages.Count < BotRuntimeDefaults.MessageHistoryCatchupBatchSize)
                    {
                        break;
                    }

                    beforeMessageId = messages.Min(message => message.Id);
                }
            }
            catch (Exception exception)
            {
                await Log(new LogMessage(LogSeverity.Warning, "HistoryCatchup", $"Channel {channel.Id} catch-up failed: {exception.Message}"));
            }

            if (fetchedCount > 0 || voidQueuedCount > 0)
            {
                await Log(new LogMessage(
                    LogSeverity.Info,
                    "HistoryCatchup",
                    $"Channel {channel.Id} catch-up fetched={fetchedCount}; cached={cachedCount}; voidQueued={voidQueuedCount}; foundAnchor={foundAnchor}"));
            }

            return (fetchedCount, cachedCount, voidQueuedCount, foundAnchor);
        }

        private async Task<bool> QueueVoidMessageAsync(IMessage message, bool isReplay)
        {
            if (message == null)
            {
                return false;
            }

            SocketTextChannel textChannel = crewGuild?.GetTextChannel(message.Channel.Id);
            if (textChannel == null)
            {
                return false;
            }

            if (!voidChannels.TryGetValue(textChannel.Id, out VoidChannelConfiguration channelConfiguration))
            {
                return false;
            }

            if (message.IsPinned)
            {
                await Log(new LogMessage(
                    LogSeverity.Info,
                    "VoidMessage",
                    $"Skipping pinned message {message.Id} in channel {textChannel.Id}. {GetVoidMessageDetails(message)}"));
                return false;
            }

            string prefix = MessageCommandParser.NormalizePrefix(botConfig?.Prefix);
            int timerSeconds = channelConfiguration.DefaultTimerSeconds;
            if (VoidTimingParser.TryParsePrefixedDurationToken(message.Content, prefix, out int overriddenTimerSeconds))
            {
                timerSeconds = overriddenTimerSeconds;
            }

            long deleteAtUnixTimeSeconds = VoidMessageScheduler.ResolveDeleteAtUnixTimeSeconds(message, timerSeconds);
            bool dueImmediately = deleteAtUnixTimeSeconds <= DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string replayLabel = isReplay ? "replay " : string.Empty;

            var scheduledMessage = new ScheduledVoidMessage
            {
                MessageID = message.Id,
                ChannelID = textChannel.Id,
                DeleteAtUnixTimeSeconds = deleteAtUnixTimeSeconds,
                RetryCount = 0
            };

            voidMessages.AddOrUpdate(message.Id, scheduledMessage, (_, _) => scheduledMessage);
            MarkVoidMessagesDirty();

            await Log(new LogMessage(
                LogSeverity.Info,
                "VoidMessage",
                $"Queued {replayLabel}message {message.Id} in channel {textChannel.Id} for deletion in {timerSeconds} second(s). DueImmediately={dueImmediately}; DeleteAtUnix={deleteAtUnixTimeSeconds}. {GetVoidMessageDetails(message)}"));
            return true;
        }

        private async void DeleteVoidMessages(object sender, ElapsedEventArgs e)
        {
            if (!botConfig.BotEnabled || client.ConnectionState != ConnectionState.Connected || crewGuild == null || voidMessages.Count == 0)
            {
                return;
            }

            List<ScheduledVoidMessage> dueMessages = voidMessages.Values
                .Where(message => message.DeleteAtUnixTimeSeconds <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                .ToList();

            foreach (ScheduledVoidMessage scheduledMessage in dueMessages)
            {
                try
                {
                    SocketTextChannel channel = crewGuild.GetTextChannel(scheduledMessage.ChannelID);
                    if (channel == null)
                    {
                        if (voidMessages.TryRemove(scheduledMessage.MessageID, out _))
                        {
                            MarkVoidMessagesDirty();
                        }

                        await Log(new LogMessage(LogSeverity.Warning, "VoidMessage", $"Configured void channel {scheduledMessage.ChannelID} is missing; removed queued message {scheduledMessage.MessageID}."));
                        continue;
                    }

                    IMessage queuedMessage = await channel.GetMessageAsync(scheduledMessage.MessageID);
                    if (queuedMessage == null)
                    {
                        if (voidMessages.TryRemove(scheduledMessage.MessageID, out _))
                        {
                            MarkVoidMessagesDirty();
                        }

                        await Log(new LogMessage(LogSeverity.Info, "VoidMessage", $"Message {scheduledMessage.MessageID} was already gone from channel {scheduledMessage.ChannelID}; removed from queue"));
                        continue;
                    }

                    if (queuedMessage.IsPinned)
                    {
                        if (voidMessages.TryRemove(scheduledMessage.MessageID, out _))
                        {
                            MarkVoidMessagesDirty();
                        }

                        await Log(new LogMessage(LogSeverity.Info, "VoidMessage", $"Pinned message {scheduledMessage.MessageID} in channel {scheduledMessage.ChannelID} was preserved and removed from the void queue"));
                        continue;
                    }

                    await channel.DeleteMessageAsync(scheduledMessage.MessageID);
                    if (voidMessages.TryRemove(scheduledMessage.MessageID, out _))
                    {
                        MarkVoidMessagesDirty();
                    }

                    await Log(new LogMessage(LogSeverity.Info, "VoidMessage", $"Deleted void message {scheduledMessage.MessageID} from channel {scheduledMessage.ChannelID}"));
                }
                catch (Discord.Net.HttpException exception)
                {
                    if (scheduledMessage.RetryCount >= 5)
                    {
                        voidMessages.TryRemove(scheduledMessage.MessageID, out _);
                        MarkVoidMessagesDirty();
                        await Log(new LogMessage(LogSeverity.Warning, "VoidMessage", $"Dropping queued message {scheduledMessage.MessageID} after repeated delete failures: {exception.Message}"));
                        continue;
                    }

                    scheduledMessage.RetryCount += 1;
                    voidMessages[scheduledMessage.MessageID] = scheduledMessage;
                    MarkVoidMessagesDirty();
                    await Log(new LogMessage(LogSeverity.Warning, "VoidMessage", $"Retry {scheduledMessage.RetryCount} deleting message {scheduledMessage.MessageID}: {exception.Message}"));
                }
                catch (Exception exception)
                {
                    if (scheduledMessage.RetryCount >= 5)
                    {
                        voidMessages.TryRemove(scheduledMessage.MessageID, out _);
                        MarkVoidMessagesDirty();
                        await Log(new LogMessage(LogSeverity.Warning, "VoidMessage", $"Dropping queued message {scheduledMessage.MessageID} after repeated delete failures: {exception.Message}"));
                        continue;
                    }

                    scheduledMessage.RetryCount += 1;
                    voidMessages[scheduledMessage.MessageID] = scheduledMessage;
                    MarkVoidMessagesDirty();
                    await Log(new LogMessage(LogSeverity.Warning, "VoidMessage", $"Retry {scheduledMessage.RetryCount} deleting message {scheduledMessage.MessageID}: {exception.Message}"));
                }
            }
        }

        async void ColorChangeSelection(object sender, ElapsedEventArgs e)
        {
            SocketGuildUser user = null;
            SocketRole nextRole = null;

            try
            {
                UpdateCrewGuildObject(botConfig);
                if (!botConfig.BotEnabled)
                {
                    await Log(new LogMessage(LogSeverity.Info, "ColorRotation", "Skipping: BotEnabled is false"));
                    return;
                }

                if (!botConfig.ColorEnabled)
                {
                    await Log(new LogMessage(LogSeverity.Info, "ColorRotation", "Skipping: ColorEnabled is false"));
                    return;
                }

                if (botConfig.ColorRoleID == 0)
                {
                    await Log(new LogMessage(LogSeverity.Info, "ColorRotation", "Skipping: ColorRoleID is 0"));
                    return;
                }

                if (crewGuild == null)
                {
                    await Log(new LogMessage(LogSeverity.Info, "ColorRotation", $"Skipping: crewGuild is null for GuildID {botConfig.GuildID}"));
                    return;
                }

                if (colorChoices.Count == 0)
                {
                    await Log(new LogMessage(LogSeverity.Info, "ColorRotation", "Skipping: colorChoices is empty"));
                    return;
                }

                SocketRole rotatingRole = crewGuild.GetRole(botConfig.ColorRoleID);
                if (rotatingRole == null)
                {
                    await Log(new LogMessage(LogSeverity.Info, "ColorRotation", $"Skipping: rotating role {botConfig.ColorRoleID} was not found in guild {crewGuild.Id}"));
                    return;
                }

                int rotatingMemberCount = rotatingRole.Members.Count();
                if (rotatingMemberCount == 0)
                {
                    await Log(new LogMessage(LogSeverity.Info, "ColorRotation", $"Skipping: rotating role {rotatingRole.Name} ({rotatingRole.Id}) has no cached members"));
                    return;
                }

                List<SocketRole> colorRoles = colorChoices.Keys
                    .Select(crewGuild.GetRole)
                    .Where(role => role != null)
                    .ToList();

                if (colorRoles.Count == 0)
                {
                    await Log(new LogMessage(LogSeverity.Info, "ColorRotation", $"Skipping: none of the {colorChoices.Count} configured color role IDs resolved in guild {crewGuild.Id}"));
                    return;
                }

                user = rotatingRole.Members.ElementAt(Random.Shared.Next(rotatingMemberCount));
                nextRole = colorRoles[Random.Shared.Next(colorRoles.Count)];

                IReadOnlyList<ulong> assignedColorRoleIds = ColorRotationPlanner.GetAssignedColorRoleIds(
                    user.Roles.Select(role => role.Id),
                    colorRoles.Select(role => role.Id));

                List<SocketRole> assignedColorRoles = colorRoles
                    .Where(role => assignedColorRoleIds.Contains(role.Id))
                    .ToList();
                List<SocketRole> removableColorRoles = assignedColorRoles
                    .Where(role => role.Id != nextRole.Id)
                    .ToList();
                bool alreadyHasNextRole = assignedColorRoleIds.Contains(nextRole.Id);

                await Log(new LogMessage(
                    LogSeverity.Info,
                    "ColorRotation",
                    $"Selected user {user.Username} ({user.Id}); CurrentColorRoleCount={assignedColorRoles.Count}; NextColorRole={nextRole.Name} ({nextRole.Id}); AlreadyHasNextRole={alreadyHasNextRole}"));

                if (!alreadyHasNextRole)
                {
                    await user.AddRoleAsync(nextRole);
                }

                if (removableColorRoles.Count > 0)
                {
                    await user.RemoveRolesAsync(removableColorRoles);
                }

                await Log(new LogMessage(
                    LogSeverity.Info,
                    "ColorRotation",
                    $"Applied color role {nextRole.Name} ({nextRole.Id}) to user {user.Username} ({user.Id})"));
            }
            catch (Discord.Net.HttpException exception)
            {
                _ = Log(new LogMessage(LogSeverity.Error, "ColorRotation", $"User={user?.Id.ToString() ?? "(unknown)"}; NextRole={nextRole?.Id.ToString() ?? "(unknown)"}; {exception.Message}"));
            }
            catch (System.Net.Http.HttpRequestException exception)
            {
                _ = Log(new LogMessage(LogSeverity.Error, "ColorRotation", $"User={user?.Id.ToString() ?? "(unknown)"}; NextRole={nextRole?.Id.ToString() ?? "(unknown)"}; {exception.Message}"));
            }
            catch (OperationCanceledException exception)
            {
                _ = Log(new LogMessage(LogSeverity.Error, "ColorRotation", $"User={user?.Id.ToString() ?? "(unknown)"}; NextRole={nextRole?.Id.ToString() ?? "(unknown)"}; {exception.Message}"));
            }
            catch (Exception exception)
            {
                _ = Log(new LogMessage(LogSeverity.Error, "ColorRotation", $"User={user?.Id.ToString() ?? "(unknown)"}; NextRole={nextRole?.Id.ToString() ?? "(unknown)"}; {exception.Message}"));
            }
        }

        public static Task Log(LogMessage msg)
        {
            logging.Log(msg);
            return Task.CompletedTask;
        }

        private async Task LogRuntimeDiagnosticsAsync()
        {
            string tokenSource = string.IsNullOrWhiteSpace(BotStartup.GetEnvironmentToken()) ? "config-file" : "environment";
            string currentUser = client?.CurrentUser == null
                ? "(unknown)"
                : $"{client.CurrentUser.Username} ({client.CurrentUser.Id})";

            await Log(new LogMessage(
                LogSeverity.Info,
                "RuntimeDiagnostics",
                $"ConnectedUser={currentUser}; GuildID={botConfig.GuildID}; ConfigBotID={botConfig.BotID}; Prefix={MessageCommandParser.NormalizePrefix(botConfig.Prefix)}; TokenSource={tokenSource}; GatewayIntents={ConfiguredGatewayIntents}"));
        }

        private async Task LogMessageDiagnosticsAsync(SocketMessage message)
        {
            if (message == null)
            {
                return;
            }

            string prefix = MessageCommandParser.NormalizePrefix(botConfig?.Prefix);
            string content = message.Content ?? string.Empty;
            bool isDm = message.Channel is IDMChannel;
            bool isPrefixed = MessageCommandParser.IsPrefixed(content, prefix);
            string contentPreview = content.Length == 0
                ? "(empty)"
                : content.Replace("\r", "\\r").Replace("\n", "\\n").Substring(0, Math.Min(content.Length, 120));

            await Log(new LogMessage(
                LogSeverity.Info,
                "MessageDiagnostics",
                $"MessageId={message.Id}; Author={message.Author?.Username} ({message.Author?.Id}); ChannelId={message.Channel.Id}; ChannelType={(isDm ? "DM" : "Guild")}; MessageType={message.Type}; Source={message.Source}; ContentLength={content.Length}; Attachments={message.Attachments.Count}; Embeds={message.Embeds.Count}; Stickers={message.Stickers.Count}; MentionedUsers={message.MentionedUsers.Count}; MentionedRoles={message.MentionedRoles.Count}; IsPrefixed={isPrefixed}; Prefix={prefix}; Preview={contentPreview}"));

            if (content.Length == 0 && message.Attachments.Count == 0 && message.Embeds.Count == 0 && message.Stickers.Count == 0 && !(message.Author?.IsBot ?? false))
            {
                await Log(new LogMessage(
                    LogSeverity.Warning,
                    "MessageDiagnostics",
                    $"Blank content received for non-bot message {message.Id} in channel {message.Channel.Id}. ChannelType={(isDm ? "DM" : "Guild")}; MessageType={message.Type}; Source={message.Source}"));
            }
        }

        private static string GetVoidMessageDetails(IMessage message)
        {
            return $"Type={message.Type}; Source={message.Source}; CreatedAt={message.CreatedAt:O}; ContentLength={message.Content?.Length ?? 0}; Attachments={message.Attachments.Count}; Embeds={message.Embeds.Count}; Stickers={message.Stickers.Count}; AuthorIsBot={message.Author?.IsBot ?? false}; AuthorIsWebhook={message.Author?.IsWebhook ?? false}";
        }
    }
}
