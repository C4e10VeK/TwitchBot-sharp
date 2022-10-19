using System.Globalization;
using TwitchBot.Extensions;
using TwitchBot.Models;
using TwitchBot.Services;
using TwitchBot.CommandLib.Attributes;
using TwitchBot.CommandLib.Models;

namespace TwitchBot.Commands;

[Group(Name = "feed")]
public class FeedCommand : CommandModule
{
    private readonly FeedDbService _feedDbService;
    private List<string?> _availableSmiles;
    private readonly Random _random;

    public FeedCommand() { }
    
    public FeedCommand(FeedDbService feedDbService)
    {
        _feedDbService = feedDbService;
        _availableSmiles = _feedDbService.GetAvailableSmiles().Result;
        _random = new Random(445662);
    }

    [Command(Name = "top")]
    public async Task GetTop(CommandContext context)
    {
        if (context.Description is not TwitchCommandDescription description) return;

        var smiles = (await _feedDbService.GetSmiles())
            .Where(s => s.Size > 0)
            .ToList();

        var top = "Топ смайлов peepoFAT : ";
        for (var index = 0; index < smiles.Count; index++)
        {
            var smile = smiles[index];
            top += index switch
            {
                0 => "👑",
                1 => "🥈",
                2 => "🥉",
                _ => ""
            };
            top += $"{index + 1}: {smile.Name} , размер = {smile.Size:n3} см; ";
        }
        
        var channel = description.Message.Channel;
        var msgId = description.Message.Id;
        description.Client.SendReply(channel, msgId, top);
    }

    public override async Task Execute(CommandContext context)
    {
        if (context.Description is not TwitchCommandDescription description) return;

        var user = await _feedDbService.GetUser(description.Message.Username) ??
                   await _feedDbService.AddUser(description.Message.Username);
        if (user is null) return;

        var channel = description.Message.Channel;
        var msgId = description.Message.Id;
        
        if (!context.Arguments.Any())
        {
            description.Client.SendReply(channel, msgId,
                $"Можно кормить {string.Join(" ", _availableSmiles)}");
            return;
        }

        var smileName = context.Arguments.First();
        var smile = await _feedDbService.GetSmile(smileName);
        if (smile is null)
        {
            description.Client.SendReply(channel, msgId, $"Можно кормить {string.Join(" ", _availableSmiles)}");
            return;
        }

        if (user.TimeToFeed > DateTime.UtcNow)
        {
            var timeToEnd = user.TimeToFeed - DateTime.UtcNow;
            description.Client.SendReply(channel, msgId,
                $"До следующей кормежки {timeToEnd.TotalHours:00}:{timeToEnd:mm\\:ss} peepoFAT . Жди");
            
            return;
        }

        smile.Size += _random.NextDouble(0.5, 0.005);
        user.TimeToFeed = DateTime.UtcNow + TimeSpan.FromMinutes(5);
        user.FeedCount++;

        description.Client.SendReply(channel, msgId, $"Ты покормил {smileName} , размер = {smile.Size:n3} см.");

        if (!user.FeedSmiles.ContainsKey(smileName))
            user.FeedSmiles.Add(smile.Name, smile.Id);
        if (!smile.Users.Contains(user.Name))
            smile.Users.Add(user.Name);

        await _feedDbService.UpdateSmile(smile.Id, smile);
        await _feedDbService.UpdateUser(user.Id, user);
    }

    [Command(Name = "status")]
    public async Task GetStatus(CommandContext context)
    {
        if (context.Description is not TwitchCommandDescription description) return;

        var channel = description.Message.Channel;
        var message = description.Message;

        var userName = context.Arguments.Any() ? context.Arguments.First().ToLower() : message.Username;
        var startStr = context.Arguments.Any() ? $"{context.Arguments.First()} покормил(а)" : "Ты покормил(а)";
            
        var foundUser = await _feedDbService.GetUser(userName);
        
        if (foundUser is null)
        {
            description.Client.SendReply(channel, message.Id, "Пользователь еще никого не кормил");
            return;
        }

        var smiles = (await _feedDbService.GetSmiles(foundUser.Name)).Select(s => s.Name).ToList();

        description.Client.SendReply(channel, message.Id,
            $"{startStr} {foundUser.FeedCount} раз(а), покормленные тобой смайлы - {string.Join(" , ", smiles)}");

    }

    [Command(Name = "add")]
    public async Task Add(CommandContext context)
    {
        if (context.Description is not TwitchCommandDescription description) return;

        var channel = description.Message.Channel;
        var message = description.Message;

        var foundUser = await _feedDbService.GetUser(description.Message.Username);

        if (foundUser is null) return;
        if (foundUser.Permission > UserPermission.Moderator)
            if (!message.IsModerator)
            {
                description.Client.SendReply(message.Channel, message.Id, "Требуются права модератора");
                return;
            }
        
        if (!context.Arguments.Any())
        {
            description.Client.SendReply(channel, message.Id, "Нужно указать смайл");
            return;
        }

        var smileName = context.Arguments.First();
        var smile = await _feedDbService.AddSmile(smileName);
        if (smile is null) return;

        description.Client.SendReply(channel, message.Id, $"Смайл {smileName} успешно добавлен");
        _availableSmiles = await _feedDbService.GetAvailableSmiles();
    }

    [Command(Name = "update")]
    public async Task Update(CommandContext context)
    {
        if (context.Description is not TwitchCommandDescription description) return;

        if (!context.Arguments.Any() || context.Arguments.Count < 2) return;

        if (context.Arguments.First() == "reset")
        {
            var foundUser = await _feedDbService.GetUser(context.Arguments[1].ToLower());
            if (foundUser is null) return;
            
            foundUser.TimeToFeed = DateTime.UtcNow;
            await _feedDbService.UpdateUser(foundUser.Id, foundUser);
            return;
        }
        
        if (!context.Arguments.Any() || context.Arguments.Count < 3) return;
        
        switch (context.Arguments.First())
        {
            case "time":
            {
                var foundUser = await _feedDbService.GetUser(context.Arguments[1]);
                if (foundUser is null) return;
            
                if (!int.TryParse(context.Arguments[2], out var result)) return;
            
                foundUser.TimeToFeed += TimeSpan.FromMinutes(result);
                await _feedDbService.UpdateUser(foundUser.Id, foundUser);
                return;
            }
            case "count":
            {
                var foundUser = await _feedDbService.GetUser(context.Arguments[1]);
                if (foundUser is null) return;
            
                if (!int.TryParse(context.Arguments[2], out var result)) return;
            
                foundUser.FeedCount = result;
                await _feedDbService.UpdateUser(foundUser.Id, foundUser);
                return;
            }
            case "size":
            {
                var foundSmile = await _feedDbService.GetSmile(context.Arguments[1]);
                if (foundSmile is null) return;

                if (!double.TryParse(context.Arguments[2], NumberStyles.Float, CultureInfo.InvariantCulture,
                        out var result)) return;
            
                foundSmile.Size = result;
                await _feedDbService.UpdateSmile(foundSmile.Id, foundSmile);
                return;
            }
        }
    }
}