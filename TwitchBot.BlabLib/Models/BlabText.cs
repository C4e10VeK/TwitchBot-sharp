namespace TwitchBot.BlabLib.Models;

public class BlabText
{
    public string Qyery { get; set; }
    public string Text { get; set; }

    public override string ToString() => $"{Qyery} {Text}";
}