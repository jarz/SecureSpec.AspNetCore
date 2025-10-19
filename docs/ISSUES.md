# SecureSpec.AspNetCore Implementation Issues

This document outlines all GitHub issues to be created based on the PRD.md implementation plan.

## Phase 1: Core OpenAPI Generation & Schema Fidelity (Weeks 1-2)

### Issue 1.1: Implement Canonical Serializer with Deterministic Hash Generation
**Priority**: Critical  
**Labels**: phase-1, core, serialization  
**Estimated Effort**: 3-5 days

**Description:**
Implement the canonical JSON/YAML serializer that produces deterministic output with stable SHA256 hashes across rebuilds.

**Acceptance Criteria:**
- AC 1-10: Canonical serialization with UTF-8, LF endings, no BOM, normalized whitespace
- AC 19-21: SHA256 hash generation and ETag format (W/"sha256:<first16hex>")
- AC 499: SHA256 hashing after normalization (LF, UTF-8)
- Component arrays sorted lexically (AC 493)
- Numeric serialization locale invariance (AC 45)

**Dependencies:** None

---

### Issue 1.2: Implement SchemaId Strategy with Collision Handling
**Priority**: Critical  
**Labels**: phase-1, core, schema  
**Estimated Effort**: 3-4 days

**Description:**
Implement SchemaId generation strategy with deterministic collision suffix handling (_schemaDup{N}).

**Acceptance Criteria:**
- AC 401: SchemaId generic naming deterministic
- AC 402: Collision applies `_schemaDup{N}` suffix starting at 1
- AC 403: Collision suffix numbering stable across rebuilds
- AC 404: SchemaId strategy override applied before collision detection
- AC 405: Collision diagnostic SCH001 emitted per duplicate
- AC 406: Generic nested types canonical form `Outer«Inner»`
- AC 407: Nullable generic arguments retain canonical ordering
- AC 408: Removing type reclaims suffix sequence deterministically

**Dependencies:** Issue 1.1

---

### Issue 1.3: Implement CLR Primitive Type Mapping
**Priority**: Critical  
**Labels**: phase-1, core, schema, type-mapping  
**Estimated Effort**: 2-3 days

**Description:**
Implement complete CLR primitive to OpenAPI type mapping with all standard .NET types.

**Acceptance Criteria:**
- AC 409: Guid → type:string format:uuid
- AC 410: DateTime/DateTimeOffset → type:string format:date-time
- AC 411: DateOnly → type:string format:date
- AC 412: TimeOnly → type:string format:time
- AC 413: byte[] → type:string format:byte (base64url)
- AC 414: IFormFile → type:string format:binary
- AC 415: Decimal → type:number (no format)
- AC 416: Nullable value types apply nullable:true (3.0) or union (3.1)
- AC 417: Enum string mode preserves declaration order
- AC 418: Integer enum mode uses type:integer
- AC 419: Enum naming policy override applied

**Dependencies:** Issue 1.2

---

### Issue 1.4: Implement Nullability Semantics (OpenAPI 3.0 & 3.1)
**Priority**: Critical  
**Labels**: phase-1, core, schema, nullability  
**Estimated Effort**: 2-3 days

**Description:**
Implement nullability representation for both OpenAPI 3.0 (nullable: true) and 3.1 (union types).

**Acceptance Criteria:**
- AC 420: Reference type optional absent nullable:true in 3.0
- AC 421: Reference type nullable emits nullable:true (3.0) / union (3.1)
- AC 422: Array nullable means array may be null
- AC 423: Nullable items don't require array union
- AC 424: Dictionary value nullable represented inside additionalProperties
- AC 425: OneOf variant nullable with separate variant or union (3.1)
- AC 426: Mixed nullable inside AllOf retains original union semantics

**Dependencies:** Issue 1.3

---

### Issue 1.5: Implement Recursion Detection and Depth Limits
**Priority**: High  
**Labels**: phase-1, core, schema, safety  
**Estimated Effort**: 2 days

**Description:**
Implement cycle detection and maximum traversal depth (default 32) with proper diagnostics.

