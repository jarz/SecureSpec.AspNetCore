# SecureSpec.AspNetCore

A security-hardened, deterministic, fully auditable OpenAPI 3.0/3.1 documentation and interactive exploration module for ASP.NET Core applications, designed as a secure replacement for Swashbuckle.AspNetCore.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Overview

SecureSpec.AspNetCore provides:

- 🔒 **Security-First**: Hardened against common vulnerabilities with strict CSP, integrity enforcement (SHA256 + SRI), and WASM sandboxing
- 📊 **Deterministic Output**: Canonical serialization ensures stable hashes across builds
- 🎯 **Complete Parity**: Drop-in replacement for Swashbuckle.AspNetCore 6.5.0
- ✅ **Fully Auditable**: 500 comprehensive acceptance criteria with complete test coverage
- 🚀 **Production-Ready**: Rate limiting, thread-safe caching, and performance optimization
- ♿ **Accessible**: WCAG 2.1 AA compliant UI

## Key Features

### Security & Authentication
- HTTP Bearer, API Key (header/query), OAuth 2.0 (Authorization Code with PKCE, Client Credentials), Mutual TLS
- Content Security Policy (CSP) enforcement with nonce generation
- Integrity validation (SHA256 + SRI + optional signatures)
- Comprehensive input sanitization and CRLF protection

### OpenAPI Generation
- Canonical JSON/YAML serialization with deterministic hashes
- Full support for OpenAPI 3.0 and 3.1
- CLR type mapping with nullability semantics
- DataAnnotations integration
- Polymorphism support (AllOf/OneOf/Automatic/Flatten)
- Recursive schema detection with depth limits

### Interactive UI
- Secure WASM-sandboxed request execution
- Try It Out functionality with full media type support
- Deep linking and operation navigation
- Search and filter capabilities
- Accessible keyboard navigation

### Performance & Safety
- Large schema virtualization
- Example generation throttling
- Thread-safe document caching
- Rate limiting buckets (Try It Out, OAuth, Spec Download)
- Resource guards for size and time limits

## Quick Start

### Installation

```bash
# Coming soon - NuGet package installation
dotnet add package SecureSpec.AspNetCore
```

### Basic Usage

```csharp
// In Program.cs or Startup.cs
builder.Services.AddSecureSpec(options =>
{
    options.Documents.Add("v1", doc =>
    {
        doc.Info.Title = "My API";
        doc.Info.Version = "1.0";
    });
});

app.UseSecureSpec();
app.UseSecureSpecUI();
```

## Documentation

Comprehensive documentation is available in the [`docs/`](docs/) directory:

- **[docs/INDEX.md](docs/INDEX.md)** - Documentation navigation hub
- **[docs/README.md](docs/README.md)** - Implementation guide and best practices
- **[docs/PRD.md](docs/PRD.md)** - Complete Product Requirements Document (500 AC)
- **[docs/ISSUES.md](docs/ISSUES.md)** - All 54 implementation issues with details
- **[docs/ROADMAP.md](docs/ROADMAP.md)** - Visual timeline with Gantt charts and dependencies
- **[docs/QUICKREF.md](docs/QUICKREF.md)** - Quick reference guide
- **[docs/SUMMARY.md](docs/SUMMARY.md)** - Executive summary and statistics

## Project Status

🚧 **Currently in Development** - Implementation begins November 1, 2025

All 54 GitHub issues have been created covering the 6 implementation phases:

- **Phase 1** (Weeks 1-2): Core OpenAPI Generation & Schema Fidelity
- **Phase 2** (Weeks 3-4): Security Schemes & OAuth Flows
- **Phase 3** (Weeks 5-6): UI & Interactive Exploration
- **Phase 4** (Week 7): Performance, Guards & Virtualization
- **Phase 5** (Week 8): Diagnostics, Retention & Concurrency
- **Phase 6** (Week 9): Accessibility, CSP & Final Hardening

See [ROADMAP.md](docs/ROADMAP.md) for detailed timeline and dependencies.

## Migration from Swashbuckle

SecureSpec.AspNetCore is designed as a drop-in replacement for Swashbuckle.AspNetCore 6.5.0 with enhanced security. Key differences:

- No automatic camelCase renaming (explicit control)
- No remote `$ref` support (security)
- Basic auth inference removed (explicit schemes only)
- Enhanced security defaults (CSP, integrity checks, rate limiting)

A detailed migration guide will be available in the documentation.

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Development Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022, VS Code, or Rider
- Git

### Building from Source

```bash
git clone https://github.com/jarz/SecureSpec.AspNetCore.git
cd SecureSpec.AspNetCore
dotnet build
dotnet test
```

## Security

Security is a top priority for this project. If you discover a security vulnerability, please see [SECURITY.md](SECURITY.md) for reporting instructions.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built as a security-hardened alternative to [Swashbuckle.AspNetCore](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- Compliant with [OpenAPI Specification 3.0/3.1](https://spec.openapis.org/oas/latest.html)
- Accessibility standards: [WCAG 2.1 Level AA](https://www.w3.org/WAI/WCAG21/quickref/)

## Support

- 📖 [Documentation](docs/INDEX.md)
- 🐛 [Issue Tracker](https://github.com/jarz/SecureSpec.AspNetCore/issues)
- 💬 [Discussions](https://github.com/jarz/SecureSpec.AspNetCore/discussions)

---

**Project Timeline**: November 2025 - January 2026 (9 weeks)  
**Current Phase**: Pre-Implementation  
**Status**: All issues created, ready for development
