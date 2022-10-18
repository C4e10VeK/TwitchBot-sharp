namespace TwitchBot.CommandLib.Models;

public interface ICommandModule
{
    Task Execute(CommandContext ctx);
}