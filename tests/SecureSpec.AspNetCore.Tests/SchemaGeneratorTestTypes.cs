using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace SecureSpec.AspNetCore.Tests;

internal static class SchemaGeneratorTestTypes
{
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    internal sealed class SimpleClass { }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    internal sealed class AnotherClass { }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    internal sealed class GenericClass<T> { }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    internal sealed class NestedGeneric<TOuter, TInner> { }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    internal sealed class RecursiveEnumerable : IEnumerable<RecursiveEnumerable>
    {
        public IEnumerator<RecursiveEnumerable> GetEnumerator() => throw new NotSupportedException("Enumeration not required for schema generation tests.");

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }

    internal enum NumericEnum
    {
        Zero = 0,
        One = 1,
        Two = 2
    }

    internal enum LongBackedEnum : long
    {
        Small = 1,
        Large = long.MaxValue
    }

    internal enum HugeEnum : ulong
    {
        Small = 1,
        Huge = ulong.MaxValue
    }
}
