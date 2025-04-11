using System.Reflection;

namespace Backend.Extensions;

public static class WebApplicationExtensions
{
    public static void MapApplicationEndpoints(this WebApplication app, Assembly assembly)
    {
        var mappers = assembly
            .DefinedTypes
            .Where(t => t is { IsAbstract: false, IsInterface: false } && t.IsAssignableTo(typeof(IMapEndpoint)))
            .Select(t => (IMapEndpoint)Activator.CreateInstance(t)!)
            .ToArray();

        foreach (var mapper in mappers)
        {
            mapper.MapEndpoint(app);
        }
    }
}