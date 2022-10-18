namespace TwitchBot.CommandLib;

public interface ICommandModule
{
    Task Execute(CommandContext ctx);
}