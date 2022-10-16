using TwitchLib.Client.Interfaces;

namespace TwitchBot.Extensions;

public static class TwitchClientExtension
{
    public static void SendMention(this ITwitchClient client, string channel, string user, string message)
    {
        client.SendMessage(channel, $"@{user}, {message}");
    }
}