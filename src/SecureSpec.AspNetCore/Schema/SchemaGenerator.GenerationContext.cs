using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Schema;

public partial class SchemaGenerator
{
    private sealed class SchemaGenerationContext
    {
        private readonly int _maxDepth;
        private readonly DiagnosticsLogger _logger;
        private readonly Stack<Type> _typeStack = new();
        private readonly HashSet<Type> _inProgress = new();
        private readonly Dictionary<Type, OpenApiSchema> _cyclePlaceholders = new();
        private readonly Dictionary<Type, OpenApiSchema> _depthPlaceholders = new();
        private readonly HashSet<Type> _depthLogged = new();

        private const int MinimumAllowedDepth = 1;

        public SchemaGenerationContext(int maxDepth, DiagnosticsLogger logger)
        {
            _maxDepth = Math.Max(MinimumAllowedDepth, maxDepth);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool TryEnter(Type type, int depth, out OpenApiSchema placeholder)
        {
            if (depth >= _maxDepth)
            {
                placeholder = GetDepthPlaceholder(type);
                return false;
            }

            if (!_inProgress.Add(type))
            {
                placeholder = GetCyclePlaceholder(type);
                return false;
            }

            _typeStack.Push(type);
            placeholder = null!;
            return true;
        }

        public void Exit(Type type)
        {
            if (_typeStack.Count == 0)
            {
                return;
            }

            var popped = _typeStack.Pop();
            if (!ReferenceEquals(popped, type))
            {
                var remainingStack = _typeStack
                    .Select(t => t.FullName ?? t.Name)
                    .ToArray();

                _typeStack.Push(popped);

                var remainingDescription = remainingStack.Length == 0
                    ? "<empty>"
                    : string.Join(" -> ", remainingStack);

                throw new InvalidOperationException(
                    $"Schema generation traversal order corrupted. Expected to exit type '{type.FullName ?? type.Name}' but found '{popped.FullName ?? popped.Name}'. Remaining stack (top to bottom): {remainingDescription}");
            }

            _inProgress.Remove(type);
        }

        private OpenApiSchema GetCyclePlaceholder(Type type)
        {
            if (!_cyclePlaceholders.TryGetValue(type, out var placeholder))
            {
                placeholder = CreatePlaceholder(type, "cycle");
                _cyclePlaceholders[type] = placeholder;
            }

            return placeholder;
        }

        private OpenApiSchema GetDepthPlaceholder(Type type)
        {
            if (!_depthPlaceholders.TryGetValue(type, out var placeholder))
            {
                placeholder = CreatePlaceholder(type, "depth");
                _depthPlaceholders[type] = placeholder;
            }

            if (_depthLogged.Add(type))
            {
                _logger.LogWarning("SCH002", $"Schema generation for type '{type.FullName ?? type.Name}' exceeded maximum depth of {_maxDepth}.");
            }

            return placeholder;
        }
    }
}
