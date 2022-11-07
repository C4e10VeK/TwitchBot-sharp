using TwitchBot.Models;
using TwitchBot.Services;

var host = Host.CreateDefaultBuilder(args)
    .UseSystemd()
    .ConfigureServices((builder, services) =>
    {
        services.Configure<BotConfig>(builder.Configuration.GetSection("BotConfig"))
            .Configure<WebApiConfig>(builder.Configuration.GetSection("WebApi"));
        services.AddSingleton<IWebApiService, WebApiService>()
            .AddHostedService<BotService>();
    })
    .Build();

await host.RunAsync();