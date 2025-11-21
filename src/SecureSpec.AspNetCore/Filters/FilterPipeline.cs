#pragma warning disable CA1031 // Do not catch general exception types - intentional for filter isolation

using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Diagnostics;

namespace SecureSpec.AspNetCore.Filters;

/// <summary>
/// Executes filters in the correct order according to the PRD (Section 23).
/// Order: Schema → Operation → Parameter → RequestBody → Document → PreSerialize
/// </summary>
public class FilterPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FilterCollection _filters;
    private readonly DiagnosticsLogger _diagnosticsLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterPipeline"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving filter instances.</param>
    /// <param name="filters">Collection of registered filters.</param>
    /// <param name="diagnosticsLogger">Logger for diagnostic events.</param>
    public FilterPipeline(
        IServiceProvider serviceProvider,
        FilterCollection filters,
        DiagnosticsLogger diagnosticsLogger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _filters = filters ?? throw new ArgumentNullException(nameof(filters));
        _diagnosticsLogger = diagnosticsLogger ?? throw new ArgumentNullException(nameof(diagnosticsLogger));
    }

    /// <summary>
    /// Applies schema filters to a schema.
    /// </summary>
    /// <param name="schema">The schema to modify.</param>
    /// <param name="context">Filter context.</param>
    public void ApplySchemaFilters(OpenApiSchema schema, SchemaFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var filterType in _filters.SchemaFilters)
        {
            try
            {
                var filter = (ISchemaFilter)_serviceProvider.GetRequiredService(filterType);
                filter.Apply(schema, context);
            }
            catch (Exception ex)
            {
                _diagnosticsLogger.LogError(
                    DiagnosticCodes.FilterExecutionCompleted,
                    $"Schema filter {filterType.Name} failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Applies operation filters to an operation.
    /// </summary>
    /// <param name="operation">The operation to modify.</param>
    /// <param name="context">Filter context.</param>
    public void ApplyOperationFilters(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var filterType in _filters.OperationFilters)
        {
            try
            {
                var filter = (IOperationFilter)_serviceProvider.GetRequiredService(filterType);
                filter.Apply(operation, context);
            }
            catch (Exception ex)
            {
                _diagnosticsLogger.LogError(
                    DiagnosticCodes.FilterExecutionCompleted,
                    $"Operation filter {filterType.Name} failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Applies parameter filters to a parameter.
    /// </summary>
    /// <param name="parameter">The parameter to modify.</param>
    /// <param name="context">Filter context.</param>
    public void ApplyParameterFilters(OpenApiParameter parameter, ParameterFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var filterType in _filters.ParameterFilters)
        {
            try
            {
                var filter = (IParameterFilter)_serviceProvider.GetRequiredService(filterType);
                filter.Apply(parameter, context);
            }
            catch (Exception ex)
            {
                _diagnosticsLogger.LogError(
                    DiagnosticCodes.FilterExecutionCompleted,
                    $"Parameter filter {filterType.Name} failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Applies request body filters to a request body.
    /// </summary>
    /// <param name="requestBody">The request body to modify.</param>
    /// <param name="context">Filter context.</param>
    public void ApplyRequestBodyFilters(OpenApiRequestBody requestBody, RequestBodyFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(requestBody);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var filterType in _filters.RequestBodyFilters)
        {
            try
            {
                var filter = (IRequestBodyFilter)_serviceProvider.GetRequiredService(filterType);
                filter.Apply(requestBody, context);
            }
            catch (Exception ex)
            {
                _diagnosticsLogger.LogError(
                    DiagnosticCodes.FilterExecutionCompleted,
                    $"Request body filter {filterType.Name} failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Applies document filters to a document.
    /// </summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="context">Filter context.</param>
    public void ApplyDocumentFilters(OpenApiDocument document, DocumentFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var filterType in _filters.DocumentFilters)
        {
            try
            {
                var filter = (IDocumentFilter)_serviceProvider.GetRequiredService(filterType);
                filter.Apply(document, context);
            }
            catch (Exception ex)
            {
                _diagnosticsLogger.LogError(
                    DiagnosticCodes.FilterExecutionCompleted,
                    $"Document filter {filterType.Name} failed: {ex.Message}");
            }
        }

        _diagnosticsLogger.LogInfo(
            DiagnosticCodes.FilterExecutionCompleted,
            $"Applied {_filters.DocumentFilters.Count} document filters to document '{context.DocumentName}'.");
    }

    /// <summary>
    /// Applies pre-serialize filters to a document (final mutation stage).
    /// </summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="context">Filter context.</param>
    public void ApplyPreSerializeFilters(OpenApiDocument document, PreSerializeFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var filterType in _filters.PreSerializeFilters)
        {
            try
            {
                var filter = (IPreSerializeFilter)_serviceProvider.GetRequiredService(filterType);
                filter.Apply(document, context);
            }
            catch (Exception ex)
            {
                _diagnosticsLogger.LogError(
                    DiagnosticCodes.FilterExecutionCompleted,
                    $"Pre-serialize filter {filterType.Name} failed: {ex.Message}");
            }
        }

        _diagnosticsLogger.LogInfo(
            DiagnosticCodes.FilterExecutionCompleted,
            $"Applied {_filters.PreSerializeFilters.Count} pre-serialize filters to document '{context.DocumentName}'.");
    }
}