**Acceptance Criteria:**
- AC 427: Max depth constant enforces cut-off at level 32
- AC 428: Depth exceed logs SCH001-DEPTH diagnostic
- AC 429: Cycle detection prevents infinite traversal
- AC 430: Multiple cycles produce single placeholder per cycle root
- AC 431: Depth change recalculates schema traversal deterministically

**Dependencies:** Issue 1.2

---

### Issue 1.6: Implement Dictionary and AdditionalProperties Handling
**Priority**: High  
**Labels**: phase-1, core, schema  
**Estimated Effort**: 2 days

**Description:**
Implement Dictionary<string,T> mapping to OpenAPI additionalProperties with constraint support.

**Acceptance Criteria:**
- AC 432: Dictionary emits additionalProperties referencing value schema
- AC 433: DataAnnotations on value type applied inside additionalProperties
- AC 434: Conflict between explicit property and dictionary key logs ANN001
- AC 435: Header/value Unicode normalized before constraint evaluation
- AC 436: Dictionary value schema keys lexical ordering
- AC 437: additionalProperties:false blocks extension injection attempts

**Dependencies:** Issue 1.3

---

### Issue 1.7: Implement DataAnnotations Ingestion
**Priority**: High  
**Labels**: phase-1, core, schema, validation  
**Estimated Effort**: 2-3 days

**Description:**
Implement ingestion of DataAnnotations attributes (Required, Range, MinLength, MaxLength, etc.) with conflict detection.

**Acceptance Criteria:**
- AC 31-40: DataAnnotations mapping (Required, Range, MinLength, MaxLength, StringLength, RegularExpression)
- AC 433: DataAnnotations conflict detection
- Proper diagnostic logging (ANN001)

**Dependencies:** Issue 1.3

---

### Issue 1.8: Implement Enum Advanced Behavior
**Priority**: High  
**Labels**: phase-1, core, schema, enums  
**Estimated Effort**: 2-3 days

**Description:**
Implement enum handling with virtualization for large enums (>10K values) and proper ordering.

**Acceptance Criteria:**
- AC 438: Enum declaration order stable across rebuilds
- AC 439: Enum switching integer→string toggles representation
- AC 440: Enum >10K triggers virtualization + VIRT001 diagnostic
- AC 441: Enum search returns results across virtualized segments
- AC 442: Enum naming policy modifies emitted value casing
- AC 443: Enum nullable adds "null" union in 3.1 only

**Dependencies:** Issue 1.3

---

## Phase 2: Security Schemes & OAuth Flows (Weeks 3-4)

### Issue 2.1: Implement HTTP Bearer Security Scheme
**Priority**: Critical  
**Labels**: phase-2, security, authentication  
**Estimated Effort**: 1-2 days

**Description:**
Implement HTTP Bearer token authentication scheme without Basic auth inference.

**Acceptance Criteria:**
- AC 189-195: HTTP Bearer implementation
- AC 221: Basic auth inference blocked with diagnostic AUTH001
- Proper header sanitization

**Dependencies:** Issue 1.1

---

### Issue 2.2: Implement API Key Security Schemes (Header & Query)
**Priority**: Critical  
**Labels**: phase-2, security, authentication  
**Estimated Effort**: 1-2 days

**Description:**
Implement API Key authentication in both header and query parameter locations with name sanitization.

**Acceptance Criteria:**
- AC 196-198: API Key header and query implementation
- Name sanitization and validation
- Proper diagnostic logging

**Dependencies:** Issue 1.1

---

### Issue 2.3: Implement OAuth Authorization Code Flow with PKCE
**Priority**: Critical  
**Labels**: phase-2, security, oauth  
**Estimated Effort**: 3-5 days

**Description:**
Implement OAuth 2.0 Authorization Code flow with required PKCE (code challenge/verifier).

**Acceptance Criteria:**
- AC 199-208: Authorization Code flow with PKCE
- AC 431-435: PKCE auto code challenge/verifier generation
- CSRF double-submit and rotation (AC 199-200)
- Token exchange and refresh handling

