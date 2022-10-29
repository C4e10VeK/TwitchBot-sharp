using System.Text;
using WebhookDiscord.Models;
using WebhookDiscord.Models.Embed;

namespace WebhookDiscord;

public class DiscordWebHook
{
    private readonly HttpClient _client = new();

    private Uri _uri;
    private string _name;

    public Uri Uri => _uri;

    public string Name => _name;

    public DiscordWebHook(string uri, string botname)
    {
        _uri = new Uri(uri);
        _name = botname;
    }
    
    public DiscordWebHook(Uri uri, string botname)
    {
        _uri = uri;
        _name = botname;
    }

    public async Task Send(string message, Color? color = null, string? avatarUrl = null, List<Embed>? embeds = null)
    {
        var builder = new WebHookMessageBuilder()
            .SetContent(message);
        
        
        if (avatarUrl is not null)
            builder.SetAvatar(avatarUrl);
        if (embeds is not null)
            builder.SetEmbeds(embeds);
        
        var msg = builder.SetUsername(Name)
            .BuildJson();

        await Post(msg);
    }
    
    private async Task Post(string content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _uri)
        {
            Headers =
            {
                {"Host", "discord.com"},
                { "Accept", "application/json" }
            },
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        await _client.SendAsync(request);
    }
}