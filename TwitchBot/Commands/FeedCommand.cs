using System.Globalization;
using TwitchBot.CommandLib;
using TwitchBot.Extensions;
using TwitchBot.Models;
using TwitchBot.Services;
using TwitchBot.CommandLib.Attributes;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;

namespace TwitchBot.Commands;

[Group(Name = "feed")]
public class FeedCommand : ICommandModule
{
    private readonly FeedDbService _feedDbService;
    private List<string?> _availableSmiles;

    public FeedCommand() { }
    
    public FeedCommand(FeedDbService feedDbService)
    {
        _feedDbService = feedDbService;
        _availableSmiles = _feedDbService.GetAvailableSmiles().Result;
    }

    [Command(Name = "top")]
    public async Task GetTop(CommandContext context)
    {
        if (context.Description is not CommandDescription description) return;
        
        var smiles = (await _feedDbService.GetSmiles())
            .Where(e => e.FeedCount > 0)
            .GroupBy(e => e.User)
            .Select(g => g.MaxBy(e => e.Size))
            .Take(new Range(0, 4))
            .ToList();
        
        smiles.Sort((s1, s2) => s2.Size.CompareTo(s1.Size));

        var top = "Топ кормильцев: ";
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
            top += $"{index + 1}: {smile?.User}, {smile?.Name} , размер = {smile?.Size:n3} см; ";
        }
        
