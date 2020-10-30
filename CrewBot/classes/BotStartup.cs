using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CrewBot.Classes
{
    static class BotStartup
    {
        public static void Startup(
            ref BotConfig bc,
            ref ConcurrentDictionary<ulong, string> colors,
            ref ConcurrentDictionary<string, string> trigger,
            ref ConcurrentDictionary<ulong, LoggedMessage> message,
            ref List<ulong> ignoreMessages)
        {
            BotConfig(ref bc);
            ColorRoles(ref colors);
            TriggerValues(ref trigger);
            MessageCache(ref message);
            IgnoreMessageCache(ref ignoreMessages);
        }

        private static void BotConfig(ref BotConfig bc)
        {
            try
            {
                JsonTextReader reader;
                // This is good for deployment where I've got the config with the executable
                reader = new JsonTextReader(new StreamReader("json/BotConfig.json"));
                bc = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText("json/BotConfig.json"));
                reader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"BotStartup->BotConfig: Executable Level SetUp Exception:\n\t{e.Message}");
            }
        }

        private static void ColorRoles(ref ConcurrentDictionary<ulong, string> colors)
        {
            try
            {
                JsonTextReader reader;
                // This is good for deployment where I've got the config with the executable
                reader = new JsonTextReader(new StreamReader("json/ColorChoices.json"));
                colors = JsonConvert.DeserializeObject<ConcurrentDictionary<ulong, string>>(File.ReadAllText("json/ColorChoices.json"));
                reader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"BotStartup->ColorChoices: Executable Level SetUp Exception:\n\t{e.Message}");
            }
        }

        private static void TriggerValues(ref ConcurrentDictionary<string, string> trigger)
        {
            try
            {
                JsonTextReader reader;
                // This is good for deployment where I've got the config with the executable
                reader = new JsonTextReader(new StreamReader("json/triggerResponses.json"));
                trigger = JsonConvert.DeserializeObject<ConcurrentDictionary<string, string>>(File.ReadAllText("json/triggerResponses.json"));
                reader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"BotConfig->triggerResponses: Executable Level SetUp Exception:\n\t{e.Message}");
            }
        }

        private static void MessageCache(ref ConcurrentDictionary<ulong, LoggedMessage> message)
        {
            try
            {
                JsonTextReader reader;
                // This is good for deployment where I've got the config with the executable
                reader = new JsonTextReader(new StreamReader("json/messageCache.json"));
                message = JsonConvert.DeserializeObject<ConcurrentDictionary<ulong, LoggedMessage>>(File.ReadAllText("json/messageCache.json"));
                reader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"BotConfig->messageCache: Executable Level SetUp Exception:\n\t{e.Message}");
            }
        }

        private static void IgnoreMessageCache(ref List<ulong> ignoreMessages)
        {
            try
            {
                JsonTextReader reader;
                // This is good for deployment where I've got the config with the executable
                reader = new JsonTextReader(new StreamReader("json/ignoreMessageCache.json"));
                ignoreMessages = JsonConvert.DeserializeObject<List<ulong>>(File.ReadAllText("json/ignoreMessageCache.json"));
                reader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"BotConfig->ignoreMessageCache: Executable Level SetUp Exception:\n\t{e.Message}");
            }
        }

    }
}
