using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace CrewBot.Classes.Abstract
{
    public abstract class MessageCommandBase
    {
        async Task AdminAction(SocketMessage message)
        {
            await message.Channel.SendMessageAsync("This command is not created yet.");
        }
        async Task UserAction(SocketMessage message)
        {
            await message.Channel.SendMessageAsync("This command is not created yet.");
        }
        
        private string HelpMessage(string prefix)
        {
            string returnString = $"There is no help message created here yet. Let Zucce know.";
            return returnString;
        }

        public void SerializeJsonObject(string filename, object value)
        {
            _ = Program.Log(new LogMessage(LogSeverity.Verbose, $"Program", $"SerializeJson"));
            using (StreamWriter file = File.CreateText($"{filename}"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, value);
            }
        }
    }
}
