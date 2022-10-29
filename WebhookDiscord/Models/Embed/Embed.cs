using Newtonsoft.Json;
using WebhookDiscord.Converters;

namespace WebhookDiscord.Models.Embed;

public class Embed
{
    [JsonProperty("title")]
    public string? Title { get; set; }
    [JsonProperty("type")]
    public string? Type { get; set; }
    [JsonProperty("description")]
    public string? Description { get; set; }
    [JsonProperty("url")]
    public string? Url { get; set; }
    [JsonProperty("timestamp")]
    public DateTime? TimeStamp { get; set; }
    [JsonProperty("color")]
    [JsonConverter(typeof(ColorToHexConverter))]
    public Color? Color { get; set; }
    [JsonProperty("footer")]
    public EmbedFooter? Footer { get; set; }
    [JsonProperty("image")]
    public EmbedMedia? Image { get; set; }
    [JsonProperty("thumbnail")]
    public EmbedMedia? Thumbnail { get; set; }
    [JsonProperty("video")]
    public EmbedMedia? Video { get; set; }
    [JsonProperty("provider")]
    public EmbedProvider? Provider { get; set; }
    [JsonProperty("author")]
    public EmbedAuthor? Author { get; set; }
    [JsonProperty("fields")]
    public List<EmbedField>? Fields { get; set; }
}