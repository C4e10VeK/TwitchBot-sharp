using Newtonsoft.Json;

namespace WebhookDiscord.Models.Embed;

public class EmbedProvider
{
    [JsonProperty("name")]
    public string? Name { get; set; }
    [JsonProperty("url")]
    public string? Url { get; set; }
}