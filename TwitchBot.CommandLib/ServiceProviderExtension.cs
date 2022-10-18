using Microsoft.Extensions.DependencyInjection;

namespace TwitchBot.CommandLib;

public static class ServiceProviderExtension
{
    public static List<ICommandModule> GetCommandModules(this IServiceProvider serviceProvider)
    {
        return serviceProvider
            .GetServices(typeof(ICommandModule))
            .Cast<ICommandModule>()
            .ToList();
    }
}