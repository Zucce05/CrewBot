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
        public static ConcurrentDictionary<ulong, string> messageCache = new ConcurrentDictionary<ulong, string>();

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

            // Populate the configuration from the BotConfig.json file for the client to use when connecting
            BotConfigurationAsync(ref botConfig);

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
            //client.UserJoined += UserJoined;
            //client.UserLeft += UserLeft;
            //client.UserUnbanned += UserUnbanned;
            //client.UserUpdated += UserUpdated;
            //client.UserVoiceStateUpdated += UserVoiceStateUpdated;
            //client.VoiceServerUpdated += VoiceServerUpdated;

            // Bot specific event handlers
            colorTimer.Elapsed += ColorChangeSelection;
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

            // Wait for events to happen
            await Task.Delay(-1);
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
                Description = $"{channel.Name}\nMessage ID:{deletedMessage.Id}"
            };
            if (deletedMessage.HasValue)
            {
                embedBuilder.Description += $"\nContent: {deletedMessage.Value.Content}";
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

            MessageCommandFactory.MessageCommand(message, botConfig, crewGuild, triggerResponses, colorChoices);

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

        public static void BotConfigurationAsync(ref BotConfig bc)
        {
            JsonTextReader reader;
            try
            {
                // This is good for deployment where I've got the config with the executable
                reader = new JsonTextReader(new StreamReader("json/BotConfig.json"));
                bc = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText("json/BotConfig.json"));
            }
            catch (Exception e)
            {
                Console.WriteLine($"BotConfig: Executable Level SetUp Exception:\n\t{e.Message}");
            }

            try
            {
                // This is good for deployment where I've got the config with the executable
                reader = new JsonTextReader(new StreamReader("json/ColorChoices.json"));
                colorChoices = JsonConvert.DeserializeObject<ConcurrentDictionary<ulong, string>>(File.ReadAllText("json/ColorChoices.json"));
            }
            catch (Exception e)
            {
                Console.WriteLine($"BotConfig: Executable Level SetUp Exception:\n\t{e.Message}");
            }

            try
            {
                // This is good for deployment where I've got the config with the executable
                reader = new JsonTextReader(new StreamReader("json/triggerResponses.json"));
                triggerResponses = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(File.ReadAllText("json/triggerResponses.json"));
            }
            catch (Exception e)
            {
                Console.WriteLine($"BotConfig: Executable Level SetUp Exception:\n\t{e.Message}");
            }

            try
            {
                // This is good for deployment where I've got the config with the executable
                reader = new JsonTextReader(new StreamReader("json/messageCache.json"));
                messageCache = JsonConvert.DeserializeObject<ConcurrentDictionary<ulong, string>>(File.ReadAllText("json/triggerResponses.json"));
            }
            catch (Exception e)
            {
                Console.WriteLine($"BotConfig: Executable Level SetUp Exception:\n\t{e.Message}");
            }
        }

        public static Task Log(LogMessage msg)
        {
            logging.Log(msg);
            return Task.CompletedTask;
        }
    }
}