**Dependencies:** Issue 1.1

---

### Issue 2.4: Implement OAuth Client Credentials Flow
**Priority**: High  
**Labels**: phase-2, security, oauth  
**Estimated Effort**: 2-3 days

**Description:**
Implement OAuth 2.0 Client Credentials flow with scope support.

**Acceptance Criteria:**
- AC 209-213: Client Credentials flow
- Scoped client authentication
- Token management

**Dependencies:** Issue 1.1

---

### Issue 2.5: Implement Mutual TLS Security Scheme
**Priority**: Medium  
**Labels**: phase-2, security, authentication  
**Estimated Effort**: 1 day

**Description:**
Implement Mutual TLS scheme display (display only, no cert upload).

**Acceptance Criteria:**
- AC 214-216: Mutual TLS display
- No certificate upload capability
- Documentation on external cert management

**Dependencies:** Issue 1.1

---

### Issue 2.6: Implement Security Requirement AND/OR Semantics
**Priority**: High  
**Labels**: phase-2, security, logic  
**Estimated Effort**: 2 days

**Description:**
Implement security requirement logic: AND within requirement, OR across objects.

**Acceptance Criteria:**
- AC 217-220: Security AND/OR semantics
- Proper requirement evaluation
- Clear documentation and examples

**Dependencies:** Issues 2.1-2.5

---

### Issue 2.7: Implement Per-Operation Security Overrides
**Priority**: High  
**Labels**: phase-2, security, operations  
**Estimated Effort**: 2 days

**Description:**
Implement per-operation security array that overrides global when present (no merge).

**Acceptance Criteria:**
- AC 464: Operation-level security present overrides global
- AC 465: Empty operation security array clears global requirements
- AC 466: Security arrays ordering lexical by scheme key
- AC 467: Multiple operation security objects preserve declaration order
- AC 468: Operation security mutation logged

**Dependencies:** Issues 2.1-2.6

---

### Issue 2.8: Implement Policy and Role to Scope Mappings
**Priority**: Medium  
**Labels**: phase-2, security, authorization  
**Estimated Effort**: 2 days

**Description:**
Implement PolicyToScope and RoleToScope mapping hooks with diagnostics.

**Acceptance Criteria:**
- AC 222-223: Policy/Role mapping hooks
- Diagnostic logging (POL001, ROLE001)
- Configuration examples

**Dependencies:** Issue 2.6

---

## Phase 3: UI & Interactive Exploration (Weeks 5-6)

### Issue 3.1: Implement SecureSpec UI Base Framework
**Priority**: Critical  
**Labels**: phase-3, ui, frontend  
**Estimated Effort**: 5-7 days

**Description:**
Implement the base SecureSpec UI framework with proper structure, routing, and component organization.

**Acceptance Criteria:**
- AC 331-340: Base UI structure
- Component architecture
- State management
- Routing framework

**Dependencies:** Issue 1.1

---

### Issue 3.2: Implement Operation Display and Navigation
**Priority**: Critical  
**Labels**: phase-3, ui, operations  
**Estimated Effort**: 3-4 days

**Description:**
Implement operation display with proper grouping, tags, and navigation.

**Acceptance Criteria:**
- AC 341-345: Operation display and grouping
- Tag-based organization
- Collapse/expand functionality
- Operation filtering

**Dependencies:** Issue 3.1

---

### Issue 3.3: Implement Deep Linking and OperationId Display
**Priority**: High  
**Labels**: phase-3, ui, navigation  
**Estimated Effort**: 2 days

**Description:**
Implement deep linking with hash navigation and operationId display control.

**Acceptance Criteria:**
- AC 469: deepLinking enabled scrolls to anchor on hash navigation
- AC 470: deepLinking disabled retains anchor but suppresses auto-scroll
- AC 471: displayOperationId false hides label but anchor ID remains stable
- AC 472: Hash fragment update triggers focus highlight

**Dependencies:** Issue 3.2

---

### Issue 3.4: Implement Try It Out Request Execution
**Priority**: Critical  
**Labels**: phase-3, ui, execution  
**Estimated Effort**: 5-7 days

