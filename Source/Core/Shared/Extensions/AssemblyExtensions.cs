using System.Reflection;

namespace PlayGround.Shared.Extensions;

public static class AssemblyExtensions
{
    public static Type? GetTypeFromString(this Assembly assembly, string typeName)
    {
        return assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
    }
}
