using TwitchBot.CommandLib;
using TwitchBot.CommandLib.Attributes;
using TwitchBot.CommandLib.Models;
using TwitchBot.Extensions;
using TwitchBot.Models;
using TwitchBot.Services;

namespace TwitchBot.Commands;

public class AdminCommand : CommandModule
{
    private readonly FeedDbService _databaseService;
    
    public AdminCommand(FeedDbService databaseService)
    {
        _databaseService = databaseService;
    }

    [Command(Name = "ban")]
    public async Task Ban(CommandContext context)
    {
        if (context.Description is not TwitchCommandDescription description) return;
        
        if (!context.Arguments.Any()) return;
        
        var channel = description.Message.Channel;
        var message = description.Message;
        
        var foundUser = await _databaseService.GetUser(description.Message.Username) ??
                        await _databaseService.AddUser(description.Message.Username);

        if (foundUser is null || (foundUser.Permission > UserPermission.Admin))
        {
            description.Client.SendReply(channel, message.Id, "Требуются права админа");
            return;
        }

        var userToUpdate = await _databaseService.GetUser(context.Arguments.First().ToLower()) ?? 
                           await _databaseService.AddUser(context.Arguments.First().ToLower());
        if (userToUpdate is null) return;

        userToUpdate.IsBanned = true;

        await _databaseService.UpdateUser(userToUpdate.Id, userToUpdate);
        description.Client.SendReply(channel, message.Id, $"{context.Arguments.First()} был забанен!");
    }
    
    [Command(Name = "unban")]
    public async Task Unban(CommandContext context)
    {
        if (context.Description is not TwitchCommandDescription description) return;
        
        if (!context.Arguments.Any()) return;
        
        var channel = description.Message.Channel;
        var message = description.Message;
        
        var foundUser = await _databaseService.GetUser(description.Message.Username) ??
                        await _databaseService.AddUser(description.Message.Username);

        if (foundUser is null || (foundUser.Permission > UserPermission.Admin))
        {
            description.Client.SendReply(channel, message.Id, "Требуются права админа");
            return;
        }
        
        var userToUpdate = await _databaseService.GetUser(context.Arguments.First().ToLower()) ?? 
                           await _databaseService.AddUser(context.Arguments.First().ToLower());
        if (userToUpdate is null) return;

        userToUpdate.IsBanned = false;

        await _databaseService.UpdateUser(userToUpdate.Id, userToUpdate);
        description.Client.SendReply(channel, message.Id, $"{context.Arguments.First()} был разбанен!");
    }
    
    [Command(Name = "setperm")]
    public async Task SetPermission(CommandContext context)
    {
        if (context.Description is not TwitchCommandDescription description) return;
        
        if (!context.Arguments.Any() || context.Arguments.Count < 2) return;
        
        var channel = description.Message.Channel;
        var message = description.Message;
        
        var foundUser = await _databaseService.GetUser(description.Message.Username) ??
                        await _databaseService.AddUser(description.Message.Username);

        if (foundUser is null || (foundUser.Permission > UserPermission.Owner))
        {
            description.Client.SendReply(channel, message.Id, "Требуются права владельца");
            return;
        }
        
        var userToUpdate = await _databaseService.GetUser(context.Arguments.First().ToLower()) ?? 
                           await _databaseService.AddUser(context.Arguments.First().ToLower());
        if (userToUpdate is null) return;

        if (!int.TryParse(context.Arguments[1], out var result)) return;
        var permission = result switch
        {
            0 => UserPermission.Admin,
            1 => UserPermission.Moderator,
            _ => UserPermission.User
        };
        userToUpdate.Permission = permission; 

        await _databaseService.UpdateUser(userToUpdate.Id, userToUpdate);
        description.Client.SendReply(channel, message.Id,
            $"Права {context.Arguments.First()} изменены на {permission.ToString()}");
    }
}