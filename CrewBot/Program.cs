using CrewBot.Classes;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Timers;
using CrewBot.Classes.FactoryClasses;
using CrewBot.Interfaces;
using System.Collections.Concurrent;

namespace CrewBot
{
    class Program
    {
        // Timer for the color role
        public static Timer colorTimer;
        public static Timer messageDeleteTimer;

        public static SocketGuild crewGuild;
        static readonly Logging logging = new Logging();

        // Create the client
        public static DiscordSocketClient client;
        // Instantiate the configuration for the bot. This is where the token is stored.
        public BotConfig botConfig = new BotConfig();
        // Set Dictionary for cross-server channels
        public static ConcurrentDictionary<ulong, string> colorChoices = new ConcurrentDictionary<ulong, string>();
        //public static List<SocketRole> colorRoles = new List<SocketRole>();
        public static ConcurrentDictionary<string, string> triggerResponses = new ConcurrentDictionary<string, string>();
        //public static ConcurrentDictionary<ulong, LoggedMessage> messageCache = new ConcurrentDictionary<ulong, LoggedMessage>();
        public static ConcurrentDictionary<ulong, LoggedMessage> messageCache = new ConcurrentDictionary<ulong, LoggedMessage>();
        public static List<ulong> ignoreMessagesCache = new List<ulong>();

        // Entry point, immediately run everything async
        public static void Main(/* string[] args */)
        => new Program().MainAsync().GetAwaiter().GetResult();

        /// <summary>
        /// "Real" Main() so everything is async.
        /// </summary>
        /// <returns></returns>
        public async Task MainAsync()
        {
            // Instantiate the client, and add the logging
            client = new DiscordSocketClient
            (new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug
            });

            // Set the timer interval
            colorTimer = new Timer
            {
                Interval = 100000
            };

            // Set the Message Deletion timer to comply with Discord TOS
            // No content over 14 days may be cached - Check hourly for content exending past 14 days within the next hour
            messageDeleteTimer = new Timer
            {
                Interval = 3600000
            };

            // Populate the configuration from the BotConfig.json file for the client to use when connecting
            BotStartup.Startup(ref botConfig, ref colorChoices, ref triggerResponses, ref messageCache, ref ignoreMessagesCache);

            // Create event handlers and start the bot
            // discord.net handled event handlers
            //client.ChannelCreated += ChannelCreated;
            //client.ChannelDestroyed += ChannelDestroyed;
            //client.ChannelUpdated += ChannelUpdated;
            //client.Connected += Connected;
            //client.CurrentUserUpdated += CurrentUserUpdated;
            //client.Disconnected += Disconnected;
            //client.GuildAvailable += GuildAvailable;
            //client.GuildMembersDownloaded += GuildMembersDownloaded;
            //client.GuildMemberUpdated += GuildMemberUpdated;
            //client.GuildUnavailable += GuildUnavailable;
            //client.GuildUpdated += GuildUpdated;
            //client.JoinedGuild += JoinedGuild;
            //client.LatencyUpdated += LatencyUpdated;
            //client.LeftGuild += LeftGuild;
            client.Log += Log;
            //client.LoggedIn += LoggedIn;
            //client.LoggedOut += LoggedOut;
            client.MessageDeleted += MessageDeleted;
            client.MessageReceived += MessageReceived;
            //client.MessagesBulkDeleted += MessagesBulkDeleted;
            //client.MessageUpdated += MessageUpdated;
            //client.ReactionAdded += ReactionAdded;
            //client.ReactionRemoved += ReactionRemoved;
            //client.ReactionsCleared += ReactionsCleared;
            //client.Ready += Ready;
            //client.RecipientAdded += RecipientAdded;
            //client.RecipientRemoved += RecipientRemoved;
            //client.RoleCreated += RoleCreated;
            //client.RoleDeleted += RoleDeleted;
            //client.RoleUpdated += RoleUpdated;
            //client.UserBanned += UserBanned;
            //client.UserIsTyping += UserIsTyping;
            client.UserJoined += UserJoined;
            client.UserLeft += UserLeft;
            //client.UserUnbanned += UserUnbanned;
            //client.UserUpdated += UserUpdated;
            //client.UserVoiceStateUpdated += UserVoiceStateUpdated;
            //client.VoiceServerUpdated += VoiceServerUpdated;

            // Bot specific event handlers
            colorTimer.Elapsed += ColorChangeSelection;
            messageDeleteTimer.Elapsed += ClearMessageCache;
            // Set botConfig Constants
            //CREWGUILDID = botConfig.GuildID;
            //CREWBOTDMCHANNELID = botConfig.DMChannelID;
            //CREWBOTLOGCHANNELID = botConfig.LogChannelID;
            //CREWBOTUSERID = botConfig.BotID;
            //COLOR_ROLE_ID = botConfig.ColorRoleID;
            // Connect client
            string token = botConfig.Token;
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            FinalSetup(botConfig);

