using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;

namespace TwitchBot.Extensions;

public static class TwitchClientExtension
{
    public static void SendMention(this ITwitchClient client, string channel, string user, string message) =>
        client.SendMessage(channel, $"@{user}, {message}");

    public static void SendMention(this ITwitchClient client, JoinedChannel channel, string user, string message) =>
        client.SendMessage(channel, $"@{user}, {message}");
}