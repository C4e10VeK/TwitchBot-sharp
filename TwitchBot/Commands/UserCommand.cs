using TwitchBot.CommandLib;
using TwitchBot.CommandLib.Attributes;
using TwitchBot.Extensions;
using TwitchBot.Models;
using TwitchBot.Services;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;

namespace TwitchBot.Commands;

public class UserCommand : ICommandModule
{
    private readonly FeedDbService _feedDbService;

    public UserCommand(FeedDbService feedDbService)
    {
        _feedDbService = feedDbService;
    }

    [Command(Name = "help")]
    public Task Execute(CommandContext context)
    {
        if (context.Description is not CommandDescription description) return Task.CompletedTask;
        
        description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
            "https://github.com/C4e10VeK/TwitchBot-sharp/blob/master/Help/Help.md");
        return Task.CompletedTask;
    }

    [Command(Name = "add")]
    public async Task Add(CommandContext context)
    {
        if (!context.Arguments.Any()) return;
        
        if (context.Description is not CommandDescription description) return ;
        var sender = await _feedDbService.GetUserAsync(description.Message.Username);
        if (sender is null || !description.Message.IsModerator || sender.Permission > UserPermission.Moderator)
        {
            description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
                "Требуются права модератора");
            return;
        }
        
        var userToAdd = context.Arguments.First().ToLower();
        await _feedDbService.AddUser(userToAdd);
        description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
            $"Пользователь {userToAdd} добавлен");
    }
    
    [Command(Name = "setperm")]
    public async Task SetPermission(CommandContext context)
    {
        if (context.Description is not CommandDescription description) return ;
        if (!context.Arguments.Any()) return;
        var sender = await _feedDbService.GetUserAsync(description.Message.Username);
        if (sender is null || sender.Permission > UserPermission.Owner)
        {
            description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
                "Требуются права владельца");
            return;
        }
        
        var userToUpdate = await _feedDbService.GetUserAsync(context.Arguments.First().ToLower()) ??
                           await _feedDbService.AddUser(context.Arguments.First().ToLower());
        
        if (!int.TryParse(context.Arguments[1], out var result)) return;
        if (userToUpdate is null) return;
        userToUpdate.Permission = result switch
        {
            0 => UserPermission.Admin,
            1 => UserPermission.Moderator,
            2 or _ => UserPermission.User
        };

        var permissionName = result switch
        {
            0 => UserPermission.Admin.ToString(),
            1 => UserPermission.Moderator.ToString(),
            2 or _ => UserPermission.User.ToString()
        };
        
        await _feedDbService.UpdateUserAsync(userToUpdate.Id, userToUpdate);
        description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
            $"Права {userToUpdate.Name} установлены на {permissionName}");
    }

    [Command(Name = "status")]
    public async Task GetStatus(CommandContext context)
    {
        if (context.Description is not CommandDescription description) return;

        var user = await _feedDbService.GetUserAsync(description.Message.Username);
        if (user is null) return;

        if (context.Arguments.Any())
        {
            user = await _feedDbService.GetUserAsync(context.Arguments.First().ToLower());
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

    private async Task Ban(ITwitchClient client, ChatCommand command, ChatMessage message)
    {
        if (!command.ArgumentsAsList.Any()) return;
        var sender = await _feedDbService.GetUserAsync(message.Username);
        if (sender is null || !message.IsModerator || sender.Permission > UserPermission.Admin)
        {
            client.SendMention(message.Channel, message.DisplayName, "Требуются права администратора");
            return;
        }

        var userToUpdate = await _feedDbService.GetUserAsync(command.ArgumentsAsList[1].ToLower()) ??
                           await _feedDbService.AddUser(command.ArgumentsAsList[1].ToLower());

        if (userToUpdate is null) return;
        if (userToUpdate.Permission <= sender.Permission)
        {
            client.SendMention(message.Channel, message.DisplayName,
                "Нельзя банить равного или выше тебя по рангу");
            return;
        }
        userToUpdate.IsBanned = true;

        await _feedDbService.UpdateUserAsync(userToUpdate.Id, userToUpdate);
        
        client.SendMention(message.Channel, message.DisplayName, $"{userToUpdate.Name} запрещено кормить!");
    }
    
    private async Task Unban(ITwitchClient client, ChatCommand command, ChatMessage message)
    {
        if (!command.ArgumentsAsList.Any()) return;
        var sender = await _feedDbService.GetUserAsync(message.Username);
        if (sender is null || !message.IsModerator || sender.Permission > UserPermission.Admin)
        {
            client.SendMention(message.Channel, message.DisplayName, "Требуются права администратора");
            return;
        }

        var userToUpdate = await _feedDbService.GetUserAsync(command.ArgumentsAsList[1].ToLower()) ??
                           await _feedDbService.AddUser(command.ArgumentsAsList[1].ToLower());

        if (userToUpdate is null) return;
        if (userToUpdate.Permission <= sender.Permission)
        {
            client.SendMention(message.Channel, message.DisplayName,
                "Нельзя разбанить равного или выше тебя по рангу");
            return;
        }
        userToUpdate.IsBanned = false;

        await _feedDbService.UpdateUserAsync(userToUpdate.Id, userToUpdate);
        
        client.SendMention(message.Channel, message.DisplayName, $"{userToUpdate.Name} разрешено кормить!");
    }
}