**Description:**
Implement the Try It Out functionality with request building and execution.

**Acceptance Criteria:**
- AC 346-355: Try It Out request building
- Parameter input forms
- Request body editors
- cURL generation
- Response display

**Dependencies:** Issue 3.2, Issue 4.5 (WASM Sandbox)

---

### Issue 3.5: Implement Models Panel with Expansion Control
**Priority**: High  
**Labels**: phase-3, ui, models  
**Estimated Effort**: 3-4 days

**Description:**
Implement Models/Schemas panel with configurable expansion depths.

**Acceptance Criteria:**
- AC 477: defaultModelsExpandDepth -1 hides Models panel
- AC 478: Positive depth expands models tree up to configured level
- AC 479: defaultModelExpandDepth limits nested property expansion
- AC 480: Depth changes re-render deterministically

**Dependencies:** Issue 3.1

---

### Issue 3.6: Implement Search and Filter Functionality
**Priority**: High  
**Labels**: phase-3, ui, search  
**Estimated Effort**: 2-3 days

**Description:**
Implement operation search/filter with sanitization and debounce.

**Acceptance Criteria:**
- AC 481: InitialFilter applied before first render
- AC 482: Clearing filter restores full operation list
- AC 483: Control characters removed from search input
- AC 484: Debounce interval honored (default 150ms)
- AC 485: Memory allocation for results below configured cap
- AC 486: Compatibility mode matches legacy substring behavior

**Dependencies:** Issue 3.2

---

### Issue 3.7: Implement Supported Submit Methods Whitelist
**Priority**: Medium  
**Labels**: phase-3, ui, configuration  
**Estimated Effort**: 1-2 days

**Description:**
Implement HTTP method whitelisting with proper UI feedback for unsupported methods.

**Acceptance Criteria:**
- AC 473: Whitelist excludes TRACE/CONNECT (non-executable)
- AC 474: Adding HEAD enables Try It Out for HEAD only
- AC 475: Whitelist modification reflected immediately
- AC 476: Unsupported method invocation logs LIM001

**Dependencies:** Issue 3.4

---

### Issue 3.8: Implement Server Variable Substitution
**Priority**: Medium  
**Labels**: phase-3, ui, configuration  
**Estimated Effort**: 2 days

**Description:**
Implement safe server variable editing with revert on invalid and isolation.

**Acceptance Criteria:**
- AC 356-360: Server variable substitution
- Safe edit with validation
- Revert invalid changes
- Isolation between requests

**Dependencies:** Issue 3.1

---

### Issue 3.9: Implement Vendor Extension Display
**Priority**: Medium  
**Labels**: phase-3, ui, extensions  
**Estimated Effort**: 2 days

**Description:**
Implement vendor extension display with sanitization, truncation, and lexical ordering.

**Acceptance Criteria:**
- AC 487: Extension truncation at 5120 bytes deterministic
- AC 488: Allowed token types restricted
- AC 489: Null extension coerced to string "null" with badge
- AC 490: Lexical ordering of extension keys preserved
- AC 491: Nested object extension keys sorted lexically
- AC 492: Expansion retains sanitized structure

**Dependencies:** Issue 3.1

---

### Issue 3.10: Implement Links and Callbacks Display
**Priority**: Low  
**Labels**: phase-3, ui, advanced  
**Estimated Effort**: 2-3 days

**Description:**
Implement read-only display of Links and Callbacks with edge case handling.

**Acceptance Criteria:**
- AC 493: Circular link detection logs diagnostic
- AC 494: Missing operationId but valid operationRef uses operationRef
- AC 495: Missing both operationId & operationRef logs warning
- AC 496: Callback section read-only (no Try It Out)
- AC 497: Broken $ref emits error and omits reference safely

**Dependencies:** Issue 3.1

---

## Phase 4: Performance, Guards & Virtualization (Week 7)

### Issue 4.1: Implement Document Generation Performance Targets
**Priority**: Critical  
**Labels**: phase-4, performance, core  
**Estimated Effort**: 3-5 days

