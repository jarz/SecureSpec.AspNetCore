# Performance Monitoring

## Overview

SecureSpec.AspNetCore includes built-in performance monitoring to ensure document generation meets the targets specified in the PRD (AC 297-300). The system tracks generation times, normalizes them to a baseline operation count, and emits diagnostics when performance thresholds are exceeded.

## Performance Targets

As defined in PRD Section 5, the performance targets are:

| Operation | Target | Degraded | Failure |
|-----------|--------|----------|---------|
| Generation (1000 ops) | <500ms | 500-2000ms | >2000ms |
| Recursive schema traversal | <100ms | 100-500ms | >500ms |
| Hash computation | <50ms | 50-200ms | >200ms |

## Configuration

Performance monitoring is configured through the `PerformanceOptions` class:

```csharp
services.AddSecureSpec(options =>
{
    options.Performance.EnablePerformanceMonitoring = true; // Default: true
    options.Performance.TargetGenerationTimeMs = 500;       // Default: 500ms
    options.Performance.DegradedThresholdMs = 2000;         // Default: 2000ms (also failure threshold)
    options.Performance.BaselineOperationCount = 1000;      // Default: 1000
});
```

### Configuration Options

- **EnablePerformanceMonitoring**: Enable or disable performance tracking. When disabled, no performance metrics are collected or emitted.
- **TargetGenerationTimeMs**: The target generation time in milliseconds. Performance below this threshold is considered optimal (AC 297).
- **DegradedThresholdMs**: The threshold for degraded performance. Times between the target and this threshold trigger a warning (AC 298). Times exceeding this threshold are considered failures (AC 299).
- **BaselineOperationCount**: The number of operations used for normalization. All performance measurements are normalized to this count for consistent comparison (default: 1000 operations).

## Performance Monitor

The `PerformanceMonitor` class tracks generation time and emits appropriate diagnostics:

```csharp
var options = new PerformanceOptions
{
    EnablePerformanceMonitoring = true,
    TargetGenerationTimeMs = 500
};
var logger = new DiagnosticsLogger();

using (var monitor = new PerformanceMonitor(options, logger, "MyOperation", operationCount: 1000))
{
    // Perform the operation
    GenerateDocument();
    
    // Monitor will automatically emit diagnostics on dispose
}
```

### Performance Status

The monitor tracks the following performance states:

- **Target**: Performance met the target threshold (<500ms for 1000 operations)
- **Degraded**: Performance is above target but below failure threshold (500-2000ms)
- **Failure**: Performance exceeded the failure threshold (>2000ms)
- **NotMonitored**: Performance monitoring is disabled

## Diagnostic Codes

The performance monitoring system emits the following diagnostic codes:

### PERF001 - Resource Limit Exceeded
- **Severity**: Warning
- **Description**: A resource limit (time or memory) was exceeded during generation
- **Action**: Review resource configuration
- **Emitted by**: ResourceGuard

### PERF002 - Performance Target Met
- **Severity**: Info
- **Description**: Performance met the target threshold (<500ms for 1000 operations)
- **Action**: None
- **Emitted by**: PerformanceMonitor

### PERF003 - Performance Degraded
- **Severity**: Warning
- **Description**: Performance is degraded (500-2000ms for 1000 operations)
- **Action**: Review performance optimizations
- **Emitted by**: PerformanceMonitor

### PERF004 - Performance Failure
- **Severity**: Error
- **Description**: Performance failure (>2000ms for 1000 operations)
- **Action**: Immediate optimization required
- **Emitted by**: PerformanceMonitor

### PERF005 - Performance Metrics
- **Severity**: Info
- **Description**: Performance metrics collected for analysis
- **Action**: Review performance trends
- **Emitted by**: PerformanceMonitor

## Operation Count Normalization

The performance monitor normalizes all measurements to the baseline operation count (default: 1000 operations). This allows for consistent comparison across different operation counts.

### Example

