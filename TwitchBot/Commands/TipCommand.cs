using System.Text;
using Newtonsoft.Json;
using TwitchBot.CommandLib.Attributes;
using TwitchBot.CommandLib.Models;
using TwitchBot.Models;

namespace TwitchBot.Commands;

public class TipCommand : CommandModule
{
    internal class QueryBlab
    {
        [JsonProperty("query")]
        public string Query { get; set; }
        [JsonProperty("intro")]
        public int Intro { get; set; }
        [JsonProperty("filter")]
        public int Filter { get; set; }
    }
    
    internal class ResponseBlab
    {
        [JsonProperty("bad_query")]
        public int BadQuery { get; set; }
        [JsonProperty("query")]
        public string Query { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("error")]
        public int Error { get; set; }
        [JsonProperty("is_cached")]
        public int IsCached { get; set; }
        [JsonProperty("empty_zeliboba")]
        public int EmptyZeliboba { get; set; }
        [JsonProperty("intro")]
        public int Intro { get; set; }
        [JsonProperty("signature")]
        public string Signature { get; set; }
    }
    
    [Command(Name = "tip")]
    public override async Task Execute(CommandContext ctx)
    {
        if (ctx.Description is not TwitchCommandDescription description) return;

        var channel = description.Message.Channel;
        var message = description.Message;
        
        var content = JsonConvert.SerializeObject(new QueryBlab
        {
            Query = $"{description.Message.DisplayName} вот тебе совет:",
            Intro = 11,
            Filter = 1
        });
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://yandex.ru/lab/api/yalm/text3")
        {
            Headers =
            {
                { "Origin", "https://yandex.ru" },
                { "Referer", "https://yandex.ru/" }
            },
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        };

        
        var response = await client.SendAsync(request);
        var rawResult = await response.Content.ReadAsStringAsync();
        var textJson = JsonConvert.DeserializeObject<ResponseBlab>(rawResult);

        var result = $"{textJson?.Query} {textJson?.Text}";
        
        description.Client.SendReply(channel, message.Id, result);
    }
}