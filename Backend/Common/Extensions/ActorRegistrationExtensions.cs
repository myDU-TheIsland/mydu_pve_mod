using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mod.DynamicEncounters.SDK;
using Mod.DynamicEncounters.Threads.Handles;

namespace Mod.DynamicEncounters.Common.Extensions;

public static class ActorRegistrationExtensions
{
    public static void AddSingleHostedService(this IServiceCollection services, Func<IServiceProvider, IHostedService> implementationFactory)
    {
        services.AddSingleton(implementationFactory);
    }
    
    public static void RegisterActorPlugins(this IServiceCollection services)
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var pluginsFolder = Path.GetDirectoryName(assemblyLocation) + "/plugins";
        var pluginsFiles = Directory.GetFiles(pluginsFolder, "*.dll");

        foreach (var plugin in pluginsFiles)
        {
            var assembly = Assembly.LoadFrom(plugin);
            var types = assembly.GetTypes();
            var actorTypes = types.Where(t => t.IsAssignableTo(typeof(IActor)));

            foreach (var type in actorTypes)
            {
                var instance = Activator.CreateInstance(type);

                if (instance is IActor actor)
                {
                    services.AddHostedService(_ => new ActorLoop(actor));
                }
            }
        }
    }
}