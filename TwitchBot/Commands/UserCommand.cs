using TwitchBot.CommandLib.Attributes;
using TwitchBot.CommandLib.Models;
using TwitchBot.Extensions;
using TwitchBot.Models;
using TwitchBot.Services;

namespace TwitchBot.Commands;

public class UserCommand : CommandModule
{
    private readonly FeedDbService _databaseService;

    public UserCommand(FeedDbService databaseService)
    {
        _databaseService = databaseService;
    }

    [Command(Name = "help")]
    public Task GetHelp(CommandContext context)
    {
        if (context.Description is not TwitchCommandDescription description) return Task.CompletedTask;
        
        description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
            "https://github.com/C4e10VeK/TwitchBot-sharp/blob/master/Help/Help.md");
        return Task.CompletedTask;
    }

    [Command(Name = "status")]
    public async Task GetStatus(CommandContext context)
    {
        if (context.Description is not TwitchCommandDescription description) return;

        var channel = description.Message.Channel;
        var message = description.Message;
        
        var userName = context.Arguments.Any() ? context.Arguments.First().ToLower() : message.Username;
        var startStr = context.Arguments.Any() ? $"Статус {context.Arguments.First()}" : "Твой статус";
        
        var foundUser = await _databaseService.GetUser(userName);
        
        if (foundUser is null)
        {
            description.Client.SendReply(channel, message.Id, "Пользователя нет в базе");
            return;
        }

        description.Client.SendReply(channel, message.Id,
            $"{startStr}: бан {(foundUser.IsBanned ? "есть" : "нет")}, права - {foundUser.Permission.ToString()}");
    }
}