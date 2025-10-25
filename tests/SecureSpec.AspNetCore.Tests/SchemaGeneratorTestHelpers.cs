using Microsoft.OpenApi.Models;
using System.Runtime.CompilerServices;

namespace SecureSpec.AspNetCore.Tests;

internal static class SchemaGeneratorTestHelpers
{
    internal static Type CreateNestedListType(Type elementType, int depth)
    {
        var current = elementType;
        for (var i = 0; i < depth; i++)
        {
            current = typeof(List<>).MakeGenericType(current);
        }

        return current;
    }

    internal static OpenApiSchema? FindFirstPlaceholder(OpenApiSchema root)
    {
        var visited = new HashSet<OpenApiSchema>(SchemaReferenceComparer.Instance);
        var queue = new Queue<OpenApiSchema>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }

            if (current.Extensions.TryGetValue("x-securespec-placeholder", out _))
            {
                return current;
            }

            if (current.Items != null)
            {
                queue.Enqueue(current.Items);
            }

            if (current.AdditionalProperties is OpenApiSchema additional)
            {
                queue.Enqueue(additional);
            }

            foreach (var property in current.Properties.Values)
            {
                queue.Enqueue(property);
            }

            foreach (var candidate in current.AllOf)
            {
                queue.Enqueue(candidate);
            }

            foreach (var candidate in current.AnyOf)
            {
                queue.Enqueue(candidate);
            }

            foreach (var candidate in current.OneOf)
            {
                queue.Enqueue(candidate);
            }
        }

        return null;
    }

    private sealed class SchemaReferenceComparer : IEqualityComparer<OpenApiSchema>
    {
        internal static SchemaReferenceComparer Instance { get; } = new();

        public bool Equals(OpenApiSchema? x, OpenApiSchema? y) => ReferenceEquals(x, y);

        public int GetHashCode(OpenApiSchema obj)
        {
            if (obj is null)
            {
                return 0;
            }

            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
