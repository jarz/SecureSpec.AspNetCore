# Testing Strategy

This document outlines the comprehensive testing strategy for SecureSpec.AspNetCore.

## Table of Contents

- [Testing Philosophy](#testing-philosophy)
- [Test Categories](#test-categories)
- [Coverage Targets](#coverage-targets)
- [Test Patterns](#test-patterns)
- [Security Testing](#security-testing)
- [Performance Testing](#performance-testing)
- [Test Infrastructure](#test-infrastructure)

## Testing Philosophy

### Principles

1. **Test Pyramid**: More unit tests, fewer integration tests, even fewer E2E tests
2. **Fast Feedback**: Unit tests run in <5 seconds total
3. **Isolated**: Tests don't depend on external services
4. **Deterministic**: Same input always produces same output
5. **Comprehensive**: Cover happy paths, edge cases, and failure modes

### Test-Driven Development

For critical security features:
1. Write failing test
2. Implement minimal code to pass
3. Refactor
4. Add edge case tests

## Test Categories

### 1. Unit Tests

**Scope**: Single class or method in isolation

**Characteristics**:
- Fast (<1ms per test)
- No external dependencies
- Use mocks/stubs for dependencies
- Test one thing per test

**Example**:
```csharp
[Fact]
public void SchemaIdGenerator_WhenCollisionOccurs_AppendsNumericSuffix()
{
    // Arrange
    var generator = new SchemaIdGenerator();
    generator.Register(typeof(Product), "Product");
    
    // Act
    var secondId = generator.GetId(typeof(ProductV2), "Product");
    
    // Assert
    Assert.Equal("Product_schemaDup1", secondId);
}
```

**Coverage Target**: 95% for critical components

### 2. Integration Tests

**Scope**: Multiple components working together

**Characteristics**:
- Moderate speed (<100ms per test)
- Real instances, minimal mocking
- Test component interactions
- Use in-memory services where possible

**Example**:
```csharp
[Fact]
public async Task DocumentGenerator_WithComplexSchema_GeneratesDeterministicOutput()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddSecureSpec(options =>
    {
        options.Documents.Add("v1", doc =>
        {
            doc.Info.Title = "Test API";
        });
    });
    var provider = services.BuildServiceProvider();
    var generator = provider.GetRequiredService<IDocumentGenerator>();
    
    // Act
    var doc1 = await generator.GenerateAsync("v1");
    var doc2 = await generator.GenerateAsync("v1");
    
    // Assert
    var hash1 = ComputeHash(doc1);
    var hash2 = ComputeHash(doc2);
    Assert.Equal(hash1, hash2); // Deterministic
}
```

**Coverage Target**: 90% for integration paths

### 3. End-to-End Tests

**Scope**: Complete workflows from HTTP request to response

**Characteristics**:
- Slower (<1s per test)
- Real HTTP requests via TestServer
- Test complete scenarios
- Validate HTTP headers, status codes

**Example**:
```csharp
[Fact]
public async Task SecureSpecUI_ServesWith_StrictCSPHeaders()
{
    // Arrange
    var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/securespec");
    
    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var csp = response.Headers.GetValues("Content-Security-Policy").First();
    Assert.Contains("default-src 'none'", csp);
    Assert.Contains("script-src 'nonce-", csp);
}
```

**Coverage Target**: 85% for critical user flows

### 4. Acceptance Tests

**Scope**: Verify acceptance criteria from PRD

**Characteristics**:
- Map to specific AC numbers
- High-level behavior validation
- May combine unit/integration/E2E
- Document AC coverage

**Example**:
```csharp
[Fact]
[AcceptanceCriteria("AC-401")]
public void SchemaId_ForGenericTypes_UsesDeterministicNaming()
{
    // Test AC-401: SchemaId generic naming deterministic
    var generator = new SchemaIdGenerator();
    
    var id1 = generator.GetId(typeof(List<Product>));
    var id2 = generator.GetId(typeof(List<Product>));
    
    Assert.Equal("List«Product»", id1);
    Assert.Equal(id1, id2); // Deterministic
}
```

**Coverage Target**: 100% of AC 1-500

## Coverage Targets

### By Component

| Component | Unit | Integration | E2E | AC |
|-----------|------|-------------|-----|-----|
| Schema Generation | 95% | 90% | 85% | 100% |
| Security Layer | 100% | 95% | 90% | 100% |
| UI | 85% | 85% | 90% | 100% |
| Rate Limiting | 90% | 95% | 85% | 100% |
| Virtualization | 90% | 90% | 85% | 100% |
| Serialization | 95% | 90% | N/A | 100% |
| Caching | 90% | 90% | 80% | 100% |
| Diagnostics | 85% | 85% | 80% | 100% |
| Accessibility | 80% | 80% | 90% | 100% |

### Overall Targets

- **Unit Test Coverage**: 90%+ overall
- **Acceptance Criteria**: 100% (all AC 1-500 tested)
- **Critical Security Paths**: 100%
- **Happy Paths**: 100%
- **Error Paths**: 90%+

## Test Patterns

### Arrange-Act-Assert (AAA)

All tests follow the AAA pattern:

```csharp
[Fact]
public void MethodName_Scenario_ExpectedResult()
{
    // Arrange - Set up test data and dependencies
    var input = new TestData();
    var sut = new SystemUnderTest();
    
    // Act - Execute the code being tested
    var result = sut.Method(input);
    
    // Assert - Verify the expected outcome
    Assert.Equal(expected, result);
}
```

### Test Naming

Format: `MethodName_Scenario_ExpectedResult`

```csharp
// ✅ Good
[Fact]
public void GetSchemaId_WhenTypeHasCollision_ReturnsSuffixedName()

[Fact]
public void Serialize_WithComplexObject_ProducesDeterministicOutput()

// ❌ Bad
[Fact]
public void Test1()  // Not descriptive

[Fact]
public void GetSchemaId()  // No scenario or expected result
```

### Test Data Builders

Use builder pattern for complex test data:

```csharp
public class DocumentBuilder
{
    private string _title = "Default Title";
    private string _version = "1.0";
    
    public DocumentBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }
    
    public DocumentBuilder WithVersion(string version)
    {
        _version = version;
        return this;
    }
    
    public OpenApiDocument Build()
    {
        return new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = _title,
                Version = _version
            }
        };
    }
}

// Usage
var doc = new DocumentBuilder()
    .WithTitle("Test API")
    .WithVersion("2.0")
    .Build();
```

### Theory Tests for Multiple Inputs

```csharp
[Theory]
[InlineData(typeof(int), "integer", "int32")]
[InlineData(typeof(long), "integer", "int64")]
[InlineData(typeof(string), "string", null)]
[InlineData(typeof(Guid), "string", "uuid")]
public void TypeMapper_MapsClrType_ToOpenApiType(
    Type clrType, string expectedType, string expectedFormat)
{
    var mapper = new TypeMapper();
    var schema = mapper.Map(clrType);
    
    Assert.Equal(expectedType, schema.Type);
    Assert.Equal(expectedFormat, schema.Format);
}
```

## Security Testing

### Security Test Categories

1. **Input Validation**
   - Boundary testing
   - Invalid input handling
   - Size limits
   - Special characters

2. **Authentication/Authorization**
   - CSRF protection
   - OAuth flow security
   - Token validation

3. **Injection Prevention**
   - XSS prevention
   - CRLF injection
   - Header injection

4. **Resource Protection**
   - Rate limiting
   - Resource exhaustion
   - DoS prevention

### Example Security Tests

```csharp
[Theory]
[InlineData("Header\r\nInjected: value")]  // CRLF
[InlineData("<script>alert('xss')</script>")]  // XSS
[InlineData("../../../etc/passwd")]  // Path traversal
public void InputSanitizer_RemovesInvalidCharacters(string maliciousInput)
{
    var sanitizer = new InputSanitizer();
    var result = sanitizer.Sanitize(maliciousInput);
    
    Assert.DoesNotContain("\r", result);
    Assert.DoesNotContain("\n", result);
    Assert.DoesNotContain("<script", result, 
        StringComparison.OrdinalIgnoreCase);
}

[Fact]
public async Task TryItOut_WhenNetworkAttempted_BlocksWithAbortCode()
{
    var sandbox = new WasmSandbox();
    var request = new RequestBuilder()
        .WithUrl("http://internal.network/secret")
        .Build();
    
    var exception = await Assert.ThrowsAsync<SandboxViolationException>(
        () => sandbox.ExecuteAsync(request));
    
    Assert.Equal("API_VIOLATION", exception.AbortCode);
}

[Fact]
public void CSP_HeadersAre_Strict()
{
    var csp = CSPHeaderGenerator.Generate();
    
    Assert.Contains("default-src 'none'", csp);
    Assert.DoesNotContain("unsafe-eval", csp);
    Assert.DoesNotContain("unsafe-inline", csp);
}
```

## Performance Testing

### Performance Test Types

1. **Latency Tests**: Measure response time
2. **Throughput Tests**: Measure requests per second
3. **Resource Usage**: Memory and CPU
4. **Scalability Tests**: Behavior under load

### Example Performance Tests

```csharp
[Fact]
public async Task DocumentGeneration_With1000Operations_CompletesUnder500ms()
{
    // Arrange
    var generator = CreateGeneratorWith1000Operations();
    var stopwatch = Stopwatch.StartNew();
    
    // Act
    var document = await generator.GenerateAsync("v1");
    stopwatch.Stop();
    
    // Assert
    Assert.True(stopwatch.ElapsedMilliseconds < 500, 
        $"Generation took {stopwatch.ElapsedMilliseconds}ms, " +
        "expected <500ms (AC 297)");
}

[Fact]
public void Virtualization_TriggersAt_200Properties()
{
    var schema = new SchemaBuilder()
        .WithProperties(201)
        .Build();
    
    var generator = new SchemaGenerator();
    var result = generator.Generate(schema);
    
    Assert.True(result.IsVirtualized);
    Assert.Contains("VIRT001", result.Diagnostics);
}
```

### Load Testing

Use tools like NBomber or k6 for load testing:

```csharp
var scenario = Scenario.Create("document_generation", async context =>
{
    var response = await Http.CreateRequest("GET", 
        "https://localhost:5001/securespec/v1.json")
        .WithHeader("Accept", "application/json")
        .Send(context);
    
    return response;
})
.WithLoadSimulations(
    Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromSeconds(30))
);
```

## Test Infrastructure

### Test Fixtures

For expensive setup:

```csharp
public class DocumentGeneratorFixture : IDisposable
{
    public DocumentGeneratorFixture()
    {
        // Expensive setup once
        Generator = CreateGenerator();
    }
    
    public IDocumentGenerator Generator { get; }
    
    public void Dispose()
    {
        // Cleanup
    }
}

public class DocumentTests : IClassFixture<DocumentGeneratorFixture>
{
    private readonly IDocumentGenerator _generator;
    
    public DocumentTests(DocumentGeneratorFixture fixture)
    {
        _generator = fixture.Generator;
    }
    
    [Fact]
    public void Test1() { /* Use _generator */ }
}
```

### Test Helpers

Common test utilities:

```csharp
public static class TestHelpers
{
    public static string ComputeHash(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
    
    public static WebApplicationFactory<T> CreateTestFactory<T>() 
        where T : class
    {
        return new WebApplicationFactory<T>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Test-specific configuration
                });
            });
    }
}
```

### Mocking Strategy

Use Moq for mocking dependencies:

```csharp
[Fact]
public async Task DocumentBuilder_CallsSchemaGenerator_ForEachType()
{
    // Arrange
    var mockGenerator = new Mock<ISchemaGenerator>();
    mockGenerator
        .Setup(g => g.GenerateSchema(It.IsAny<Type>()))
        .Returns(new OpenApiSchema());
    
    var builder = new DocumentBuilder(mockGenerator.Object);
    
    // Act
    await builder.BuildAsync();
    
    // Assert
    mockGenerator.Verify(g => 
        g.GenerateSchema(typeof(Product)), Times.Once);
}
```

## Continuous Integration

### CI Pipeline

```yaml
# .github/workflows/test.yml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore
      
      - name: Unit Tests
        run: dotnet test --no-build --filter "Category=Unit"
      
      - name: Integration Tests
        run: dotnet test --no-build --filter "Category=Integration"
      
      - name: E2E Tests
        run: dotnet test --no-build --filter "Category=E2E"
      
      - name: Code Coverage
        run: |
          dotnet test --no-build --collect:"XPlat Code Coverage"
          dotnet tool install -g dotnet-reportgenerator-globaltool
          reportgenerator -reports:**/coverage.cobertura.xml \
            -targetdir:coverage -reporttypes:Html
      
      - name: Upload Coverage
        uses: codecov/codecov-action@v3
```

## Test Reporting

### Coverage Reports

- Use Coverlet for coverage collection
- Generate HTML reports with ReportGenerator
- Upload to Codecov or similar service

### Acceptance Criteria Tracking

```csharp
// Custom attribute to link tests to AC
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class AcceptanceCriteriaAttribute : Attribute
{
    public AcceptanceCriteriaAttribute(string criteriaId)
    {
        CriteriaId = criteriaId;
    }
    
    public string CriteriaId { get; }
}

// Report generator can extract these to show AC coverage
```

## References

- [PRD.md](PRD.md) - All AC 1-500 that must be tested
- [ARCHITECTURE.md](../ARCHITECTURE.md) - Components to test
- [THREAT_MODEL.md](THREAT_MODEL.md) - Security tests needed

---

**Last Updated**: 2025-10-19  
**Version**: 1.0
