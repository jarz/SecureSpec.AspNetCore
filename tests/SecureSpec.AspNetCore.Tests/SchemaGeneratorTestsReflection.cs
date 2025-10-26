using System.Reflection;
using SecureSpec.AspNetCore.Schema;

namespace SecureSpec.AspNetCore.Tests;

internal static class SchemaGeneratorTestsReflection
{
    internal static MethodInfo GetApplyNullabilityMethod() =>
        typeof(SchemaGenerator)
            .GetMethod("ApplyNullability", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("ApplyNullability method not found.");
}