**Description:**
Optimize document generation to meet performance targets (<500ms for 1000 operations).

**Acceptance Criteria:**
- AC 297-300: Generation performance targets
- Performance monitoring instrumentation
- Degraded/failure thresholds
- Performance diagnostics

**Dependencies:** All Phase 1 issues

---

### Issue 4.2: Implement Large Schema Virtualization
**Priority**: High  
**Labels**: phase-4, performance, virtualization  
**Estimated Effort**: 3-4 days

**Description:**
Implement virtualization for large schemas (>200 properties or >50 nested) with lazy loading.

**Acceptance Criteria:**
- AC 301-303: Virtualization thresholds
- AC 440: Large enum virtualization (>10K values)
- VIRT001 diagnostic
- Placeholder tokens and lazy rendering

**Dependencies:** Issues 1.3, 1.8

---

### Issue 4.3: Implement Example Generation Throttling
**Priority**: High  
**Labels**: phase-4, performance, examples  
**Estimated Effort**: 2 days

**Description:**
Implement example synthesis throttling with 25ms time budget per example.

**Acceptance Criteria:**
- AC 304-306: Example throttling
- EXM001 diagnostic on exceed
- Truncated nested structure expansion
- Atomic counters for thread safety

**Dependencies:** Issue 1.3

---

### Issue 4.4: Implement Resource Guards (Size & Time)
**Priority**: Critical  
**Labels**: phase-4, performance, safety  
**Estimated Effort**: 2-3 days

**Description:**
Implement size and time guards for document generation with fallback behavior.

**Acceptance Criteria:**
- AC 319-324: Size/time guard enforcement
- Fallback document generation
- PERF001 diagnostic
- Memory and CPU monitoring

**Dependencies:** Issue 1.1

---

### Issue 4.5: Implement WASM Sandbox Isolation
**Priority**: Critical  
**Labels**: phase-4, security, sandbox  
**Estimated Effort**: 5-7 days

**Description:**
Implement WASM sandbox for request execution with memory and CPU limits.

**Acceptance Criteria:**
- AC 209-213: Sandbox isolation
- AC 436: Hard fail on network/DOM attempts
- Memory cap enforcement
- CPU timeout
- Abort reason codes (MEMORY_CAP, CPU_TIMEOUT, API_VIOLATION)

**Dependencies:** Issue 3.4

---

### Issue 4.6: Implement Thread-Safe Document Cache
**Priority**: High  
**Labels**: phase-4, performance, concurrency  
**Estimated Effort**: 2-3 days

**Description:**
Implement thread-safe document cache with RW lock (multiple readers, single writer).

**Acceptance Criteria:**
- AC 325-330: Cache implementation
- RW lock strategy
- Cache invalidation
- Integrity revalidation post-expiry

**Dependencies:** Issue 1.1

---

### Issue 4.7: Implement Asset Caching with Integrity
**Priority**: Medium  
**Labels**: phase-4, performance, caching  
**Estimated Effort**: 2 days

**Description:**
Implement Cache-Control headers for assets with post-expiry integrity revalidation.

**Acceptance Criteria:**
- AC 16: Asset caching configuration
- Cache-Control header generation
- Post-expiry integrity checks
- Cache configuration options

**Dependencies:** Issue 1.1

---

## Phase 5: Diagnostics, Retention & Concurrency (Week 8)

### Issue 5.1: Implement Structured Diagnostics System
**Priority**: Critical  
**Labels**: phase-5, diagnostics, monitoring  
**Estimated Effort**: 3-4 days

**Description:**
Implement comprehensive structured diagnostics logging with event schema.

**Acceptance Criteria:**
- AC 381-390: Diagnostic event structure
- All error codes defined (SEC001, CSP001, SCH001, etc.)
- Severity levels (Info, Warn, Error, Critical)
- Context metadata
- Sanitization flags

**Dependencies:** All previous issues

---

### Issue 5.2: Implement Diagnostics Retention with Bounded Purge
**Priority**: High  
**Labels**: phase-5, diagnostics, storage  
**Estimated Effort**: 2-3 days

