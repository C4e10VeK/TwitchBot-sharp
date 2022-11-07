namespace TwitchBot.Models;

public class BotConfig
{
    public string Token { get; set; }
    public string Name { get; set; }
    public string ClientId { get; set; }
    public string TokenApi { get; set; }
    public char Prefix { get; set; }
    
    public string WebHookAnonc { get; set; }
    public List<string> Channels { get; set; }
}