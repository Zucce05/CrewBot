using Discord.WebSocket;
using System.Threading.Tasks;

namespace CrewBot.Interfaces
{
    public interface IMessageCommand
    {
        Task AdminAction(SocketMessage message);
        Task UserAction(SocketMessage message);

    }
}
