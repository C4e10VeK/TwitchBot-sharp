using Newtonsoft.Json;
using WebhookDiscord.Models;
using WebhookDiscord.Models.Embed;

namespace WebhookDiscord;

public class WebHookMessageBuilder
{
    private const int MaxEmbedsCount = 10;
    private WebHookMessage _message = new();

    public WebHookMessageBuilder SetContent(string content)
    {
        _message.Content = content;
        return this;
    }

    public WebHookMessageBuilder SetUsername(string username)
    {
        _message.Username = username;
        return this;
    }

    public WebHookMessageBuilder SetAvatar(string url)
    {
        _message.AvatarUrl = url;
        return this;
    }

    public WebHookMessageBuilder AddEmbed(Embed embed)
    {
        if (_message.Embeds.Count == MaxEmbedsCount)
            return this;

        _message.Embeds.Add(embed);
        return this;
    }

    public WebHookMessageBuilder SetEmbeds(List<Embed> embeds)
    {
        if (embeds.Count > MaxEmbedsCount)
            return this;
        if (_message.Embeds.Count == MaxEmbedsCount)
            return this;
        if (_message.Embeds.Count + embeds.Count > MaxEmbedsCount)
            return this;
        
        _message.Embeds.AddRange(embeds);
        
        return this;
    }

    public WebHookMessage Build()
    {
        _message.Username = string.IsNullOrWhiteSpace(_message.Username) ? "WebHookBot" : _message.Username;
        
        return _message;
    }

    public string BuildJson()
    {
        return JsonConvert.SerializeObject(_message, Formatting.Indented, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Error,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Auto,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatString = "o"
        });
    }
}