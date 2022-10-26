using Microsoft.Extensions.Options;
using TwitchBot.CommandLib;
using TwitchBot.CommandLib.Models;
using TwitchBot.Commands;
using TwitchBot.Extensions;
using TwitchBot.Models;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Enums;
using TwitchLib.PubSub.Events;
using OnLogArgs = TwitchLib.Client.Events.OnLogArgs;

namespace TwitchBot.Services;

public class BotService : BackgroundService
{
    private readonly TwitchClient _client;
    private readonly TwitchPubSub _pubSub;
    private readonly TwitchAPI _api;
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

        _api = new TwitchAPI
        {
            Settings =
            {
                ClientId = config.ClientId,
                AccessToken = config.TokenApi
            }
        };

        _pubSub = new TwitchPubSub();

        _pubSub.OnPrediction += OnPubSubPrediction; 
        _pubSub.OnPubSubServiceConnected += (_, _) =>
        {
            _pubSub.ListenToPredictions(config.Channels.First().GetChannelId(_api));
            _pubSub.SendTopics(config.Token);
        };
        _pubSub.OnPubSubServiceError += (_, args) =>
        {
            _logger.LogError("{Message}", args.Exception.Message);
        };
        
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
            .Add<AutoCommand>(databaseService)
            .Add<TipCommand>();
    }

    public override void Dispose()
    {
        _autoPong.Join(TimeSpan.FromSeconds(5));
        base.Dispose();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _client.Connect();
        _pubSub.Connect();
        _autoPong.Start();
        await Task.Delay(0, stoppingToken);
    }
    
    private void ClientOnLog(object? sender, OnLogArgs e)
    {
        // if (e.Data is null) return;
        // using var sw = File.AppendText("log.txt");
        // sw.WriteLine("{0:HH:mm:ss}: {1}", e.DateTime, e.Data);
        // sw.Close();
    }

    private Task OnMessageReceived(OnMessageReceivedArgs e)
    {
        var reactStrings = e.ChatMessage.Message.Split(" ")
            .Where(s => s == "pirat")
            .Aggregate((s, x) => s + " " + x);

        _client.SendMessage(e.ChatMessage.Channel, reactStrings);
        return Task.CompletedTask;
    }
    
    private async Task OnChatCommandReceived(OnChatCommandReceivedArgs e)
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
        Task.Run(async () => await OnChatCommandReceived(e), token);
    }

    private void ClientOnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        var token = new CancellationToken();
        Task.Run(async () => await OnMessageReceived(e), token);
    }
    
    private void OnPubSubPrediction(object? sender, OnPredictionArgs args)
    {
        var channel = _client.GetJoinedChannel(args.ChannelId.GetChannelName(_api));
        switch (args.Status)
        {
            case PredictionStatus.Active:
            {
                _client.SendMention(channel, channel.Channel, "запустили прогноз OOOO");
                return;
            }
            case PredictionStatus.Locked:
                _client.SendMention(channel, channel.Channel, "прогноз окончен MMMM");
                return;
            case PredictionStatus.Resolved:
            {
                if (!args.WinningOutcomeId.HasValue) return;
                var winingOutcome = args.Outcomes.First(o => o.Id == args.WinningOutcomeId);
                var topPredictors = winingOutcome.TopPredictors.OrderByDescending(p => p.Points)
                    .ToList();
                var topPointerStr = topPredictors.Any()
                    ? $"Топ поинтер: {topPredictors.First().DisplayName}, кол-во {topPredictors.First().Points}"
                    : "";
                _client.SendMessage(channel, $"Победил вариант: {winingOutcome.Title}. {topPointerStr}");
                return;
            }
            case PredictionStatus.Canceled:
                _client.SendMention(channel, channel.Channel, "прогноз отменен MMMM");
                return;
            default:
                return;
        }
    }
}