**Description:**
Implement diagnostics retention with atomic FIFO purge based on size and age.

**Acceptance Criteria:**
- AC 391-400: Retention policy
- Size-based purge (RET001)
- Age-based purge (RET002)
- Atomic FIFO ordering
- Purge diagnostics

**Dependencies:** Issue 5.1

---

### Issue 5.3: Implement Rate Limiting Buckets
**Priority**: Critical  
**Labels**: phase-5, security, rate-limiting  
**Estimated Effort**: 3-4 days

**Description:**
Implement rate limiting with separate buckets for TryItOut, OAuth, and SpecDownload.

**Acceptance Criteria:**
- AC 301-303: Rate limit buckets
- Sliding window counters
- Atomic increment/read
- Retry-After header
- LIM001 diagnostic
- Burst handling

**Dependencies:** Issue 3.4

---

### Issue 5.4: Implement Filter Pipeline Ordering
**Priority**: High  
**Labels**: phase-5, core, extensibility  
**Estimated Effort**: 2-3 days

**Description:**
Implement ordered filter pipeline execution with proper propagation.

**Acceptance Criteria:**
- AC 459: Schema filters execute before Operation filters
- AC 460: Operation filters before Parameter filters
- AC 461: Parameter filters before RequestBody filters
- AC 462: RequestBody filters before Document filters
- AC 463: Document filters before PreSerialize filters
- Timestamp verification tests

**Dependencies:** Issues 1.3, 1.7

---

### Issue 5.5: Implement PreSerialize Mutation Boundaries
**Priority**: High  
**Labels**: phase-5, core, security  
**Estimated Effort**: 2 days

**Description:**
Implement PreSerialize filter boundaries with enforcement that only servers/security can be mutated.

**Acceptance Criteria:**
- AC 400: Structural schema mutation forbidden post PreSerialize
- AC 433: PreSerialize boundary enforcement
- Mutation attempt logging and blocking
- Sanitization ordering

**Dependencies:** Issue 5.4

---

### Issue 5.6: Implement Concurrency Guarantees
**Priority**: High  
**Labels**: phase-5, performance, concurrency  
**Estimated Effort**: 2 days

**Description:**
Ensure thread safety across all components with proper locking strategy.

**Acceptance Criteria:**
- AC 38-39: Thread safety guarantees
- Immutable filter lists after startup
- Isolated PreSerialize contexts
- No shared mutable schema objects
- Atomic counter usage

**Dependencies:** Issues 4.6, 5.4

---

## Phase 6: Accessibility, CSP & Final Hardening (Week 9)

### Issue 6.1: Implement Accessibility (WCAG 2.1 AA)
**Priority**: High  
**Labels**: phase-6, accessibility, ui  
**Estimated Effort**: 3-5 days

**Description:**
Implement full WCAG 2.1 Level AA accessibility compliance.

**Acceptance Criteria:**
- AC 325-330: Skip link implementation
- AC 331-335: Keyboard navigation for all interactive elements
- AC 336-340: aria-expanded synchronization
- AC 341-345: Modal focus trap and ESC dismissal
- AC 346-350: Contrast ratios ≥4.5:1
- AC 351-355: Screen reader announcements with live regions
- AC 356-360: Focus indicators ≥2px visible

**Dependencies:** Issue 3.1

---

### Issue 6.2: Implement Content Security Policy (CSP)
**Priority**: Critical  
**Labels**: phase-6, security, csp  
**Estimated Effort**: 2-3 days

**Description:**
Implement strict CSP policy with nonce generation and enforcement.

**Acceptance Criteria:**
- AC 209-213: CSP policy enforcement
- AC 498: CSP policy matches defined directives exactly
- No unsafe-eval, no remote script origins
- Nonce generation per request
- CSP001 diagnostic on mismatch

**Dependencies:** Issue 3.1

---

### Issue 6.3: Implement Integrity Enforcement (SHA256 + SRI)
**Priority**: Critical  
**Labels**: phase-6, security, integrity  
**Estimated Effort**: 2-3 days

