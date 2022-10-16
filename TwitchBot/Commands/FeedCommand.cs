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
    private List<string> _availableSmiles;

    public FeedCommand(FeedDbService feedDbService)
    {
        _feedDbService = feedDbService;
        _subCommands = new Dictionary<string, Func<ITwitchClient, ChatCommand, ChatMessage, Task>>
        {
            {"update", Update},
            {"add", Add}
        };
        _subCommands.Add("status", GetStatus);
        _availableSmiles = _feedDbService.GetAvailableSmiles().Result;
    }
    
    public async Task Execute(ITwitchClient client, ChatCommand command, ChatMessage message)
    {
        var foundUser = await _feedDbService.GetUserAsync(message.Username) ??
                        await _feedDbService.AddUser(message.Username);

        if (command.ArgumentsAsList.Count == 1 && _availableSmiles.Any(s => s == command.ArgumentsAsList[0]))
        {
            var smileName = command.ArgumentsAsList.First();
            if (foundUser is null) return;

            var smile = await _feedDbService.GetSmileAsync(foundUser, smileName) ??
                        await _feedDbService.AddSmileAsync(foundUser, smileName);
            if (smile is null) return;

            if (smile.Timer > DateTime.UtcNow)
            {
                var timeToEnd = smile.Timer - DateTime.UtcNow;
                client.SendMention(message.Channel, message.DisplayName,
                    $"До следующей кормежки {timeToEnd.TotalHours:00}:{timeToEnd:mm\\:ss}. Жди");
                return;
            }
            
            smile.FeedCount++;
            smile.Size += new Random().NextFloat(0.005f, 0.5f);
            smile.Timer = DateTime.UtcNow + TimeSpan.FromMinutes(5);
            await _feedDbService.UpdateSmileAsync(smile.Id, smile);

            client.SendMention(message.Channel, message.DisplayName,
                $"Ты покормил(а) {smile.Name} {smile.FeedCount} раз(а). Размер = {smile.Size}");
        }
        
        if (!command.ArgumentsAsList.Any()) return;

        if (_subCommands.TryGetValue(command.ArgumentsAsList[0], out var cmd))
            await cmd.Invoke(client, command, message);
    }

    private async Task GetStatus(ITwitchClient client, ChatCommand command, ChatMessage message)
    {
        var smiles = await _feedDbService.GetSmiles(message.Username);
        var status = smiles
            .Where(e => e.FeedCount > 0)
            .Aggregate("Ты поккормил(а) - ",
            (current, smile) => current + $"{smile.Name} {smile.FeedCount} раз(а), размер {smile.Size}; ");

        client.SendMention(message.Channel, message.DisplayName, status);
    }

    private async Task Add(ITwitchClient client, ChatCommand command, ChatMessage message)
    {
        var sender = await _feedDbService.GetUserAsync(message.Username);
        if (sender is null || sender.Permission > UserPermission.Moderator)
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
        if (!command.ArgumentsAsList.Any()) return;
        var sender = await _feedDbService.GetUserAsync(message.Username);
        if (sender is null || sender.Permission > UserPermission.Owner)
        {
            client.SendMention(message.Channel, message.DisplayName, "Требуются права администратора");
            return;
        }
        
        if (command.ArgumentsAsList.Count < 4) return;
        var userToUpdate = await _feedDbService.GetUserAsync(command.ArgumentsAsList[1].ToLower()) ??
                           await _feedDbService.AddUser(command.ArgumentsAsList[1].ToLower());
        
        if (userToUpdate is null) return;
        var smile = await _feedDbService.GetSmileAsync(userToUpdate, command.ArgumentsAsList[2]) ??
                    await _feedDbService.AddSmileAsync(userToUpdate, command.ArgumentsAsList[2]);
        if (smile is null) return;
        
        if (command.ArgumentsAsList[3] == "size")
        {
            if (command.ArgumentsAsList.Count < 5) return;
            if (!float.TryParse(command.ArgumentsAsList[4], out var result)) return;
            smile.Size = result;

            await _feedDbService.UpdateSmileAsync(smile.Id, smile);
            client.SendMention(message.Channel, message.DisplayName, "Размер обновлен");
        }
        if (command.ArgumentsAsList[3] == "count")
        {
            if (command.ArgumentsAsList.Count < 5) return;
            if (!int.TryParse(command.ArgumentsAsList[4], out var result)) return;
            smile.FeedCount = result;

            await _feedDbService.UpdateSmileAsync(smile.Id, smile);
            client.SendMention(message.Channel, message.DisplayName, "Количество обновлен");
        }
        if (command.ArgumentsAsList[3] == "timer")
        {
            if (command.ArgumentsAsList.Count < 5) return;
            if (command.ArgumentsAsList[4] == "reset")
            {
                smile.Timer = DateTime.UtcNow;
                await _feedDbService.UpdateSmileAsync(smile.Id, smile);
                client.SendMention(message.Channel, message.DisplayName, "Таймер обновлен");
            }
            if (command.ArgumentsAsList.Count < 6) return;
            if (command.ArgumentsAsList[4] == "add")
            {
                if (!int.TryParse(command.ArgumentsAsList[5], out var result)) return;
                smile.Timer += TimeSpan.FromMinutes(result);
            }
            
            await _feedDbService.UpdateSmileAsync(smile.Id, smile);
            client.SendMention(message.Channel, message.DisplayName, "Таймер обновлен");
        }
    }
}