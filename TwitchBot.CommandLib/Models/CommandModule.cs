namespace TwitchBot.CommandLib.Models;

public abstract class CommandModule : ICommandModule
{
    public virtual Task Execute(CommandContext ctx)
    {
        return Task.CompletedTask;
    }
}