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

    [Command(Name = "top")]
    public async Task Top(CommandContext context)
    {
        if (context.Description is not TwitchCommandDescription description) return;

        var channel = description.Message.Channel;
        var message = description.Message;
        
        var animes = await _feedDbService.GetAnimes();

        var res = animes
            .OrderByDescending(a => a.Count)
            .ToList();

        var str = "Топ анимешников: ";

        for (var index = 0; index < res.Count; index++)
        {
            var r = res[index];
            str += $"{index + 1}:{r.User}({r.Count}); ";
        }

        description.Client.SendReply(channel, message.Id, str);
    }
    
    [Command(Name = "add")]
    public async Task Add(CommandContext context)
    {
        if (context.Description is not TwitchCommandDescription description) return;
        if (!context.Arguments.Any()) return;

        var channel = description.Message.Channel;
        var message = description.Message;

        var user = context.Arguments.First().ToLower();
        var foundUser = await _feedDbService.GetAnime(user);

        if (foundUser is null)
        {
            foundUser = await _feedDbService.AddAnime(user);
            if (foundUser is null) return;
            foundUser.Count++;

            await _feedDbService.UpdateAnime(foundUser.Id, foundUser);
            description.Client.SendReply(channel, message.Id, $"{user} стал анимешником");
            return;
        }
        
        foundUser.Count++;
        await _feedDbService.UpdateAnime(foundUser.Id, foundUser);
        description.Client.SendReply(channel, message.Id, $"{user} анимешник {foundUser.Count} раз(а)");
    }
}