            colorTimer.Enabled = true;
            messageDeleteTimer.Enabled = true;

            // Wait for events to happen
            await Task.Delay(-1);
        }

        private async void ClearMessageCache(object sender, EventArgs e)
        {
            foreach (LoggedMessage message in messageCache.Values)
            {
                if (message.messageDateTimeOffset.AddDays(13).AddHours(23) < DateTime.Now)
                {
                    _ = messageCache.TryRemove(message.messageID, out _);
                    await Log(new LogMessage(LogSeverity.Verbose, $"Program", $"Message cleared from messageCache"));
                }
            }
        }

        private async Task UserJoined(SocketGuildUser user)
        {
            if (botConfig.GeneralChannelID != 0)
            {
                string msg = $"Welcome <@{user.Id}>! Go to <#454538448418766858> to pick a color and unlock channels!\nWe hope you enjoy it here!";
                await client.GetGuild(botConfig.GuildID).GetTextChannel(botConfig.GeneralChannelID).SendMessageAsync(msg);
            }
        }

        private async Task UserLeft(SocketGuildUser user)
        {
            if (botConfig.GeneralChannelID != 0)
            {
                string msg = $"{user.Username} just left.\nhttps://media.giphy.com/media/2ept7eRuyq98s/giphy.gif";
                await client.GetGuild(botConfig.GuildID).GetTextChannel(botConfig.GeneralChannelID).SendMessageAsync(msg);
            }
        }

        //private Task ReactionRemoved(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        //{
        //    throw new NotImplementedException();
        //}

        //private Task ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        //{
        //    throw new NotImplementedException();
        //}

        //private Task MessageUpdated(Cacheable<IMessage, ulong> arg1, SocketMessage arg2, ISocketMessageChannel arg3)
        //{
        //    throw new NotImplementedException();
        //}

        private async Task MessageDeleted(Cacheable<IMessage, ulong> deletedMessage, ISocketMessageChannel channel)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder()
            {
                ThumbnailUrl = $"{crewGuild.IconUrl}",
                Title = $"Message Deleted",
                Description = $"{channel.Name}\n**Message ID:** {deletedMessage.Id}"
            };
            if (messageCache.TryRemove(deletedMessage.Id, out LoggedMessage message))
            {
                embedBuilder.Description += $"\n**Content:** {message.messageContent}\n**Author:** {message.messageAuthorUsername}";
                embedBuilder.ThumbnailUrl = message.messageAvatarURL;
            }