If you perform 500 operations in 250ms, the normalized time would be:
- **Actual time**: 250ms for 500 operations
- **Normalized time**: 500ms for 1000 operations
- **Status**: Target met (500ms <= 500ms target)

If you perform 2000 operations in 800ms, the normalized time would be:
- **Actual time**: 800ms for 2000 operations
- **Normalized time**: 400ms for 1000 operations
- **Status**: Target met (400ms < 500ms target)

## Integration with Document Generator

The `DocumentGenerator` class automatically integrates performance monitoring:

```csharp
var options = new SecureSpecOptions
{
    Performance = 
    {
        EnablePerformanceMonitoring = true,
        TargetGenerationTimeMs = 500
    }
};
var logger = new DiagnosticsLogger();
var generator = new DocumentGenerator(options, logger);

// Generate with performance monitoring
var document = generator.GenerateWithGuards("MyAPI", () =>
{
    // Your document generation logic
    return myOpenApiDocument;
}, operationCount: 1000);

// Check diagnostics
var events = logger.GetEvents();
var perfEvents = events.Where(e => e.Code.StartsWith("PERF"));
foreach (var evt in perfEvents)
{
    Console.WriteLine($"{evt.Code}: {evt.Message}");
}
```

## Metrics Context

Performance diagnostics include detailed context information:

```json
{
  "Operation": "Document generation: MyAPI",
  "OperationCount": 1000,
  "ElapsedMs": 450,
  "NormalizedMs": 450,
  "BaselineOperations": 1000,
  "TargetMs": 500,
  "DegradedThresholdMs": 2000,
  "Status": "Target"
}
```

## Best Practices

1. **Always enable monitoring in production**: Performance monitoring has minimal overhead and provides valuable insights.

2. **Review degraded performance warnings**: If you see PERF003 warnings, investigate potential optimizations.

3. **Act on performance failures**: PERF004 errors indicate critical performance issues that require immediate attention.

4. **Use normalization for comparison**: When comparing performance across different operation counts, use the normalized metrics.

5. **Adjust thresholds based on your requirements**: The default thresholds are based on the PRD, but you can adjust them based on your specific needs.

6. **Monitor trends over time**: Track PERF005 metrics events to identify performance degradation over time.

## Relationship with Resource Guards

Performance monitoring is separate from resource guards:

- **Resource Guards**: Enforce hard limits on time and memory to prevent runaway generation. When exceeded, they return a fallback document.
- **Performance Monitoring**: Tracks performance against targets and emits diagnostics. Does not prevent generation completion.

Both systems can be enabled independently, but they work best together:
- Resource guards protect against extreme cases
- Performance monitoring tracks normal operation and trends

## Testing

Performance monitoring includes comprehensive test coverage:

```csharp
[Fact]
public void PerformanceMonitor_MeetsTargetThreshold()
{
    var options = new PerformanceOptions
    {
        EnablePerformanceMonitoring = true,
        TargetGenerationTimeMs = 500
    };
    var logger = new DiagnosticsLogger();

    using (var monitor = new PerformanceMonitor(options, logger, "test", 1000))
    {
        // Fast operation
    }

    var events = logger.GetEvents();
    Assert.Contains(events, e => e.Code == DiagnosticCodes.PerformanceTargetMet);
}
```

## Acceptance Criteria Mapping

The performance monitoring system satisfies the following acceptance criteria:

- **AC 297**: Performance target <500ms for 1000 operations ✅
- **AC 298**: Degraded performance detection (500-2000ms) ✅
- **AC 299**: Performance failure detection (>2000ms) ✅
- **AC 300**: Performance monitoring and metrics collection ✅

## See Also

- [Resource Guards](../src/SecureSpec.AspNetCore/Core/ResourceGuard.cs) - Resource limit enforcement
- [Diagnostics](../src/SecureSpec.AspNetCore/Diagnostics/DiagnosticsLogger.cs) - Diagnostic logging system
- [PRD Section 5](PRD.md#5-performance-targets) - Performance targets specification
