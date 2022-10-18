namespace TwitchBot.CommandLib.Models;

public class CommandContext
{
    public  ICommandDescription? Description { get; set; }
    public IReadOnlyList<string> Arguments { get; set; } = new List<string>();
}