            await DiscordLogMessage(embedBuilder);
        }

        private async Task DiscordLogMessage(EmbedBuilder builder)
        {
            await crewGuild.GetTextChannel(botConfig.LogChannelID).SendMessageAsync(string.Empty, false, builder.Build());
        }

        private static void FinalSetup(BotConfig botConfig)
        {
            System.Threading.Thread.Sleep(3000);
            UpdateCrewGuildObject(botConfig);
            //foreach (ulong id in colorChoices.Keys)
            //{
            //    colorRoles.Add(crewGuild.GetRole(id));
            //}
        }

        private static void UpdateCrewGuildObject(BotConfig bc)
        {
            _ = Log(new LogMessage(LogSeverity.Verbose, $"Program", $"UpdateCrewGuildObject"));
            crewGuild = client.GetGuild(bc.GuildID);
        }

        private async Task MessageReceived(SocketMessage message)
        {
            try
            {
                if (ignoreMessagesCache != null && !ignoreMessagesCache.Contains(message.Channel.Id))
                {
                    LoggedMessage messageToLog = new LoggedMessage(message);
                    if (messageCache.TryAdd(message.Id, messageToLog))
                    {
                        _ = Log(new LogMessage(LogSeverity.Verbose, $"Program", "MessageReceived: TryAdd:messageCache success"));
                        SerializeJsonObject("json/messageCache.json", messageCache);
                    }
                    else
                    {
                        _ = Log(new LogMessage(LogSeverity.Error, $"Program", "MessageReceived: TryAdd:messageCache failed"));
                    }
                }
            }
            catch (Exception e)
            {
                _ = Log(new LogMessage(LogSeverity.Error, $"Program", $"MessageReceived: TryAdd:messageCache Error: {e.Message}"));
            }

            _ = Log(new LogMessage(LogSeverity.Verbose, $"Program", $"MessageReceived"));
            if (message.Channel is IDMChannel && message.Author.MutualGuilds.Count > 0 && botConfig.DMChannelID > 0)
            {
                EmbedBuilder builder = new EmbedBuilder()
                {
                    ThumbnailUrl = $"{message.Author.GetAvatarUrl()}",
                    Title = $"From: {message.Author.Username}",
                    Description = $"{message.Content}",
                };
                await crewGuild.GetTextChannel(botConfig.DMChannelID).SendMessageAsync(String.Empty, false, builder.Build());
            }

            if (message.MentionedUsers.Count > 0)
            {
                foreach (SocketUser user in message.MentionedUsers)
                {
                    if (user.Id == botConfig.BotID)
                    {
                        EmbedBuilder builder = new EmbedBuilder()
                        {
                            ThumbnailUrl = $"{message.Author.GetAvatarUrl()}",
                            Title = $"From: {message.Author.Username} in {message.Channel.Name}",
                            Description = $"{message.Content}",
                        };
                        await crewGuild.GetTextChannel(botConfig.DMChannelID).SendMessageAsync($"{message.GetJumpUrl()}", false, builder.Build());
                        break;
                    }
                }
            }
            else if (message.MentionedRoles.Count > 0)
            {
                foreach (SocketRole role in message.MentionedRoles)
                {
                    if (role.Id == botConfig.AdminID)
                    {
                        EmbedBuilder builder = new EmbedBuilder()
                        {
                            ThumbnailUrl = $"{message.Author.GetAvatarUrl()}",
                            Title = $"From: {message.Author.Username} in {message.Channel.Name}",
                            Description = $"{message.Content}",
                        };
                        await crewGuild.GetTextChannel(botConfig.DMChannelID).SendMessageAsync($"{message.GetJumpUrl()}", false, builder.Build());
                        break;
                    }

                }
            }
            try
            {
                MessageCommandFactory.MessageCommand(message, botConfig, crewGuild, triggerResponses, colorChoices);
            }
            catch (System.NullReferenceException e)
            {
                await Log(new LogMessage(LogSeverity.Error, $"MessageCommand", $"{e.Message}"));
            }

            // Unit test for MessageCommandFactory
            // if message startswith +trigger
            // x is TriggerCommand


            if (message.Author.Id == crewGuild.OwnerId)
            {
                if (message.Content.StartsWith($"+setlogchannel"))
                {
                    botConfig.LogChannelID = message.Channel.Id;
                    SerializeJsonObject($"json/BotConfig.json", botConfig);
                }
                if (message.Content.StartsWith($"+removelogchannel"))
                {
                    botConfig.LogChannelID = 0;
                    SerializeJsonObject($"json/BotConfig.json", botConfig);
                }
                if (message.Content.StartsWith($"+setdmchannel"))
                {
                    botConfig.DMChannelID = message.Channel.Id;
                    SerializeJsonObject($"json/BotConfig.json", botConfig);
                }
                if (message.Content.StartsWith($"+removedmchannel"))
                {
                    botConfig.DMChannelID = 0;
                    SerializeJsonObject($"json/BotConfig.json", botConfig);
                }
            }


        }

        public void SerializeJsonObject(string filename, object value)
        {
            _ = Log(new LogMessage(LogSeverity.Verbose, $"Program", $"SerializeJson"));
            using (StreamWriter file = File.CreateText($"{filename}"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, value);
            }
        }

        //public async Task ColorChangeSelection(object sender, EventArgs e)
        async void ColorChangeSelection(object sender, EventArgs e)
        {
            try
            {
                if (botConfig.ColorRoleID > 0)
                {
                    List<SocketRole> colorRoles = new List<SocketRole>();
                    foreach (ulong id in colorChoices.Keys)
                    {
                        colorRoles.Add(crewGuild.GetRole(id));
                    }

                    _ = Log(new LogMessage(LogSeverity.Verbose, $"Program", $"ColorChangeSelection"));
                    UpdateCrewGuildObject(botConfig);
                    Random rand = new Random();
                    SocketGuildUser user = crewGuild.GetRole(botConfig.ColorRoleID).Members.ElementAt(rand.Next(crewGuild.GetRole(botConfig.ColorRoleID).Members.Count()));
                    await user.RemoveRolesAsync(colorRoles);
                    await user.AddRoleAsync(crewGuild.GetRole(colorChoices.ElementAt(rand.Next(colorChoices.Count)).Key));

                }
            }
            catch (Discord.Net.HttpException exception)
            {
                _ = Log(new LogMessage(LogSeverity.Error, $"Program", $"{exception.Message}"));
            }
            catch (System.Net.Http.HttpRequestException exception)
            {
                _ = Log(new LogMessage(LogSeverity.Error, $"Program", $"{exception.Message}"));
            }
            catch (System.OperationCanceledException exception)
            {
                _ = Log(new LogMessage(LogSeverity.Error, $"Program", $"{exception.Message}"));
            }
        }

        public static Task Log(LogMessage msg)
        {
            logging.Log(msg);
            return Task.CompletedTask;
        }
    }
}
