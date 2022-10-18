using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TwitchBot.CommandLib.Attributes;

namespace TwitchBot.CommandLib;

public class CommandContainer
{
    private readonly List<Command> _commands;

    public CommandContainer()
    {
        _commands = new List<Command>();
    }

    public CommandContainer Add<T>(params object[] args) where T : class, ICommandModule
    {
        var module = (T?)Activator.CreateInstance(typeof(T), args);
        if (module is null) return this;
        var commandGroup = GetGroupCommand(module);

        var commands = GetCommands(module, commandGroup);

        _commands.AddRange(commands);
        if (commandGroup is null) return this;
        commandGroup.Childrens = commands;
        _commands.Add(commandGroup);

        return this;
    }

    public CommandContainer Add<T>() where T : class, ICommandModule, new()
    {
        var module = new T();
        var commandGroup = GetGroupCommand(module);

        var commands = GetCommands(module, commandGroup);

        _commands.AddRange(commands);
        if (commandGroup is null) return this;
        commandGroup.Childrens = commands;
        _commands.Add(commandGroup);

        return this;
    }

    public async Task Execute(string command, CommandContext context)
    {
        var cmd = _commands.First(c => c.Name == command);
        if ((cmd.Childrens ?? Array.Empty<Command>()).Any())
        {
            var subCommand = context.Arguments.Any() ? context.Arguments.First() : "";

            if (!cmd.Childrens?.Any(c => c.Name == subCommand) ?? true)
            {
                await cmd.Execute(context);
                return;
            }

            context.Arguments = context.Arguments.ToArray()[1..];

            cmd.Childrens?.First(c => c.Name == subCommand).Execute(context);
            return;
        }

        await _commands
            .First(c => c.Name == command && c.Childrens is null && c.Parent is null)
            .Execute(context);
    }

    private Command? GetGroupCommand(ICommandModule module)
    {
        var isGroup = module.GetType().GetCustomAttribute<GroupAttribute>() is not null;
        if (!isGroup) return null;
        var groupAttrib = module.GetType().GetCustomAttributes<GroupAttribute>(false).First();
        var defaultMethod = module.GetType().GetMethod(nameof(module.Execute));
        return new Command
        {
            Name = groupAttrib.Name,
            Module = module,
            CommandMethod = defaultMethod
        };
    }

    private List<Command> GetCommands(ICommandModule module, Command? commandGroup)
    {
        return module.GetType().GetMethods()
            .Where(m => m.GetCustomAttribute<CommandAttribute>(false) is not null)
            .Select(m =>
            {
                var commandAttrib = m.GetCustomAttribute<CommandAttribute>();
                return new Command
                {
                    Name = commandAttrib?.Name,
                    Module = module,
                    CommandMethod = m,
                    Parent = commandGroup
                };
            }).ToList();
    }

}
