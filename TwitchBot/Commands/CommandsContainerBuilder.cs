namespace TwitchBot.Commands;

public class CommandsContainerBuilder
{
    private ServiceCollection _serviceCollection;
    private Dictionary<string, Type> _commandsType;

    public CommandsContainerBuilder()
    {
        _serviceCollection = new ServiceCollection();
        _commandsType = new Dictionary<string, Type>();
    }

    public void AddDBContext<T>() where T : class
    {
        _serviceCollection.AddSingleton<T>();
    }
}