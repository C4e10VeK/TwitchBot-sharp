namespace TwitchBot.Commands;

public class CommandContainer
{
    private ServiceProvider _serviceProvider;
    private Dictionary<string, Type> _commandsType;

    public CommandContainer(ServiceProvider serviceProvider, Dictionary<string, Type> commandsType)
    {
        _serviceProvider = serviceProvider;
        _commandsType = commandsType;
    }

    public bool GetCommand(string name, out ICommand? command)
    {
        if (!_commandsType.TryGetValue(name, out var type))
        {
            command = null;
            return false;
        }

        command = (ICommand?) _serviceProvider.GetService(type);
        return true;
    }
}