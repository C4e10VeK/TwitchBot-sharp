using System.Diagnostics.CodeAnalysis;
using TwitchBot.CommandLib;
using TwitchBot.CommandLib.Attributes;
using TwitchBot.Extensions;
using TwitchBot.Models;
using TwitchBot.Services;

namespace TwitchBot.Commands;

public class UserCommand : ICommandModule
{
    private readonly FeedDbService _databaseService;

    public UserCommand(FeedDbService databaseService)
    {
        _databaseService = databaseService;
    }

    [Command(Name = "help")]
    public Task Execute(CommandContext context)
    {
        if (context.Description is not CommandDescription description) return Task.CompletedTask;
        
        description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
            "https://github.com/C4e10VeK/TwitchBot-sharp/blob/master/Help/Help.md");
        return Task.CompletedTask;
    }

    [Command(Name = "status")]
    public async Task GetStatus(CommandContext context)
    {
        if (context.Description is not CommandDescription description) return;

        var user = await _databaseService.GetUserAsync(description.Message.Username);
        if (user is null) return;

        if (context.Arguments.Any())
        {
            user = await _databaseService.GetUserAsync(context.Arguments.First().ToLower());
            if (user is null)
            {
                description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
                    "Пользователя нет в базе");
                return;
            }
        }

        var startStr = context.Arguments.Any() ? $"Статус {user.Name}:" : "Твой статус:";

        description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
            $"{startStr} бан - {(user.IsBanned ? "Да" : "нет")}, права - {user.Permission.ToString()}");
    }
}