using System.Diagnostics.CodeAnalysis;
using Zerra.Reflection;

#pragma warning disable CS0414 // Never used

namespace Zerra.Repository
{
    [GenerateTypeDetail]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllFields)]
    internal static class TypesToGenerate
    {
        public static readonly object? Object = null;
        public static readonly List<object>? ObjectList = null;
        public static readonly HashSet<object>? HashSetList = null;
    }
}
