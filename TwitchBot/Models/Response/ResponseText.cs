using Newtonsoft.Json;

namespace TwitchBot.Models.Response;

public class ResponseText
{
    [JsonProperty("text")]
    public string Text { get; set; }
}