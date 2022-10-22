using TwitchBot.CommandLib.Attributes;
using TwitchBot.CommandLib.Models;
using TwitchBot.Models;
using TwitchBot.Services;

namespace TwitchBot.Commands;

[Group(Name = "anime")]
public class AnimeCommand : CommandModule
{
    private readonly FeedDbService _feedDbService;
    
    public AnimeCommand(FeedDbService feedDbService)
    {
        _feedDbService = feedDbService;
    }

    public override async Task Execute(CommandContext context)
    {
        if (context.Description is not TwitchCommandDescription description) return;

        var channel = description.Message.Channel;
        var message = description.Message;

        var userName = context.Arguments.Any() ? context.Arguments.First().ToLower() : message.Username;
        var foundUser = await _feedDbService.GetUser(userName);

        if (foundUser is null)
        {
            description.Client.SendReply(channel, message.Id, "Пользователя нет в базе");
            return;
        }

        var str = context.Arguments.Any() ? $"{userName}" : "Ты";
        var isAnimeStr = foundUser.IsAnime ? "анимешник D:" : "не анимешник"; 
        description.Client.SendReply(channel, message.Id, $"{str} - {isAnimeStr}");
    }

    [Command(Name = "list")]
    public async Task GetList(CommandContext context)
    {
        if (context.Description is not TwitchCommandDescription description) return;

        var channel = description.Message.Channel;
        var message = description.Message;
        
        var animes = (await _feedDbService.GetUsersAsync())
            .Where(u => u.IsAnime)
            .Aggregate("Список анимешников: ", (s, u) => s + u.Name + "; ");

        description.Client.SendReply(channel, message.Id, animes);
    }
    
    [Command(Name = "add")]
    public async Task Add(CommandContext context)
    {
        if (context.Description is not TwitchCommandDescription description) return;
        if (!context.Arguments.Any()) return;

        var channel = description.Message.Channel;
        var message = description.Message;

        var userName = context.Arguments.First().ToLower();
        var foundUser = await _feedDbService.GetUser(userName) ??
                        await _feedDbService.AddUser(userName);
        
        if (foundUser is null) return;

        if (foundUser.IsAnime)
        {
            description.Client.SendReply(channel, message.Id, "Пользователь уже анимешник");
            return;
        }
        foundUser.IsAnime = true;

        await _feedDbService.UpdateUser(foundUser.Id, foundUser);
        description.Client.SendReply(channel, message.Id, $"{userName} стал анимешником D:");
    }
    
    [Command(Name = "remove")]
    public async Task Remove(CommandContext context)
    {
        if (context.Description is not TwitchCommandDescription description) return;
        if (!context.Arguments.Any()) return;

        var channel = description.Message.Channel;
        var message = description.Message;

        var userName = context.Arguments.First().ToLower();
        var foundUser = await _feedDbService.GetUser(userName) ??
                        await _feedDbService.AddUser(userName);
        
        if (foundUser is null) return;

        if (!foundUser.IsAnime)
        {
            description.Client.SendReply(channel, message.Id, "Пользователь не анимешник");
            return;
        }
        foundUser.IsAnime = false;

        await _feedDbService.UpdateUser(foundUser.Id, foundUser);
        description.Client.SendReply(channel, message.Id, $"{userName} больше не анимешник lizardPls");
    }
}