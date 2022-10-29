using Newtonsoft.Json;

namespace WebhookDiscord.Models;

public class WebHookMessage
{
    [JsonProperty("content")]
    public string? Content { get; set; }
    [JsonProperty("username")]
    public string? Username { get; set; }
    [JsonProperty("avatar_url")]
    public string? AvatarUrl { get; set; }
    [JsonProperty("embeds")]
    public List<Embed.Embed> Embeds { get; set; } = new ();

}