using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;

namespace TwitchBot.Commands;

public interface ICommand
{
    Task Execute(ITwitchClient client, ChatCommand command, ChatMessage message);
}