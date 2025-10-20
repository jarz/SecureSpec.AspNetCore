# SecureSpec.AspNetCore - Source Code

This directory contains the source code for the SecureSpec.AspNetCore library.

## Project Structure

```
src/SecureSpec.AspNetCore/
├── Configuration/          # Configuration options and builders
│   ├── SecureSpecOptions.cs
│   ├── DocumentCollection.cs
│   ├── SchemaOptions.cs
│   ├── SecurityOptions.cs
│   ├── UIOptions.cs
│   ├── SerializationOptions.cs
│   └── DiagnosticsOptions.cs
├── Core/                  # Core API discovery and processing
│   └── ApiDiscoveryEngine.cs
├── Schema/                # Schema generation and type mapping
│   └── SchemaGenerator.cs
├── Serialization/         # Canonical serialization and hashing
│   └── CanonicalSerializer.cs
├── Security/              # Security schemes and authentication
├── UI/                    # UI components and rendering
├── Diagnostics/           # Diagnostics and logging
│   └── DiagnosticsLogger.cs
└── Filters/               # Filter pipeline components
```

## Building

Build the library:
```bash
dotnet build
```

## Testing

Run tests:
```bash
dotnet test
```

## Implementation Status

✅ **Completed:**
- Initial project structure
- Configuration API with fluent builders
- Placeholder classes for core components
- Basic test infrastructure

🚧 **In Progress:**
- Phase 1: Core OpenAPI Generation & Schema Fidelity (Weeks 1-2)

📋 **Planned:**
- Phase 2: Security Schemes & OAuth Flows (Weeks 3-4)
- Phase 3: UI & Interactive Exploration (Weeks 5-6)
- Phase 4: Performance, Guards & Virtualization (Week 7)
- Phase 5: Diagnostics, Retention & Concurrency (Week 8)
- Phase 6: Accessibility, CSP & Final Hardening (Week 9)

See [../docs/ROADMAP.md](../docs/ROADMAP.md) for the complete implementation plan.

## Contributing

Please see [../CONTRIBUTING.md](../CONTRIBUTING.md) for contribution guidelines.
