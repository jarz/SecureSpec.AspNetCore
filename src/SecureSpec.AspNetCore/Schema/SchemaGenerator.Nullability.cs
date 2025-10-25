using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;

namespace SecureSpec.AspNetCore.Schema;

public partial class SchemaGenerator
{
    private OpenApiSchema ApplyNullability(OpenApiSchema schema, bool isNullable)
    {
        ArgumentNullException.ThrowIfNull(schema);

        if (!isNullable)
        {
            schema.Nullable = false;
            return schema;
        }

        if (IsPlaceholder(schema))
        {
            if (_options.SpecVersion == SchemaSpecVersion.OpenApi3_0)
            {
                return new OpenApiSchema
                {
                    Nullable = true,
                    AllOf =
                    {
                        schema
                    }
                };
            }

            return new OpenApiSchema
            {
                AnyOf =
                {
                    schema,
                    CreateNullSchema()
                }
            };
        }

        if (_options.SpecVersion == SchemaSpecVersion.OpenApi3_0)
        {
            schema.Nullable = true;
            return schema;
        }

        schema.Nullable = false;
        return CreateNullUnion(schema);
    }

    private static OpenApiSchema CreateNullUnion(OpenApiSchema schema)
    {
        if (schema.OneOf.Count > 0)
        {
            if (!ContainsNullSchema(schema.OneOf))
            {
                schema.OneOf.Add(CreateNullSchema());
            }

            return schema;
        }

        if (schema.AnyOf.Count > 0)
        {
            if (!ContainsNullSchema(schema.AnyOf))
            {
                schema.AnyOf.Add(CreateNullSchema());
            }

            return schema;
        }

        if (schema.AllOf.Count > 0)
        {
            return new OpenApiSchema
            {
                AnyOf =
                {
                    schema,
                    CreateNullSchema()
                }
            };
        }

        return new OpenApiSchema
        {
            AnyOf =
            {
                schema,
                CreateNullSchema()
            }
        };
    }

    private static bool ContainsNullSchema(IList<OpenApiSchema> candidates)
    {
        for (var i = 0; i < candidates.Count; i++)
        {
            if (IsNullSchema(candidates[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNullSchema(OpenApiSchema candidate)
    {
        return string.Equals(candidate.Type, "null", StringComparison.Ordinal)
               && candidate.AnyOf.Count == 0
               && candidate.OneOf.Count == 0
               && candidate.AllOf.Count == 0
               && candidate.Properties.Count == 0
               && candidate.Items == null
               && candidate.AdditionalProperties == null;
    }

    private static OpenApiSchema CreateNullSchema()
    {
        return new OpenApiSchema { Type = "null" };
    }

    private static OpenApiSchema CreatePlaceholder(Type type, string kind)
    {
        var typeName = type.FullName ?? type.Name;
        var metadata = new OpenApiObject
        {
            ["kind"] = new OpenApiString(kind),
            ["type"] = new OpenApiString(typeName)
        };

        var description = kind switch
        {
            "cycle" => $"Cycle detected for type '{typeName}'.",
            "depth" => $"Depth limit reached for type '{typeName}'.",
            _ => $"Schema placeholder for type '{typeName}'."
        };

        var placeholder = new OpenApiSchema
        {
            Type = "object",
            Description = description
        };

        placeholder.Extensions[PlaceholderExtensionKey] = metadata;
        return placeholder;
    }

    private static bool IsPlaceholder(OpenApiSchema schema)
    {
        return schema.Extensions.ContainsKey(PlaceholderExtensionKey);
    }
}
