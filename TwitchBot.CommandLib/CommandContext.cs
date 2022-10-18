namespace TwitchBot.CommandLib;

public class CommandContext
{
    public  ICommandDescription? Description { get; set; }
    public IReadOnlyList<string> Arguments { get; set; } = new List<string>();
}