using Microsoft.Extensions.Options;
using TwitchBot.Extensions;
using TwitchBot.Models;
using TwitchBot.Models.Request;
using TwitchBot.Models.Response;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Enums;
using TwitchLib.PubSub.Events;
using WebhookDiscord;
using WebhookDiscord.Models;
using WebhookDiscord.Models.Embed;
using OnLogArgs = TwitchLib.Client.Events.OnLogArgs;

namespace TwitchBot.Services;

public class BotService : BackgroundService
{
    private readonly TwitchClient _client;
    private readonly TwitchPubSub _pubSub;
    private readonly TwitchAPI _api;
    private readonly ILogger<BotService> _logger;
    private readonly Thread _autoPong;
    private static readonly TimeSpan AutoPongInterval = TimeSpan.FromMinutes(4);
    private readonly DiscordWebHook _discordWebHook;
    private IWebApiService _webApiService;
    private DateTime _anonncDelay = DateTime.Now;
    private bool _isActivatedPrediction = false;

    public BotService(ILogger<BotService> logger, IOptions<BotConfig> options, IWebApiService webApiService)
    {
        var config = options.Value;
        var credentials = new ConnectionCredentials(config.Name, config.Token);

        _client = new TwitchClient
        {
            DisableAutoPong = true
        };
        _client.Initialize(credentials, config.Channels, config.Prefix);
        _logger = logger;
        _webApiService = webApiService;

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
        
        _discordWebHook = new DiscordWebHook(config.WebHookAnonc, "Twitch Announcement");
        
        foreach (var configChannel in config.Channels)
        {
            _pubSub.ListenToPredictions(configChannel.GetChannelId(_api));
            _pubSub.ListenToVideoPlayback(configChannel.GetChannelId(_api));
        }

        _pubSub.OnPubSubServiceConnected += (_, _) => _pubSub.SendTopics(config.Token);
        _pubSub.OnPubSubServiceError += (_, args) => _logger.LogError("{Message}", args.Exception.Message);
        // _pubSub.OnPrediction += OnPubSubPrediction;
        _pubSub.OnStreamUp += (_, _) => Task.Run(OnStreamUp);
        
        _autoPong = new Thread(() =>
        {
            while (_client.IsConnected)
            {
                _client.SendRaw("PONG :tmi.twitch.tv");
                Thread.Sleep(AutoPongInterval);
            }
        });
    }

    private async Task OnStreamUp()
    {
        if (_anonncDelay > DateTime.Now) return;
        var color = new Color {R = 100, G = 65, B = 165};

        var embed = new Embed
        {
            Title = "Screamlark запустил поток",
            Description = "Скорее все залетайте",
            Color = color,
            Thumbnail = new EmbedMedia {Url = "https://static-cdn.jtvnw.net/jtv_user_pictures/aa7aaab0-d593-457b-9ea1-3a1997fe5332-profile_image-70x70.png"},
            Url = "https://www.twitch.tv/screamlark",
            Footer = new EmbedFooter {Text = _discordWebHook.Name},
            TimeStamp = DateTime.UtcNow
        };

        await _discordWebHook.Send("@everyone", color, "https://cdn-icons-png.flaticon.com/512/5968/5968819.png",
            new List<Embed> {embed});
        _anonncDelay = DateTime.Now.AddMinutes(5);
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

        var result = await _webApiService.CallApi<ResponseText, CommandRequest>("/RunCommand", HttpMethod.Post, 
            new CommandRequest
            {
                Text = $"{command.CommandText} {command.ArgumentsAsString}",
                Username = chatMessage.Username
            });

        if (result is null)
            return;
        _client.SendReply(chatMessage.Channel, chatMessage.Id, result.Text);
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
                if (_isActivatedPrediction) return;
                _client.SendMention(channel, channel.Channel, "запустили прогноз OOOO");
                _isActivatedPrediction = true;
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
                _isActivatedPrediction = false;
                return;
            }
            case PredictionStatus.Canceled:
                _client.SendMention(channel, channel.Channel, "прогноз отменен MMMM");
                _isActivatedPrediction = false;
                return;
            default:
                return;
        }
    }
}