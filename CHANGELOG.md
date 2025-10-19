# Changelog

All notable changes to SecureSpec.AspNetCore will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Project Status
- 🚧 **In Development** - Implementation begins November 1, 2025
- All 54 implementation issues created and organized into 6 phases
- Comprehensive documentation suite completed
- Target release: January 2026

### Planned for v1.0.0

#### Added - Core Features
- Canonical JSON/YAML serialization with deterministic hashes
- SchemaId generation with collision handling
- Complete CLR primitive type mapping
- Nullability semantics for OpenAPI 3.0 and 3.1
- Recursion detection with depth limits
- Dictionary and additionalProperties handling
- DataAnnotations integration
- Advanced enum behavior with virtualization
- Polymorphism support (AllOf/OneOf/Automatic/Flatten)

#### Added - Security Features
- HTTP Bearer authentication scheme
- API Key authentication (header and query)
- OAuth 2.0 Authorization Code flow with mandatory PKCE
- OAuth 2.0 Client Credentials flow
- Mutual TLS support (display only)
- Content Security Policy (CSP) enforcement
- Integrity validation (SHA256 + SRI)
- Comprehensive input sanitization
- WASM sandbox for request execution
- Rate limiting with separate buckets

#### Added - UI Features
- SecureSpec UI framework
- Operation display and navigation
- Deep linking and operationId display
- Try It Out request execution
- Models panel with expansion control
- Search and filter functionality
- Server variable substitution
- Vendor extension display
- Links and callbacks support

#### Added - Performance Features
- Document generation performance optimization
- Large schema virtualization
- Example generation throttling
- Resource guards (size and time)
- Thread-safe document cache
- Asset caching with integrity

#### Added - Quality Features
- Structured diagnostics system
- Diagnostics retention with bounded purge
- Filter pipeline with ordered execution
- PreSerialize mutation boundaries
- Concurrency guarantees
- WCAG 2.1 AA accessibility compliance

#### Added - Documentation
- Comprehensive PRD with 500 acceptance criteria
- Implementation guide and best practices
- Visual roadmap with Gantt charts
- 54 detailed issue specifications
- Quick reference guide
- Contributing guidelines
- Security policy
- Code of conduct

### Implementation Phases

#### Phase 1: Core OpenAPI Generation & Schema Fidelity (Weeks 1-2)
- 8 issues covering serialization, SchemaId, type mapping, nullability, recursion, dictionaries, DataAnnotations, and enums

#### Phase 2: Security Schemes & OAuth Flows (Weeks 3-4)
- 8 issues covering authentication schemes, OAuth flows, security semantics, and authorization mappings

#### Phase 3: UI & Interactive Exploration (Weeks 5-6)
- 10 issues covering UI framework, operations, Try It Out, models, search, and navigation

#### Phase 4: Performance, Guards & Virtualization (Week 7)
- 7 issues covering performance optimization, virtualization, throttling, guards, sandbox, and caching

#### Phase 5: Diagnostics, Retention & Concurrency (Week 8)
- 6 issues covering diagnostics, retention, rate limiting, filter pipeline, and concurrency

#### Phase 6: Accessibility, CSP & Final Hardening (Week 9)
- 10 issues covering accessibility, CSP, integrity, sanitization, media types, and polymorphism

---

## Version History Format

Future releases will follow this format:

## [X.Y.Z] - YYYY-MM-DD

### Added
- New features

### Changed
- Changes to existing functionality

### Deprecated
- Features that will be removed in upcoming releases

### Removed
- Removed features

### Fixed
- Bug fixes

### Security
- Security fixes and improvements

---

## Links

- [GitHub Releases](https://github.com/jarz/SecureSpec.AspNetCore/releases)
- [Issues](https://github.com/jarz/SecureSpec.AspNetCore/issues)
- [Roadmap](docs/ROADMAP.md)

---

**Note**: This project is currently in pre-release development. The first stable release (v1.0.0) is scheduled for January 2026.