        description.Client.SendMention(description.Message.Channel, description.Message.DisplayName, top);
    }

    public async Task Execute(CommandContext context)
    {
        if (context.Description is not CommandDescription description) return;
        
        var foundUser = await _feedDbService.GetUserAsync(description.Message.Username) ??
                        await _feedDbService.AddUser(description.Message.Username);

        if (!context.Arguments.Any())
        {
            description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
                $"Можно кормить {string.Join(" ", _availableSmiles)}");
            return;
        }

        if (_availableSmiles.Any(s => s == context.Arguments.First()))
        {
            var smileName = context.Arguments.First();
            if (foundUser is null) return;

            var smileId = foundUser.FeedSmiles[smileName];
            var smile = await _feedDbService.GetSmileAsync(smileId) ??
                        await _feedDbService.AddSmileAsync(foundUser, smileName);
            if (smile is null) return;

            if (smile.Timer > DateTime.UtcNow)
            {
                var timeToEnd = smile.Timer - DateTime.UtcNow;
                description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
                    $"До следующей кормежки {timeToEnd.TotalHours:00}:{timeToEnd:mm\\:ss} peepoFAT . Жди");
                return;
            }
            
            smile.FeedCount++;
            smile.Size += (float)Math.Round(new Random().NextFloat(0.5f, 0.005f), 3);
            smile.Timer = DateTime.UtcNow + TimeSpan.FromMinutes(5);
            await _feedDbService.UpdateSmileAsync(smile.Id, smile);

            description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
                $"Ты покормил(а) {smile.Name} {smile.FeedCount} раз(а). Размер = {smile.Size:n3} см");
        }
    }

    [Command(Name = "status")]
    public async Task GetStatus(CommandContext context)
    {
        if (context.Description is not CommandDescription description) return;
        
        var userName = context.Arguments.Any() ? context.Arguments.First() : description.Message.Username;
        var statusStart = context.Arguments.Any() ? $"{userName} покормил(а) - " : "Ты покормил(а) - ";
        var smiles = await _feedDbService.GetSmiles(userName.ToLower());
        if (!smiles.Any())
        {
            description.Client.SendMention(description.Message.Channel, description.Message.DisplayName, 
                $"{userName} еще никого не кормил");
            return;
        }
        var status = smiles
            .Where(e => e.FeedCount > 0)
            .Aggregate(statusStart,
            (current, smile) => current + $"{smile.Name} {smile.FeedCount} раз(а), размер = {smile.Size:n3} см; ");

        description.Client.SendMention(description.Message.Channel, description.Message.DisplayName, status);
    }

    [Command(Name = "add")]
    public async Task Add(CommandContext context)
    {
        if (context.Description is not CommandDescription description) return;
        
        var sender = await _feedDbService.GetUserAsync(description.Message.Username);
        if (sender is null || !description.Message.IsModerator || sender.Permission > UserPermission.Moderator)
        {
            description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
                    "Требуются права модератора");
            return;
        }

        if (!context.Arguments.Any()) return;

        var smileToAdd = context.Arguments.First();
        await _feedDbService.AddAvailableSmile(smileToAdd);
        _availableSmiles = await _feedDbService.GetAvailableSmiles();
        description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
            $"Смайл {smileToAdd} добавлен");
    }

    [Command(Name = "update")]
    public async Task Update(CommandContext context)
    {
        if (context.Description is not CommandDescription description) return;
        
        if (!context.Arguments.Any()) return;
        var sender = await _feedDbService.GetUserAsync(description.Message.Username);
        if (sender is null || sender.Permission > UserPermission.Owner)
        {
            description.Client.SendMention(description.Message.Channel, description.Message.DisplayName,
                "Требуются права администратора");
            return;
        }

        if (context.Arguments.Count < 2) return;
        var foundUser = await _feedDbService.GetUserAsync(context.Arguments[1].ToLower());
        
        if (context.Arguments.First() == "reset")
        {
            await ResetTimer(foundUser, context.Arguments.ToArray()[2..]);
        }
        
        if (context.Arguments.First() == "size")
        {
            await SetSize(foundUser, context.Arguments.ToArray()[2..]);
        }
        
        if (context.Arguments.First() == "count")
        {
            await SetCount(foundUser, context.Arguments.ToArray()[2..]);
        }
        
        if (context.Arguments.First() == "time")
        {
            await SetTime(foundUser, context.Arguments.ToArray()[2..]);
        }
    }

    private async Task ResetTimer(User? user, IReadOnlyList<string?> args)
    {
        if (user is null) return;
        if (args.Count == 1)
        {
            var smile = await _feedDbService.GetSmileAsync(user.FeedSmiles[args[0]]);
            if (smile is null) return;
                
            smile.Timer = DateTime.UtcNow;
            await _feedDbService.UpdateSmileAsync(smile.Id, smile);
            return;
        }
        var smiles = (await _feedDbService.GetSmiles(user))
            .Where(s => s.FeedCount > 0);
        foreach (var smile in smiles)
        {
            smile.Timer = DateTime.UtcNow;
            await _feedDbService.UpdateSmileAsync(smile.Id, smile);
        }
    }
    
    private async Task SetSize(User? user, IReadOnlyList<string?> args)
    {
        if (user is null || !args.Any()) return;
        if (args.Count == 2)
        {
            var smile = await _feedDbService.GetSmileAsync(user.FeedSmiles[args[0]]);
            if (smile is null) return;

            if (!float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                return;

            smile.Size = result;
            await _feedDbService.UpdateSmileAsync(smile.Id, smile);
            return;
        }
        if (!float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return;
        var smiles = await _feedDbService.GetSmiles(user);
        foreach (var smile in smiles)
        {
            smile.Size = value;
            await _feedDbService.UpdateSmileAsync(smile.Id, smile);
        }
    }

    private async Task SetCount(User? user, IReadOnlyList<string?> args)
    {
        if (user is null || !args.Any()) return;
        if (args.Count == 2)
        {
            var smile = await _feedDbService.GetSmileAsync(user.FeedSmiles[args[0]]);
            if (smile is null) return;
            
            if (!int.TryParse(args[1], out var result))
                return;
            
            smile.FeedCount = result;
            await _feedDbService.UpdateSmileAsync(smile.Id, smile);
            return;
        }
        
        if (!int.TryParse(args[0], out var value))
            return;
        var smiles = await _feedDbService.GetSmiles(user);
        foreach (var smile in smiles)
        {
            smile.FeedCount = value;
            await _feedDbService.UpdateSmileAsync(smile.Id, smile);
        }
    }
    
    private async Task SetTime(User? user, IReadOnlyList<string?> args)
    {
        if (user is null || !args.Any()) return;
        if (args.Count == 2)
        {
            var smile = await _feedDbService.GetSmileAsync(user.FeedSmiles[args[0]]);
            if (smile is null) return;
            
            if (!int.TryParse(args[1], out var result))
                return;
            
            smile.Timer = DateTime.UtcNow + TimeSpan.FromMinutes(result);
            await _feedDbService.UpdateSmileAsync(smile.Id, smile);
            return;
        }
        
        if (!int.TryParse(args[0], out var value))
            return;
        var smiles = await _feedDbService.GetSmiles(user);
        foreach (var smile in smiles)
        {
            smile.Timer = DateTime.UtcNow + TimeSpan.FromMinutes(value);
            await _feedDbService.UpdateSmileAsync(smile.Id, smile);
        }
    }
}