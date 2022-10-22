using Microsoft.Extensions.Options;
using TwitchBot.CommandLib;
using TwitchBot.CommandLib.Models;
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
    private readonly CommandContainer _commandContainer;
    private readonly Thread _autoPong;
    private static readonly TimeSpan AutoPongInterval = TimeSpan.FromMinutes(4);

    public BotService(ILogger<BotService> logger, IOptions<BotConfig> options, FeedDbService databaseService)
    {
        var config = options.Value;
        var credentials = new ConnectionCredentials(config.Name, config.Token);

        _client = new TwitchClient
        {
            DisableAutoPong = true
        };
        _client.Initialize(credentials, config.Channels, config.Prefix);
        _logger = logger;

        _client.OnLog += ClientOnLog;
        _client.OnMessageReceived += ClientOnMessageReceived;
        _client.OnChatCommandReceived += ClientOnChatCommandReceived;

        _autoPong = new Thread(() =>
        {
            while (_client.IsConnected)
            {
                _client.SendRaw("PONG :tmi.twitch.tv");
                Thread.Sleep(AutoPongInterval);
            }
        });

        _commandContainer = new CommandContainer()
            .Add<FeedCommand>(databaseService)
            .Add<UserCommand>(databaseService)
            .Add<AdminCommand>(databaseService)
            .Add<AnimeCommand>(databaseService)
            .Add<AutoCommand>(databaseService);
    }

    private void ClientOnLog(object? sender, OnLogArgs e)
    {
        // if (e.Data is null) return;
        // using var sw = File.AppendText("log.txt");
        // sw.WriteLine("{0:HH:mm:ss}: {1}", e.DateTime, e.Data);
        // sw.Close();
    }

    private Task OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        var reactStrings = e.ChatMessage.Message.Split(" ")
            .Where(s => s == "pirat")
            .Aggregate((s, x) => s + " " + x);

        _client.SendMessage(e.ChatMessage.Channel, reactStrings);
        return Task.CompletedTask;
    }
    
    private async Task OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
    {
        var command = e.Command;
        var chatMessage = command.ChatMessage;

        User? foundUser = null;

        if (foundUser is {IsBanned: true})
        {
            _client.SendMention(chatMessage.Channel, chatMessage.DisplayName, "Ты забанен(а), кормить нельзя!!!");
            return;
        }

        var commandContext = new CommandContext
        {
            Description = new TwitchCommandDescription
            {
                Client = _client,
                Message = chatMessage
            },
            Arguments = command.ArgumentsAsList
        };

        await _commandContainer.Execute(command.CommandText, commandContext);
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
        _autoPong.Start();
        await Task.Delay(500, stoppingToken);
    }

    public override void Dispose()
    {
        _autoPong.Join(TimeSpan.FromSeconds(5));
        base.Dispose();
    }
}