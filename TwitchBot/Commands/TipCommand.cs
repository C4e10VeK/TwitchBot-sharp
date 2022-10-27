using TwitchBot.BlabLib;
using TwitchBot.BlabLib.Models;
using TwitchBot.CommandLib.Attributes;
using TwitchBot.CommandLib.Models;
using TwitchBot.Models;

namespace TwitchBot.Commands;

public class TipCommand : CommandModule
{
    [Command(Name = "tip")]
    public override async Task Execute(CommandContext ctx)
    {
        if (ctx.Description is not TwitchCommandDescription description) return;

        var channel = description.Message.Channel;
        var message = description.Message;

        description.Client.SendReply(channel, message.Id, "Генерирую совет coMMMMfy");
        var result = await Blab.GenerateAsync(BlabType.Wisdom, $"{description.Message.DisplayName} вот тебе совет:");
        
        description.Client.SendReply(channel, message.Id, result.ToString());
    }
}