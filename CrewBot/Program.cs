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
        static readonly ulong CREWGUILDID = 434740024316133376;
        static readonly ulong CREWBOTCHANNELID = 584465458242256944;
        static readonly ulong CREWBOTUSERID = 483772719985328130;
        static readonly ulong CREWADMINID = 543251167522586625;
        static readonly ulong COLOR_ROLE_ID = 584772355226861618;
        static SocketGuild crewGuild;

        // Create the client
        public static DiscordSocketClient client;
        // Instantiate the configuration for the bot. This is where the token is stored.
        BotConfig botConfig = new BotConfig();
        // Set Dictionary for cross-server channels
        //public static Dictionary<ulong, MazeRoom> MazeRooms = new Dictionary<ulong, MazeRoom>();
        public static Dictionary<ulong, string> colorChoices = new Dictionary<ulong, string>();
        public static List<SocketRole> colorRoles = new List<SocketRole>();
        //public static HashSet<SocketGuildUser> colorUsers = new HashSet<SocketGuildUser>();

        public OverwritePermissions see = new OverwritePermissions(viewChannel: PermValue.Allow);

        public OverwritePermissions hide = new OverwritePermissions();

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
                //LogLevel = LogSeverity.Verbose
                //LogLevel = LogSeverity.Info
                //LogLevel = LogSeverity.Warning
            });

            // Set the timer interval
            colorTimer = new Timer
            {
                Interval = 100000
            };

            // Populate the configuration from the BotConfig.json file for the client to use when connecting
            BotConfigurationAsync(ref botConfig);

            // Create event handlers and start the bot
            client.Log += Log;
            string token = botConfig.Token;

            // Use Message Received for all message handling
            client.MessageReceived += MessageReceived;
            //client.UserUpdated += UserUpdated;
            //client.GuildMemberUpdated += GuildMemberUpdated;
            //client.GuildMemberUpdated += MemberUpdate;
            //client.UserJoined += UserJoined;
            //_ = ColorChangeSelection();
            colorTimer.Elapsed += ColorChangeSelection;

            // Connect client
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            FinalSetup();

            colorTimer.Enabled = true;

            // Wait for events to happen
            await Task.Delay(-1);
        }

        //private Task Client_GuildMemberUpdated(SocketGuildUser arg1, SocketGuildUser arg2)
        //{
        //    throw new NotImplementedException();
        //}

        private static void FinalSetup()
        {
            System.Threading.Thread.Sleep(3000);
            //crewGuild = client.GetGuild(CREWGUILDID);
            UpdateCrewGuildObject();
            foreach (ulong id in colorChoices.Keys)
            {
                colorRoles.Add(crewGuild.GetRole(id));
            }

            //foreach (var user in crewGuild.Users)
            //{
            //    if (user.Roles.Contains(crewGuild.GetRole(COLOR_ROLE_ID)))
            //    {
            //        colorUsers.Add(user);
            //    }
            //}
        }

        private static void UpdateCrewGuildObject()
        {
            crewGuild = client.GetGuild(CREWGUILDID);
        }

        //private async Task GuildMemberUpdated(SocketUser beforeUser, SocketUser afterUser)
        //{
        //if(colorCount > 50)
        //{
        //    colorCount = 0;
        //    _ = ColorChangeSelection();
        //}

        //if(crewGuild.GetUser(beforeUser.Id).Roles.ToList().Contains(afterUser COLOR_ROLE_ID)
        //{

        //}
        //}

        private async Task MessageReceived(SocketMessage message)
        {
            if (message.Channel is IDMChannel && message.Author.MutualGuilds.Count > 0)
            {
                EmbedBuilder builder = new EmbedBuilder()
                {
                    ThumbnailUrl = $"{message.Author.GetAvatarUrl()}",
                    Title = $"From: {message.Author.Username}",
                    Description = $"{message.Content}",
                };
                await crewGuild.GetTextChannel(CREWBOTCHANNELID).SendMessageAsync(String.Empty, false, builder.Build());
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
                        await crewGuild.GetTextChannel(CREWBOTCHANNELID).SendMessageAsync($"{message.GetJumpUrl()}", false, builder.Build());
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
                        await crewGuild.GetTextChannel(CREWBOTCHANNELID).SendMessageAsync($"{message.GetJumpUrl()}", false, builder.Build());
                        break;
                    }

                }
            }

            if (message.Author.Id == crewGuild.OwnerId)
            {
                if (message.Content.StartsWith($"+addcolors"))
                {
                    foreach (var color in message.MentionedRoles)
                    {
                        colorChoices.TryAdd(color.Id, color.Name);
                        colorRoles.Add(color);
                    }
                }
                if (message.Content.StartsWith($"+removecolors"))
                {
                    foreach (var color in message.MentionedRoles)
                    {
                        colorChoices.Remove(color.Id);
                        colorRoles.Remove(color);
                    }
                }

                using (StreamWriter file = File.CreateText("json/ColorChoices.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(file, colorChoices);
                }
            }
        }

        //public async Task ColorChangeSelection(object sender, EventArgs e)
        async void ColorChangeSelection(object sender, EventArgs e)
        {
            UpdateCrewGuildObject();
            Random rand = new Random();
            SocketGuildUser user = crewGuild.GetRole(COLOR_ROLE_ID).Members.ElementAt(rand.Next(crewGuild.GetRole(COLOR_ROLE_ID).Members.Count()));
            await user.RemoveRolesAsync(colorRoles);
            await user.AddRoleAsync(crewGuild.GetRole(colorChoices.ElementAt(rand.Next(colorChoices.Count)).Key));

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

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
