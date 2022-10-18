using TwitchBot.CommandLib;
using TwitchBot.CommandLib.Attributes;
using TwitchBot.Extensions;
using TwitchBot.Models;
using TwitchBot.Services;

namespace TwitchBot.Commands;

public class AdminCommand : ICommandModule
{
    private readonly FeedDbService _databaseService;
    
    public AdminCommand(FeedDbService databaseService)
    {
        _databaseService = databaseService;
    }
    
    public Task Execute(CommandContext ctx)
    {
        return Task.CompletedTask;
    }
    
    [Command(Name = "ban")]
    public async Task Ban(CommandContext context)
    {
        if (context.Description is not CommandDescription description) return;
        
        if (!context.Arguments.Any()) return;
        var sender = await _databaseService.GetUserAsync(description.Message.Username);
        if (sender is null || !description.Message.IsModerator || sender.Permission > UserPermission.Admin)
        {
            description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
                "Требуются права администратора");
            return;
        }

        var userToUpdate = await _databaseService.GetUserAsync(context.Arguments.First().ToLower()) ??
                           await _databaseService.AddUser(context.Arguments.First().ToLower());

        if (userToUpdate is null) return;
        if (userToUpdate.Permission <= sender.Permission)
        {
            description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
                "Нельзя банить равного или выше тебя по рангу");
            return;
        }
        userToUpdate.IsBanned = true;

        await _databaseService.UpdateUserAsync(userToUpdate.Id, userToUpdate);

        description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
            $"{userToUpdate.Name} запрещено кормить!");
    }
    
    [Command(Name = "unban")]
    public async Task Unban(CommandContext context)
    {
        if (context.Description is not CommandDescription description) return;
        
        if (!context.Arguments.Any()) return;
        
        var sender = await _databaseService.GetUserAsync(description.Message.Username);
        if (sender is null || !description.Message.IsModerator || sender.Permission > UserPermission.Admin)
        {
            description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
                "Требуются права администратора");
            return;
        }

        var userToUpdate = await _databaseService.GetUserAsync(context.Arguments.First().ToLower()) ??
                           await _databaseService.AddUser(context.Arguments.First().ToLower());

        if (userToUpdate is null) return;
        if (userToUpdate.Permission <= sender.Permission)
        {
            description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
                "Нельзя разбанить равного или выше тебя по рангу");
            return;
        }
        userToUpdate.IsBanned = false;

        await _databaseService.UpdateUserAsync(userToUpdate.Id, userToUpdate);

        description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
            $"{userToUpdate.Name} разрешено кормить!");
    }
    
    [Command(Name = "setperm")]
    public async Task SetPermission(CommandContext context)
    {
        if (context.Description is not CommandDescription description) return ;
        if (!context.Arguments.Any()) return;
        var sender = await _databaseService.GetUserAsync(description.Message.Username);
        if (sender is null || sender.Permission > UserPermission.Owner)
        {
            description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
                "Требуются права владельца");
            return;
        }
        
        var userToUpdate = await _databaseService.GetUserAsync(context.Arguments.First().ToLower()) ??
                           await _databaseService.AddUser(context.Arguments.First().ToLower());
        
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
        
        await _databaseService.UpdateUserAsync(userToUpdate.Id, userToUpdate);
        description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
            $"Права {userToUpdate.Name} установлены на {permissionName}");
    }
    
    [Command(Name = "add")]
    public async Task Add(CommandContext context)
    {
        if (!context.Arguments.Any()) return;
        
        if (context.Description is not CommandDescription description) return ;
        var sender = await _databaseService.GetUserAsync(description.Message.Username);
        if (sender is null || !description.Message.IsModerator || sender.Permission > UserPermission.Moderator)
        {
            description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
                "Требуются права модератора");
            return;
        }
        
        var userToAdd = context.Arguments.First().ToLower();
        await _databaseService.AddUser(userToAdd);
        description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
            $"Пользователь {userToAdd} добавлен");
    }
}