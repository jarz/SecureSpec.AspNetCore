# Design Decisions

This document captures key design decisions made during the SecureSpec.AspNetCore project, along with the rationale, alternatives considered, and implications.

## Table of Contents

- [Architectural Decisions](#architectural-decisions)
- [Security Decisions](#security-decisions)
- [Performance Decisions](#performance-decisions)
- [API Design Decisions](#api-design-decisions)
- [Technology Decisions](#technology-decisions)

## Architectural Decisions

### AD-001: Monolithic vs Modular Architecture

**Decision**: Modular monolith with clear component boundaries

**Rationale**:
- Single deployment unit simplifies distribution
- Clear internal boundaries enable future extraction if needed
- Reduced operational complexity for users
- Better performance without inter-service calls

**Alternatives Considered**:
- Microservices: Rejected due to deployment complexity
- Pure monolith: Rejected due to poor testability

**Implications**:
- Components must have well-defined interfaces
- Dependency injection manages component lifecycle
- Testing requires proper mocking strategies

**Status**: Approved  
**Date**: 2025-10-19

---

### AD-002: Canonical Serialization Approach

**Decision**: Implement custom canonical serialization with deterministic ordering

**Rationale**:
- Enables stable hashes across builds (reproducibility)
- Supports integrity verification (SHA256)
- Required for ETag generation
- Eliminates timing attack vectors from non-deterministic ordering

**Alternatives Considered**:
- Use standard serializers: Cannot guarantee determinism
- Order by insertion: Not reproducible across builds
- Hash of unordered structure: Loses human readability

**Implications**:
- Custom serialization code required
- Performance overhead for sorting
- Must maintain compatibility with JSON/YAML standards
- Lexical ordering of all object keys

**Trade-offs**:
- **Gained**: Determinism, security, reproducibility
- **Lost**: Some serialization performance
- **Accepted Risk**: Slightly slower than native serializers

**Status**: Approved  
**Date**: 2025-10-19

---

### AD-003: Filter Pipeline Execution Order

**Decision**: Fixed execution order: Schema → Operation → Parameter → RequestBody → Document → PreSerialize

**Rationale**:
- Predictable behavior for developers
- Schema modifications propagate to operations
- PreSerialize as final mutation point is clear boundary
- Aligns with OpenAPI document structure

**Alternatives Considered**:
- Configurable order: Too complex, hard to reason about
- Parallel execution: Race conditions, unpredictable results
- Single filter type: Too limiting for extensibility

**Implications**:
- Filters must be idempotent
- Later filters see earlier filter changes
- Documentation must clearly explain ordering

**Status**: Approved  
**Date**: 2025-10-19

---

## Security Decisions

### SD-001: WASM Sandbox for Request Execution

**Decision**: Execute Try It Out requests in WASM sandbox with no network/DOM access

**Rationale**:
- Prevents SSRF attacks
- Blocks XSS vectors
- Isolates execution from host environment
- Provides resource limits (memory, CPU)

**Alternatives Considered**:
- JavaScript isolation: Insufficient isolation guarantees
- Server-side proxy: Creates SSRF vector
- No isolation: Unacceptable security risk

**Implications**:
- WASM runtime required
- Additional complexity for request building
- Performance overhead acceptable for security gain
- Abort codes needed for debugging

**Trade-offs**:
- **Gained**: Strong isolation, SSRF prevention
- **Lost**: Some execution flexibility
- **Accepted Risk**: WASM runtime bugs (mitigated by resource limits)

**Security Controls**:
- Memory limit enforcement
- CPU timeout watchdog
- API surface restriction
- Abort reason logging

**Status**: Approved  
**Date**: 2025-10-19

---

### SD-002: No Implicit/Password OAuth Flows

**Decision**: Omit support for OAuth Implicit and Password flows (OMIT-SECURE)

**Rationale**:
- Implicit flow: Deprecated by OAuth 2.1, insecure by design
- Password flow: Exposes credentials to client, anti-pattern
- PKCE required for Authorization Code: Industry best practice

**Alternatives Considered**:
- Support with warnings: Users would still use insecure flows
- Configuration flag: Complexity without security benefit

**Implications**:
- Breaking change from Swashbuckle
- Migration guide required
- May require OAuth server updates for some users

**Security Impact**:
- Eliminates token exposure in browser history
- Prevents credential exposure to applications
- Enforces modern security standards

**Status**: Approved  
**Date**: 2025-10-19

---

### SD-003: Strict CSP with Nonce-Based Scripts

**Decision**: Enforce strict CSP with per-request nonces, no unsafe-eval or unsafe-inline

**Rationale**:
- Prevents XSS attacks
- Blocks unauthorized script execution
- Industry best practice
- Required for high-security environments

**Alternatives Considered**:
- Relaxed CSP: Unacceptable security posture
- Hash-based CSP: Doesn't protect against dynamic content
- No CSP: Not an option for security-focused project

**Implications**:
- All scripts must use nonce attribute
- Inline styles require careful handling
- Some browser features may be limited
- Testing requires CSP-aware tools

**CSP Policy**:
```
default-src 'none';
script-src 'nonce-{nonce}';
style-src 'self' 'unsafe-inline';
img-src 'self' data:;
font-src 'self';
connect-src 'self';
frame-ancestors 'none';
```

**Status**: Approved  
**Date**: 2025-10-19

---

### SD-004: SHA256 + SRI for Integrity

**Decision**: Use SHA256 hashing with Subresource Integrity (SRI) for all assets

**Rationale**:
- Detects tampering
- Prevents CDN compromise
- Enables fail-closed integrity checks
- Supports optional signatures

**Alternatives Considered**:
- MD5/SHA1: Cryptographically weak
- No integrity checks: Unacceptable for security
- Signatures only: Too complex for simple use cases

**Implications**:
- Hash generation during build
- SRI attributes on all script/link tags
- Cache invalidation on hash change
- Optional signature verification

**Status**: Approved  
**Date**: 2025-10-19

---

## Performance Decisions

### PD-001: Virtualization for Large Schemas

**Decision**: Virtualize schemas with >200 properties or >50 nested objects

**Rationale**:
- Prevents browser performance issues
- Maintains responsiveness for large APIs
- Balances completeness with usability

**Alternatives Considered**:
- No virtualization: Poor UX for large schemas
- Different thresholds: 200/50 based on testing
- Always virtualize: Unnecessary complexity for small APIs

**Implications**:
- Lazy rendering implementation required
- Placeholder UI elements
- Search across virtualized content
- VIRT001 diagnostic emitted

**Performance Targets**:
- Initial render: <1s
- Virtualized section load: <200ms
- Search across virtualized: <50ms

**Status**: Approved  
**Date**: 2025-10-19

---

### PD-002: Three-Tier Caching Strategy

**Decision**: Implement L1 (document), L2 (schema), L3 (HTTP) caching

**Rationale**:
- L1: Fast in-memory access
- L2: Reuse schemas across documents
- L3: Reduce server load via browser/CDN

**Alternatives Considered**:
- Single-tier: Insufficient performance
- Distributed cache: Unnecessary complexity
- No caching: Unacceptable performance

**Implications**:
- RW locks for L1 thread safety
- Immutable L2 after startup
- ETag/Cache-Control for L3
- Invalidation strategy required

**Status**: Approved  
**Date**: 2025-10-19

---

### PD-003: Example Generation Throttling

**Decision**: 25ms time budget per example, truncate on exceed

**Rationale**:
- Prevents DoS via complex schema examples
- Maintains responsive generation
- Balances completeness with performance

**Alternatives Considered**:
- No throttling: DoS vector
- Smaller budget: Too restrictive
- Larger budget: Performance impact

**Implications**:
- Atomic counter for thread safety
- EXM001 diagnostic on throttle
- Truncated example structure
- Explicit examples recommended

**Status**: Approved  
**Date**: 2025-10-19

---

## API Design Decisions

### APD-001: Configuration API Fluent Interface

**Decision**: Use fluent builder pattern for configuration

**Rationale**:
- Familiar to ASP.NET Core developers
- IntelliSense-friendly
- Type-safe configuration
- Chainable for readability

**Example**:
```csharp
builder.Services.AddSecureSpec(options =>
{
    options.Documents.Add("v1", doc =>
    {
        doc.Info.Title = "My API";
        doc.Info.Version = "1.0";
    });
    
    options.Security.OAuth.AuthorizationCode(oauth =>
    {
        oauth.AuthorizationUrl = new Uri("https://auth.example.com/authorize", UriKind.Absolute);
        oauth.TokenUrl = new Uri("https://auth.example.com/token", UriKind.Absolute);
        oauth.RequirePKCE = true; // Always required
    });
});
```

**Alternatives Considered**:
- JSON configuration: Less type-safe
- Attribute-based: Too limited
- Separate builder classes: More complex

**Status**: Approved  
**Date**: 2025-10-19

---

### APD-002: SchemaId Collision Suffix Pattern

**Decision**: Use `_schemaDup{N}` suffix for collisions, deterministic numbering

**Rationale**:
- Clear indication of duplicate
- Deterministic across builds
- Human-readable
- Doesn't interfere with typical type names

**Alternatives Considered**:
- GUID suffix: Not deterministic
- Hash suffix: Less readable
- Throw error: Too restrictive

**Implications**:
- Collision detection algorithm required
- SCH001 diagnostic emitted
- Suffix reclaimed when type removed
- Stable ordering by SchemaId

**Status**: Approved  
**Date**: 2025-10-19

---

## Technology Decisions

### TD-001: .NET 8.0 as Minimum Version

**Decision**: Target .NET 8.0 as minimum supported version

**Rationale**:
- Long-term support (LTS)
- Modern C# features (nullable reference types, required members)
- Performance improvements
- Security updates

**Alternatives Considered**:
- .NET 6.0: Older LTS, missing features
- .NET 9.0: Too new, reduced compatibility

**Implications**:
- Users must be on .NET 8.0+
- Can use latest language features
- Better performance than older versions

**Status**: Approved  
**Date**: 2025-10-19

---

### TD-002: System.Text.Json for Serialization

**Decision**: Use System.Text.Json with custom converters for canonical mode

**Rationale**:
- Built into .NET, no external dependency
- High performance
- Customizable via converters
- Microsoft-supported

**Alternatives Considered**:
- Newtonsoft.Json: External dependency, slower
- Custom JSON writer: Too much work

**Implications**:
- Custom converter for deterministic ordering
- May need workarounds for edge cases
- Good performance characteristics

**Status**: Approved  
**Date**: 2025-10-19

---

## Decision Template

For future decisions, use this template:

```markdown
### [Category]-[Number]: [Decision Title]

**Decision**: [One sentence summary]

**Rationale**:
- [Why this decision was made]
- [Supporting arguments]

**Alternatives Considered**:
- [Alternative 1]: [Why rejected]
- [Alternative 2]: [Why rejected]

**Implications**:
- [Impact on architecture]
- [Impact on users]
- [Technical debt or constraints]

**Trade-offs**:
- **Gained**: [Benefits]
- **Lost**: [Costs]
- **Accepted Risk**: [Known risks]

**Status**: [Proposed|Approved|Superseded]  
**Date**: YYYY-MM-DD  
**Superseded By**: [Reference if superseded]
```

## Decision Categories

- **AD**: Architectural Decisions
- **SD**: Security Decisions
- **PD**: Performance Decisions
- **APD**: API Design Decisions
- **TD**: Technology Decisions
- **DD**: Data Decisions
- **UD**: UX/UI Decisions

## Change Process

1. Propose decision with filled template
2. Review with team/community
3. Update status to Approved
4. Implement decision
5. Document learnings

## References

- [ARCHITECTURE.md](../ARCHITECTURE.md) - System architecture
- [PRD.md](PRD.md) - Product requirements
- [THREAT_MODEL.md](THREAT_MODEL.md) - Security analysis

---

**Last Updated**: 2025-10-19  
**Total Decisions**: 12  
**Status**: Living Document
