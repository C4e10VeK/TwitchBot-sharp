namespace TwitchBot.CommandLib;

public interface ICommandDescription
{
    public object? Sender { get; }
    public object? Detail { get; }
}