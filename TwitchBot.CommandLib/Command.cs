using System.Reflection;

namespace TwitchBot.CommandLib;

public class Command
{
    public string? Name { get; set; }

    public ICommandModule? Module { get; set; }

    public MethodInfo? CommandMethod { get; set; }

    public Command? Parent { get; set; } = null;

    public IReadOnlyList<Command>? Childrens { get; set; } = null;

    public async Task Execute(CommandContext context)
    {
        if (Module is null) return;

        if (CommandMethod?.Invoke(Module, new object?[] {context}) is Task task)
            await task;
    }
}