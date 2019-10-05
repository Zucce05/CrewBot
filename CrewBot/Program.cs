using CrewBot.classes;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Timers;

namespace CrewBot
{
    class Program
    {
        // Timer for the color role
        public static Timer colorTimer;

        // STATIC VALUES FOR SERVER AND CHANNEL
        static ulong CREWGUILDID; // = 434740024316133376;
        static ulong CREWBOTDMCHANNELID; // = 584465458242256944;
        static ulong CREWBOTLOGCHANNELID; // = 584465458242256944;
        static ulong CREWBOTUSERID; // = 483772719985328130;
        static ulong CREWADMINID; // = 543251167522586625;
        static ulong COLOR_ROLE_ID; // = 584772355226861618;
        static SocketGuild crewGuild;
        static readonly Logging logging = new Logging();

        // Create the client
        public static DiscordSocketClient client;
        // Instantiate the configuration for the bot. This is where the token is stored.
        public BotConfig botConfig = new BotConfig();
        // Set Dictionary for cross-server channels
        public static Dictionary<ulong, string> colorChoices = new Dictionary<ulong, string>();
        public static List<SocketRole> colorRoles = new List<SocketRole>();

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
            //client.MessageDeleted += MessageDeleted;
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
            CREWGUILDID = botConfig.GuildID;
            CREWBOTDMCHANNELID = botConfig.DMChannelID;
            CREWBOTLOGCHANNELID = botConfig.LogChannelID;
            CREWBOTUSERID = botConfig.BotID;
            COLOR_ROLE_ID = botConfig.ColorRoleID;
            // Connect client
            string token = botConfig.Token;
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            FinalSetup();

            colorTimer.Enabled = true;

            // Wait for events to happen
            await Task.Delay(-1);
        }

        private static void FinalSetup()
        {
            System.Threading.Thread.Sleep(3000);
            UpdateCrewGuildObject();
            foreach (ulong id in colorChoices.Keys)
            {
                colorRoles.Add(crewGuild.GetRole(id));
            }
        }

        private static void UpdateCrewGuildObject()
        {
            _ = Log(new LogMessage(LogSeverity.Verbose, $"Program", $"UpdateCrewGuildObject"));
            crewGuild = client.GetGuild(CREWGUILDID);
        }

        private async Task MessageReceived(SocketMessage message)
        {
            _ = Log(new LogMessage(LogSeverity.Verbose, $"Program", $"MessageReceived"));
            if (message.Channel is IDMChannel && message.Author.MutualGuilds.Count > 0 && CREWBOTDMCHANNELID > 0)
            {
                EmbedBuilder builder = new EmbedBuilder()
                {
                    ThumbnailUrl = $"{message.Author.GetAvatarUrl()}",
                    Title = $"From: {message.Author.Username}",
                    Description = $"{message.Content}",
                };
                await crewGuild.GetTextChannel(CREWBOTDMCHANNELID).SendMessageAsync(String.Empty, false, builder.Build());
            }

            if (message.MentionedUsers.Count > 0)
            {
                foreach (SocketUser user in message.MentionedUsers)
                {
                    if (user.Id == CREWBOTUSERID)
                    {
                        EmbedBuilder builder = new EmbedBuilder()
                        {
                            ThumbnailUrl = $"{message.Author.GetAvatarUrl()}",
                            Title = $"From: {message.Author.Username} in {message.Channel.Name}",
                            Description = $"{message.Content}",
                        };
                        await crewGuild.GetTextChannel(CREWBOTDMCHANNELID).SendMessageAsync($"{message.GetJumpUrl()}", false, builder.Build());
                        break;
                    }
                }
            }
            else if (message.MentionedRoles.Count > 0)
            {
                foreach (SocketRole role in message.MentionedRoles)
                {
                    if (role.Id == CREWADMINID)
                    {
                        EmbedBuilder builder = new EmbedBuilder()
                        {
                            ThumbnailUrl = $"{message.Author.GetAvatarUrl()}",
                            Title = $"From: {message.Author.Username} in {message.Channel.Name}",
                            Description = $"{message.Content}",
                        };
                        await crewGuild.GetTextChannel(CREWBOTDMCHANNELID).SendMessageAsync($"{message.GetJumpUrl()}", false, builder.Build());
                        break;
                    }

                }
            }

            if (message.Author.Id == crewGuild.OwnerId)
            {
                if (message.Content.StartsWith($"+addcolors"))
                {
                    _ = Log(new LogMessage(LogSeverity.Verbose, $"Program", $"MessageRecieved :: +addcolors"));
                    foreach (var color in message.MentionedRoles)
                    {
                        colorChoices.TryAdd(color.Id, color.Name);
                        colorRoles.Add(color);
                    }
                    SerializeJsonObject($"json/ColorChoices.json", colorChoices);
                }
                if (message.Content.StartsWith($"+removecolors"))
                {
                    _ = Log(new LogMessage(LogSeverity.Verbose, $"Program", $"MessageRecieved :: +removecolors"));
                    foreach (var color in message.MentionedRoles)
                    {
                        colorChoices.Remove(color.Id);
                        colorRoles.Remove(color);
                    }
                    SerializeJsonObject($"json/ColorChoices.json", colorChoices);
                }
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
                if (message.Content.StartsWith($"+setcolorchange"))
                {
                    if (message.MentionedRoles.Count == 1)
                    {
                        botConfig.ColorRoleID = message.MentionedRoles.FirstOrDefault().Id;
                        SerializeJsonObject($"json/BotConfig.json", botConfig);
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"You must mention exactly one role for this command\nExample: ``+SetColorChange @MyColorChangeRole``");
                    }
                }
                if (message.Content.StartsWith($"+removecolorchange"))
                {
                    botConfig.ColorRoleID = 0;
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
                if (COLOR_ROLE_ID > 0)
                {
                    _ = Log(new LogMessage(LogSeverity.Verbose, $"Program", $"ColorChangeSelection"));
                    UpdateCrewGuildObject();
                    Random rand = new Random();
                    SocketGuildUser user = crewGuild.GetRole(COLOR_ROLE_ID).Members.ElementAt(rand.Next(crewGuild.GetRole(COLOR_ROLE_ID).Members.Count()));
                    await user.RemoveRolesAsync(colorRoles);
                    await user.AddRoleAsync(crewGuild.GetRole(colorChoices.ElementAt(rand.Next(colorChoices.Count)).Key));

                }
            }
            catch (Discord.Net.HttpException exception)
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
                colorChoices = JsonConvert.DeserializeObject<Dictionary<ulong, string>>(File.ReadAllText("json/ColorChoices.json"));
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
