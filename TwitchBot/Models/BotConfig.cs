namespace TwitchBot.Models;

public class BotConfig
{
    public string Token { get; set; }
    public string Name { get; set; }
    public char Prefix { get; set; }
    public List<string> Channels { get; set; }
}