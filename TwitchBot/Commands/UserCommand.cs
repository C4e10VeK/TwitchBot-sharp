using TwitchBot.Extensions;
using TwitchBot.Models;
using TwitchBot.Services;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;

namespace TwitchBot.Commands;

public class UserUpdateCommand : ICommand
{
    private readonly FeedDbService _feedDbService;
    private readonly Dictionary<string, Func<ITwitchClient, ChatCommand, ChatMessage, Task>> _subCommands;

    public UserUpdateCommand(FeedDbService feedDbService)
    {
        _feedDbService = feedDbService;
        _subCommands = new();
        _subCommands.Add("ban", Ban);
        _subCommands.Add("unban", Unban);
        _subCommands.Add("permission", SetPermission);
        _subCommands.Add("add", Add);
        _subCommands.Add("help", GetHelp);
    }

    private Task GetHelp(ITwitchClient client, ChatCommand command, ChatMessage message)
    {
        client.SendMention(message.Channel, message.DisplayName,
            "https://github.com/C4e10VeK/TwitchBot-sharp/blob/master/Help/Help.md");
        return Task.CompletedTask;
    }

    public async Task Execute(ITwitchClient client, ChatCommand command, ChatMessage message)
    {
        if (!command.ArgumentsAsList.Any()) return;

        if (_subCommands.TryGetValue(command.ArgumentsAsList[0], out var cmd))
            await cmd.Invoke(client, command, message);
    }

    private async Task Add(ITwitchClient client, ChatCommand command, ChatMessage message)
    {
        var sender = await _feedDbService.GetUserAsync(message.Username);
        if (sender is null || sender.Permission > UserPermission.Moderator
                           || message.IsModerator || message.IsBroadcaster)
        {
            client.SendMention(message.Channel, message.DisplayName, "Требуются права модератора");
            return;
        }
        
        if (command.ArgumentsAsList.Count < 2) return;
        var userToAdd = command.ArgumentsAsList[1].ToLower();
        await _feedDbService.AddUser(userToAdd);
        client.SendMention(message.Channel, message.DisplayName, $"Пользователь {userToAdd} добавлен");
        
    }
    
    private async Task SetPermission(ITwitchClient client, ChatCommand command, ChatMessage message)
    {
        if (command.ArgumentsAsList.Count < 3) return;
        if (!command.ArgumentsAsList.Any()) return;
        var sender = await _feedDbService.GetUserAsync(message.Username);
        if (sender is null || sender.Permission > UserPermission.Owner || message.IsBroadcaster)
        {
            client.SendMention(message.Channel, message.DisplayName, "Требуются права владельца");
            return;
        }
        
        var userToUpdate = await _feedDbService.GetUserAsync(command.ArgumentsAsList[1].ToLower()) ??
                           await _feedDbService.AddUser(command.ArgumentsAsList[1].ToLower());
        
        if (!int.TryParse(command.ArgumentsAsList[2], out var result)) return;
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
        client.SendMention(message.Channel, message.DisplayName,
            $"Права {userToUpdate.Name} установлены на {permissionName}");
    }

    private async Task Ban(ITwitchClient client, ChatCommand command, ChatMessage message)
    {
        if (!command.ArgumentsAsList.Any()) return;
        var sender = await _feedDbService.GetUserAsync(message.Username);
        if (sender is null || sender.Permission > UserPermission.Admin
                           || message.IsModerator || message.IsBroadcaster)
        {
            client.SendMention(message.Channel, message.DisplayName, "Требуются права администратора");
            return;
        }

        var userToUpdate = await _feedDbService.GetUserAsync(command.ArgumentsAsList[1].ToLower()) ??
                           await _feedDbService.AddUser(command.ArgumentsAsList[1].ToLower());

        if (userToUpdate is null) return;
        if (userToUpdate.Permission >= sender.Permission)
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
        if (sender is null || sender.Permission > UserPermission.Admin
                           || message.IsModerator || message.IsBroadcaster)
        {
            client.SendMention(message.Channel, message.DisplayName, "Требуются права администратора");
            return;
        }

        var userToUpdate = await _feedDbService.GetUserAsync(command.ArgumentsAsList[1].ToLower()) ??
                           await _feedDbService.AddUser(command.ArgumentsAsList[1].ToLower());

        if (userToUpdate is null) return;
        if (userToUpdate.Permission >= sender.Permission)
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