using TwitchBot.CommandLib.Models;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;

namespace TwitchBot.Models;

public class CommandDescription : ICommandDescription
{
    public ITwitchClient Client { get; set; }
    public ChatMessage Message { get; set; }

    public object Sender => Client;
    public object Detail => Message;
}