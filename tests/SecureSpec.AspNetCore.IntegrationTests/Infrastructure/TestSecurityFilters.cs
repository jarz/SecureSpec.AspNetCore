using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Filters;

namespace SecureSpec.AspNetCore.IntegrationTests.Infrastructure;

#pragma warning disable CA1812
internal sealed class SecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var requirement = new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "apiKey"
                    }
                },
                SecurityScopeDefaults
            }
        };

        operation.Security.Add(requirement);
    }

    private static readonly string[] SecurityScopeDefaults = new[] { "write", "read" };
}

internal sealed class MetadataOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Extensions["x-operation-id"] = new OpenApiString(context.MethodInfo.Name);
    }
}
#pragma warning restore CA1812
