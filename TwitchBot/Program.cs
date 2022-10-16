using TwitchBot.Models;
using TwitchBot.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((builder, services) =>
    {
        services.Configure<BotConfig>(builder.Configuration.GetSection("BotConfig"));
        services.Configure<FeedDBConfig>(builder.Configuration.GetSection("FeedDBConfig"));
        services.AddSingleton<FeedDbService>();
        services.AddHostedService<BotService>();
    })
    .Build();

await host.RunAsync();