**Description:**
Implement SHA256 hash verification with SRI (Subresource Integrity) support.

**Acceptance Criteria:**
- AC 19-21: SHA256 + SRI + signature
- AC 304-306: Integrity fail-close
- AC 499: SHA256 hashing after normalization
- AC 500: Integrity mismatch diagnostic with redaction
- SEC001 critical diagnostic on failure

**Dependencies:** Issue 1.1

---

### Issue 6.4: Implement Header and Input Sanitization
**Priority**: Critical  
**Labels**: phase-6, security, sanitization  
**Estimated Effort**: 2-3 days

**Description:**
Implement comprehensive sanitization for headers, server variables, and user input.

**Acceptance Criteria:**
- AC 31-32: Sanitization, redaction, restricted head
- AC 238-241: CRLF strip, Unicode normalization, sanitized head/CSS
- AC 435: Unicode normalization (NFC)
- Header sanitization ordering
- HD001 diagnostic on disallowed head injection

**Dependencies:** Issue 3.1

---

### Issue 6.5: Implement Signature Support (Optional Extension)
**Priority**: Low  
**Labels**: phase-6, security, signing  
**Estimated Effort**: 2-3 days

**Description:**
Implement optional cryptographic signature support for documents.

**Acceptance Criteria:**
- Signature generation configuration
- Signature verification
- Algorithm agility (SHA256 baseline)
- Exit code 5 on signature mismatch

**Dependencies:** Issue 6.3

---

### Issue 6.6: Implement Fallback Document Behavior
**Priority**: High  
**Labels**: phase-6, safety, resilience  
**Estimated Effort**: 2 days

**Description:**
Implement fallback document generation on validation/timeout/memory failures.

**Acceptance Criteria:**
- AC 41: Fallback triggers
- Minimal surface: Info + sanitized banner only
- No paths, operations, or security arrays
- No stale operation leakage
- Previous doc discarded

**Dependencies:** Issue 4.4

---

### Issue 6.7: Implement Media Type Handling and Content Negotiation
**Priority**: High  
**Labels**: phase-6, core, media-types  
**Estimated Effort**: 3-4 days

**Description:**
Implement deterministic media type ordering and content negotiation with validation.

**Acceptance Criteria:**
- AC 452: Deterministic media type ordering
- AC 453: Shared schema across media types uses single $ref
- AC 454: Multipart validator enforces field count limit
- AC 455: Multipart file + field mix preserves ordering
- AC 456: Binary size threshold enforcement
- AC 457: text/plain request handling
- AC 458: application/xml generation stable

**Dependencies:** Issue 1.3

---

### Issue 6.8: Implement Polymorphism Support
**Priority**: High  
**Labels**: phase-6, core, schema, polymorphism  
**Estimated Effort**: 3-5 days

**Description:**
Implement full polymorphism support with AllOf/OneOf/Automatic/Flatten strategies.

**Acceptance Criteria:**
- AC 444: Discriminator property emission
- AC 445: Missing variant mapping logs warning
- AC 446: Reserved keyword variant names escaped
- AC 447: AllOf variant ordering lexical by SchemaId
- AC 448: OneOf variant ordering lexical by discriminator key
- AC 449: Automatic strategy chooses AllOf appropriately
- AC 450: Automatic strategy chooses OneOf appropriately
- AC 451: Fallback without discriminator retains variants

**Dependencies:** Issue 1.3

---

### Issue 6.9: Implement Example Precedence Engine
**Priority**: Medium  
**Labels**: phase-6, core, examples  
**Estimated Effort**: 2-3 days

**Description:**
Implement example precedence: Named > Single/Attribute > Component > Generated > Blocked.

**Acceptance Criteria:**
- AC 4: Example precedence order
- Named examples have priority
- Single/attribute examples
- Component reference examples
- Generated fallback examples
- Blocked example handling

**Dependencies:** Issue 1.3

---

### Issue 6.10: Implement Multi-File XML Documentation Ingestion
**Priority**: Medium  
**Labels**: phase-6, core, documentation  
**Estimated Effort**: 2-3 days

