using Microsoft.Extensions.Options;
using TwitchBot.CommandLib;
using TwitchBot.Commands;
using TwitchBot.Extensions;
using TwitchBot.Models;
using TwitchLib.Api;
using TwitchLib.Api.Core.HttpCallHandlers;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.PubSub;

namespace TwitchBot.Services;

public class BotService : BackgroundService
{
    private readonly TwitchClient _client;
    private readonly List<TwitchPubSub> _pubSubs;
    private readonly ILogger<BotService> _logger;
    private readonly FeedDbService _feedDbService;
    private readonly CommandContainer _commandContainer;

    public BotService(ILogger<BotService> logger, IOptions<BotConfig> options, FeedDbService service)
    {
        var config = options.Value;
        var credentials = new ConnectionCredentials(config.Name, config.Token);

        _client = new TwitchClient();
        _pubSubs = new List<TwitchPubSub>();
        _client.Initialize(credentials, config.Channels, config.Prefix);
        _logger = logger;
        _feedDbService = service;
        
        _client.OnLog += ClientOnOnLog;
        _client.OnMessageReceived += ClientOnMessageReceived;
        _client.OnChatCommandReceived += ClientOnChatCommandReceived;

        foreach (var channel in config.Channels)
        {
            var pubSub = new TwitchPubSub();
            pubSub.OnStreamUp += (sender, args) => Task.Run(() =>
            {
                _client.SendMessage(args.ChannelId, $"{args.ChannelId} запустил поток Pog");
            });
            
            pubSub.OnStreamDown += (sender, args) => Task.Run(() =>
            {
                _client.SendMessage(args.ChannelId, "Пока потокер FeelsWeakMan");
            });

            pubSub.ListenToVideoPlayback(channel);
            pubSub.Connect();
            
            _pubSubs.Add(pubSub);
        }
        
        _commandContainer = new CommandContainer()
            .Add<FeedCommand>(service)
            .Add<UserCommand>(service);
    }

    private void ClientOnOnLog(object? sender, OnLogArgs e)
    {
        // if (e.Data is null) return; 
        // _logger.LogInformation("{Message}", e.Data);
    }

    private async Task OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
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

        var commandContext = new CommandContext
        {
            Description = new CommandDescription
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
        await Task.Delay(500, stoppingToken);
    }
}