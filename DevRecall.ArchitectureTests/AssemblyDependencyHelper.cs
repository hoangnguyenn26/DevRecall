using System.Reflection;

namespace DevRecall.ArchitectureTests;

internal static class AssemblyDependencyHelper
{
    public static string[] GetReferencedAssemblyNames(
        Assembly assembly)
    {
        return assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .ToArray();
    }
}
