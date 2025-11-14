namespace SecureSpec.AspNetCore.Filters;

/// <summary>
/// Collection for managing OpenAPI filters.
/// </summary>
public class FilterCollection
{
    private readonly List<Type> _schemaFilters = new();
    private readonly List<Type> _operationFilters = new();
    private readonly List<Type> _parameterFilters = new();
    private readonly List<Type> _requestBodyFilters = new();
    private readonly List<Type> _documentFilters = new();
    private readonly List<Type> _preSerializeFilters = new();

    /// <summary>
    /// Gets the registered schema filter types.
    /// </summary>
    public IReadOnlyList<Type> SchemaFilters => _schemaFilters.AsReadOnly();

    /// <summary>
    /// Gets the registered operation filter types.
    /// </summary>
    public IReadOnlyList<Type> OperationFilters => _operationFilters.AsReadOnly();

    /// <summary>
    /// Gets the registered parameter filter types.
    /// </summary>
    public IReadOnlyList<Type> ParameterFilters => _parameterFilters.AsReadOnly();

    /// <summary>
    /// Gets the registered request body filter types.
    /// </summary>
    public IReadOnlyList<Type> RequestBodyFilters => _requestBodyFilters.AsReadOnly();

    /// <summary>
    /// Gets the registered document filter types.
    /// </summary>
    public IReadOnlyList<Type> DocumentFilters => _documentFilters.AsReadOnly();

    /// <summary>
    /// Gets the registered pre-serialize filter types.
    /// </summary>
    public IReadOnlyList<Type> PreSerializeFilters => _preSerializeFilters.AsReadOnly();

    /// <summary>
    /// Adds a schema filter.
    /// </summary>
    /// <typeparam name="T">The filter type implementing <see cref="ISchemaFilter"/>.</typeparam>
    /// <returns>This collection for method chaining.</returns>
    public FilterCollection AddSchemaFilter<T>() where T : ISchemaFilter
    {
        _schemaFilters.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// Adds a schema filter by type.
    /// </summary>
    /// <param name="filterType">The filter type implementing <see cref="ISchemaFilter"/>.</param>
    /// <returns>This collection for method chaining.</returns>
    public FilterCollection AddSchemaFilter(Type filterType)
    {
        ArgumentNullException.ThrowIfNull(filterType);

        if (!typeof(ISchemaFilter).IsAssignableFrom(filterType))
        {
            throw new ArgumentException($"Type {filterType.Name} must implement ISchemaFilter", nameof(filterType));
        }

        _schemaFilters.Add(filterType);
        return this;
    }

    /// <summary>
    /// Adds an operation filter.
    /// </summary>
    /// <typeparam name="T">The filter type implementing <see cref="IOperationFilter"/>.</typeparam>
    /// <returns>This collection for method chaining.</returns>
    public FilterCollection AddOperationFilter<T>() where T : IOperationFilter
    {
        _operationFilters.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// Adds an operation filter by type.
    /// </summary>
    /// <param name="filterType">The filter type implementing <see cref="IOperationFilter"/>.</param>
    /// <returns>This collection for method chaining.</returns>
    public FilterCollection AddOperationFilter(Type filterType)
    {
        ArgumentNullException.ThrowIfNull(filterType);

        if (!typeof(IOperationFilter).IsAssignableFrom(filterType))
        {
            throw new ArgumentException($"Type {filterType.Name} must implement IOperationFilter", nameof(filterType));
        }

        _operationFilters.Add(filterType);
        return this;
    }

    /// <summary>
    /// Adds a parameter filter.
    /// </summary>
    /// <typeparam name="T">The filter type implementing <see cref="IParameterFilter"/>.</typeparam>
    /// <returns>This collection for method chaining.</returns>
    public FilterCollection AddParameterFilter<T>() where T : IParameterFilter
    {
        _parameterFilters.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// Adds a request body filter.
    /// </summary>
    /// <typeparam name="T">The filter type implementing <see cref="IRequestBodyFilter"/>.</typeparam>
    /// <returns>This collection for method chaining.</returns>
    public FilterCollection AddRequestBodyFilter<T>() where T : IRequestBodyFilter
    {
        _requestBodyFilters.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// Adds a document filter.
    /// </summary>
    /// <typeparam name="T">The filter type implementing <see cref="IDocumentFilter"/>.</typeparam>
    /// <returns>This collection for method chaining.</returns>
    public FilterCollection AddDocumentFilter<T>() where T : IDocumentFilter
    {
        _documentFilters.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// Adds a pre-serialize filter.
    /// </summary>
    /// <typeparam name="T">The filter type implementing <see cref="IPreSerializeFilter"/>.</typeparam>
    /// <returns>This collection for method chaining.</returns>
    public FilterCollection AddPreSerializeFilter<T>() where T : IPreSerializeFilter
    {
        _preSerializeFilters.Add(typeof(T));
        return this;
    }
}
