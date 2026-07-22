using System.Reflection;

namespace DevRecall.Infrastructure;

public static class AssemblyReference
{
    public static readonly Assembly Assembly =
        typeof(AssemblyReference).Assembly;
}