using TwitchBot.Extensions;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;

namespace TwitchBot.Commands;

public class HelpCommand : ICommand
{
    public Task Execute(ITwitchClient client, ChatCommand command, ChatMessage message)
    {
        client.SendMention(message.Channel, message.DisplayName,
            "https://github.com/C4e10VeK/TwitchBot-sharp/blob/master/Help/Help.md");
        return Task.CompletedTask;
    }
}