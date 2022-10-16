using System.Globalization;
using TwitchBot.Extensions;
using TwitchBot.Models;
using TwitchBot.Services;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;

namespace TwitchBot.Commands;

public class FeedCommand : ICommand
{
    private readonly FeedDbService _feedDbService;
    private readonly Dictionary<string, Func<ITwitchClient, ChatCommand, ChatMessage, Task>> _subCommands;
    private List<string?> _availableSmiles;

    public FeedCommand(FeedDbService feedDbService)
    {
        _feedDbService = feedDbService;
        _subCommands = new Dictionary<string, Func<ITwitchClient, ChatCommand, ChatMessage, Task>>
        {
            {"update", Update},
            {"add", Add},
            {"status", GetStatus},
            {"top", GetTop}
        };
        _availableSmiles = _feedDbService.GetAvailableSmiles().Result;
    }

    private async Task GetTop(ITwitchClient client, ChatCommand command, ChatMessage message)
    {
        var smiles = (await _feedDbService.GetSmiles())
            .Where(e => e.FeedCount > 0)
            .GroupBy(e => e.User)
            .Select(g => g.MaxBy(e => e.Size))
            .Take(new Range(0, 4))
            .ToList();
        
        smiles.Sort((s1, s2) => s2.Size.CompareTo(s1.Size));

        var top = "Топ кормящих: ";
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
            top += $"{index + 1}: {smile?.User}, смайл - {smile?.Name} , размер = {smile?.Size:n3} см; ";
        }
        
        client.SendMention(message.Channel, message.DisplayName, top);
    }

    public async Task Execute(ITwitchClient client, ChatCommand command, ChatMessage message)
    {
        var foundUser = await _feedDbService.GetUserAsync(message.Username) ??
                        await _feedDbService.AddUser(message.Username);

        if (!command.ArgumentsAsList.Any())
        {
            client.SendMention(message.Channel, message.DisplayName,
                $"Можно кормить {string.Join(" ", _availableSmiles)}");
            return;
        }

        if (command.ArgumentsAsList.Count == 1 && _availableSmiles.Any(s => s == command.ArgumentsAsList[0]))
        {
            var smileName = command.ArgumentsAsList.First();
            if (foundUser is null) return;

            var smileId = foundUser.FeedSmiles[smileName];
            var smile = await _feedDbService.GetSmileAsync(smileId) ??
                        await _feedDbService.AddSmileAsync(foundUser, smileName);
            if (smile is null) return;

            if (smile.Timer > DateTime.UtcNow)
            {
                var timeToEnd = smile.Timer - DateTime.UtcNow;
                client.SendMention(message.Channel, message.DisplayName,
                    $"До следующей кормежки {timeToEnd.TotalHours:00}:{timeToEnd:mm\\:ss} peepoFAT . Жди");
                return;
            }
            
            smile.FeedCount++;
            smile.Size += (float)Math.Round(new Random().NextFloat(0.5f, 0.005f), 3);
            smile.Timer = DateTime.UtcNow + TimeSpan.FromMinutes(5);
            await _feedDbService.UpdateSmileAsync(smile.Id, smile);

            client.SendMention(message.Channel, message.DisplayName,
                $"Ты покормил(а) {smile.Name} {smile.FeedCount} раз(а). Размер = {smile.Size:n3} см");
        }
        
        if (!command.ArgumentsAsList.Any()) return;

        if (_subCommands.TryGetValue(command.ArgumentsAsList[0], out var cmd))
            await cmd.Invoke(client, command, message);
    }

    private async Task GetStatus(ITwitchClient client, ChatCommand command, ChatMessage message)
    {
        var userName = command.ArgumentsAsList.Count < 2 ? message.Username : command.ArgumentsAsList[1];
        var statusStart = command.ArgumentsAsList.Count < 2 ? "Ты покормил(а) - " : $"{userName} покормил(а) = ";
        var smiles = await _feedDbService.GetSmiles(userName);
        if (!smiles.Any()) return;
        var status = smiles
            .Where(e => e.FeedCount > 0)
            .Aggregate(statusStart,
            (current, smile) => current + $"{smile.Name} {smile.FeedCount} раз(а), размер = {smile.Size:n3} см; ");

        client.SendMention(message.Channel, message.DisplayName, status);
    }

    private async Task Add(ITwitchClient client, ChatCommand command, ChatMessage message)
    {
        var sender = await _feedDbService.GetUserAsync(message.Username);
        if (sender is null || !message.IsModerator || sender.Permission > UserPermission.Moderator)
        {
            client.SendMention(message.Channel, message.DisplayName, "Требуются права модератора");
            return;
        }

        if (command.ArgumentsAsList.Count < 2) return;

        var smileToAdd = command.ArgumentsAsList[1];
        await _feedDbService.AddAvailableSmile(smileToAdd);
        _availableSmiles = await _feedDbService.GetAvailableSmiles();
        client.SendMention(message.Channel, message.DisplayName, $"Смайл {smileToAdd} добавлен");
    }

    private async Task Update(ITwitchClient client, ChatCommand command, ChatMessage message)
    {
        if (command.ArgumentsAsList.Count < 2) return;
        var sender = await _feedDbService.GetUserAsync(message.Username);
        if (sender is null || sender.Permission > UserPermission.Owner)
        {
            client.SendMention(message.Channel, message.DisplayName, "Требуются права администратора");
            return;
        }

        if (command.ArgumentsAsList.Count < 3) return;
        var foundUser = await _feedDbService.GetUserAsync(command.ArgumentsAsList[2].ToLower());
        
        if (command.ArgumentsAsList[1] == "reset")
        {
            await ResetTimer(foundUser, command.ArgumentsAsList.ToArray()[3..]);
        }

        if (command.ArgumentsAsList[1] == "size")
        {
            await SetSize(foundUser, command.ArgumentsAsList.ToArray()[3..]);
        }

        if (command.ArgumentsAsList[1] == "count")
        {
            await SetCount(foundUser, command.ArgumentsAsList.ToArray()[3..]);
        }
        
        if (command.ArgumentsAsList[1] == "time")
        {
            await SetTime(foundUser, command.ArgumentsAsList.ToArray()[3..]);
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
        
        if (!int.TryParse(args[1], out var value))
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
        
        if (!int.TryParse(args[1], out var value))
            return;
        var smiles = await _feedDbService.GetSmiles(user);
        foreach (var smile in smiles)
        {
            smile.Timer = DateTime.UtcNow + TimeSpan.FromMinutes(value);
            await _feedDbService.UpdateSmileAsync(smile.Id, smile);
        }
    }
}