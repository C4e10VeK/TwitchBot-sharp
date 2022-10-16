using Microsoft.Extensions.Options;
using TwitchBot.Commands;
using TwitchBot.Extensions;
using TwitchBot.Models;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace TwitchBot.Services;

public class BotService : BackgroundService
{
    private readonly TwitchClient _client;
    private readonly ILogger<BotService> _logger;
    private readonly FeedDbService _feedDbService;
    private readonly Dictionary<string, ICommand> _commands;

    public BotService(ILogger<BotService> logger, IOptions<BotConfig> options, FeedDbService service)
    {
        var config = options.Value;
        var credentials = new ConnectionCredentials(config.Name, config.Token);

        _client = new TwitchClient();
        _client.Initialize(credentials, config.Channels, config.Prefix);
        _logger = logger;
        _feedDbService = service;
        _commands = new Dictionary<string, ICommand>();
        
        _client.OnLog += ClientOnOnLog;
        _client.OnMessageReceived += ClientOnMessageReceived;
        _client.OnChatCommandReceived += ClientOnChatCommandReceived;
        
        _commands.Add("feed", new FeedCommand(_feedDbService));
        _commands.Add("user", new UserUpdateCommand(_feedDbService));
        _commands.Add("help", new HelpCommand());
    }

    private void ClientOnOnLog(object? sender, OnLogArgs e)
    {
        if (e.Data is null) return; 
        _logger.LogInformation("{Message}", e.Data);
    }

    private async Task OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        _logger.LogInformation("{User}: {Message}", e.ChatMessage.DisplayName, e.ChatMessage.Message);
        var reactStrings = e.ChatMessage.Message.Split(" ")
            .Where(s => s == "pirat")
            .Aggregate((s, x) => s + " " + x);
        
        _client.SendMessage(e.ChatMessage.Channel, reactStrings);
    }
    
    private async Task OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
    {
        var command = e.Command;
        var chatMessage = command.ChatMessage;
        
        var foundUser = await _feedDbService.GetUserAsync(chatMessage.Username) ??
                        await _feedDbService.AddUser(chatMessage.Username);

        if (foundUser is {IsBanned: true})
        {
            _client.SendMention(chatMessage.Channel, chatMessage.DisplayName, "Ты забанен(а), кормить нельзя!!!");
            return;
        }

        if (_commands.TryGetValue(command.CommandText, out var cmd))
            await cmd.Execute(_client, command, chatMessage);
    }
    
    private void ClientOnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
    {
        var token = new CancellationToken();
        Task.Run(async () => await OnChatCommandReceived(sender, e), token);
    }

    private void ClientOnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        var token = new CancellationToken();
        Task.Run(async () => await OnMessageReceived(sender, e), token);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.Connect();
        await Task.Delay(500, stoppingToken);
    }
}