**Description:**
Implement ordered merge of multiple XML documentation files with conflict diagnostics.

**Acceptance Criteria:**
- AC 9: Multi-file XML ingestion
- Ordered merge logic
- Conflict detection and diagnostics
- Proper comment resolution

**Dependencies:** Issue 1.3

---

## Cross-Cutting Issues

### Issue X.1: Create CLI Tools
**Priority**: High  
**Labels**: tooling, cli  
**Estimated Effort**: 3-5 days

**Description:**
Create CLI tools for document generation, validation, and integrity checking.

**Acceptance Criteria:**
- Document generation command
- Validation command
- Integrity check command
- Exit codes (0-5) mapping
- Configuration file support

**Dependencies:** Issues 1.1, 6.3

---

### Issue X.2: Create Comprehensive Test Suite
**Priority**: Critical  
**Labels**: testing, quality  
**Estimated Effort**: Ongoing across all phases

**Description:**
Create comprehensive test coverage as specified in testing strategy.

**Acceptance Criteria:**
- Unit tests: Generation 95%, Security 100%, UI 85%, Rate Limiting 90%, Virtualization 90%, Accessibility 80%
- Integration tests: Generation 90%, Security 95%, UI 85%, Rate Limiting 95%, Virtualization 90%, Accessibility 80%
- E2E tests: Generation 85%, Security 90%, UI 90%, Rate Limiting 85%, Virtualization 85%, Accessibility 90%
- Performance tests for all categories

**Dependencies:** All feature issues

---

### Issue X.3: Create Migration Guide from Swashbuckle
**Priority**: High  
**Labels**: documentation, migration  
**Estimated Effort**: 3-5 days

**Description:**
Create comprehensive migration guide from Swashbuckle.AspNetCore to SecureSpec.

**Acceptance Criteria:**
- API mapping documentation
- Breaking changes documentation
- Migration examples
- Step-by-step guide
- Common pitfall warnings

**Dependencies:** All core issues

---

### Issue X.4: Create Configuration Documentation
**Priority**: High  
**Labels**: documentation  
**Estimated Effort**: 3-5 days

**Description:**
Create comprehensive configuration documentation with examples.

**Acceptance Criteria:**
- All configuration options documented
- Default values specified
- Examples for common scenarios
- Security configuration guidance
- Performance tuning guidance

**Dependencies:** All configuration-related issues

---

### Issue X.5: Implement Monitoring and Observability
**Priority**: Medium  
**Labels**: monitoring, observability  
**Estimated Effort**: 2-3 days

**Description:**
Implement metrics collection and observability as specified in monitoring section.

**Acceptance Criteria:**
- Gen latency p95 instrumentation
- Integrity failure counting
- Rate limit violation counters
- Sandbox memory gauges
- Diagnostics purge frequency counting
- Enum search latency timers
- Alert threshold configuration

**Dependencies:** Issues 5.1, 5.3, 4.5

---

## Issue Creation Instructions

To create these issues in GitHub, you can use the GitHub CLI (`gh`) or the GitHub API. Here's an example using `gh`:

```bash
# Example for Issue 1.1
gh issue create \
  --title "Phase 1.1: Implement Canonical Serializer with Deterministic Hash Generation" \
  --body "$(cat issue-1.1-description.md)" \
  --label "phase-1,core,serialization" \
  --assignee "" \
  --project "SecureSpec Implementation" \
  --milestone "Phase 1"
```

Or use the provided script (see docs/create-issues.sh) to batch create all issues.

## Priority Legend
- **Critical**: Blocking for subsequent work, core functionality
- **High**: Important for feature completeness, significant user impact
- **Medium**: Important but not blocking, nice to have features
- **Low**: Optional enhancements, can be deferred

## Dependency Management
Issues should be worked in order within each phase, respecting cross-issue dependencies. Dependencies are listed explicitly for each issue.

## Progress Tracking
Use GitHub Projects or ZenHub to track progress across phases. Recommend creating a board with columns for each phase plus "Backlog", "In Progress", "Review", and "Done".
