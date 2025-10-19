# Security Policy

## Overview

Security is a fundamental priority for SecureSpec.AspNetCore. This project is designed as a security-hardened alternative to Swashbuckle.AspNetCore, with built-in protections against common vulnerabilities.

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

**Note**: The project is currently in pre-release development. Version 1.0.0 is scheduled for release in January 2026.

## Security Features

SecureSpec.AspNetCore includes multiple layers of security:

### Input Validation & Sanitization
- CRLF injection protection
- Unicode normalization (NFC)
- Header value sanitization
- Query parameter validation
- Size limits on inputs (configurable)

### Content Security Policy (CSP)
- Strict CSP enforcement
- Nonce-based script execution
- No unsafe-eval or unsafe-inline (except controlled styles)
- Frame-ancestors protection
- Configurable CSP directives

### Integrity Enforcement
- SHA256 hash verification for all assets
- Subresource Integrity (SRI) support
- Optional cryptographic signatures
- Fail-closed integrity checks
- Tamper detection

### WASM Sandbox Isolation
- Request execution in isolated WASM environment
- No network or DOM access from sandbox
- Memory and CPU limits
- Abort reason codes for violations
- Fixed memory pages (no growth)

### Rate Limiting
- Separate buckets for Try It Out, OAuth, and Spec Download
- Sliding window counters
- Atomic increment operations
- Configurable limits and windows
- Retry-After headers

### Authentication & Authorization
- OAuth 2.0 with mandatory PKCE
- No support for insecure flows (Implicit, Password)
- No Basic auth inference (explicit only)
- Per-operation security overrides
- Security requirement AND/OR semantics

### Additional Protections
- Thread-safe caching with RW locks
- Deterministic serialization (no timing attacks)
- Bounded diagnostics retention
- Resource guards (size, time, depth)
- Example generation throttling

## Reporting a Vulnerability

We take all security vulnerabilities seriously. If you discover a security issue, please follow these steps:

### DO NOT:
- Open a public GitHub issue
- Discuss the vulnerability in public forums
- Share details on social media
- Test the vulnerability on production systems

### DO:
1. **Email the security team** at: [INSERT SECURITY EMAIL ADDRESS]
2. **Include in your report**:
   - Description of the vulnerability
   - Steps to reproduce
   - Potential impact
   - Suggested fix (if you have one)
   - Your contact information

3. **Use encryption** if reporting highly sensitive issues:
   - PGP Key: [INSERT PGP KEY OR LINK]

### What to Expect

- **Initial Response**: Within 48 hours
- **Progress Update**: Within 7 days
- **Resolution Timeline**: Varies by severity
  - Critical: 1-7 days
  - High: 7-30 days
  - Medium: 30-90 days
  - Low: Best effort

### Disclosure Policy

We follow coordinated disclosure:

1. You report the vulnerability privately
2. We investigate and develop a fix
3. We release a patch
4. We publicly disclose the issue (crediting you if desired)
5. You may publish your findings after disclosure

**Embargo Period**: We ask for at least 90 days before public disclosure to give users time to update.

## Security Best Practices for Users

### Configuration
- Use strong, unique secrets for signing keys
- Enable integrity checking in production
- Configure appropriate rate limits
- Set restrictive CSP policies
- Enable HTTPS/TLS for all endpoints

### Deployment
- Keep dependencies up to date
- Monitor security advisories
- Review diagnostics logs regularly
- Implement defense in depth
- Use secure hosting environments

### OAuth Configuration
- Always use PKCE with Authorization Code flow
- Use Client Credentials only for machine-to-machine
- Never use Implicit or Password flows
- Validate redirect URIs strictly
- Rotate client secrets regularly

### API Keys
- Never commit keys to source control
- Use environment variables or key vaults
- Rotate keys periodically
- Use different keys per environment
- Implement key revocation process

## Known Limitations & Non-Goals

The following are explicitly out of scope for security reasons (see [docs/PRD.md](docs/PRD.md) Section 3):

- **No Swagger 2.0 support**: Legacy format with known security issues
- **No Implicit/Password OAuth flows**: Insecure by design
- **No remote `$ref` resolution**: SSRF vulnerability vector
- **No remote validator URLs**: Data exfiltration risk
- **No raw JavaScript plugins**: XSS vector
- **No full HTML override**: Injection risk
- **No persist authorization**: Credential exposure
- **No withCredentials**: CORS security
- **No Basic auth inference**: Weak authentication

These omissions are intentional security decisions.

## Security Audit

SecureSpec.AspNetCore is designed with audit-ability in mind:

- **500 Acceptance Criteria** covering all security requirements
- **Comprehensive test coverage** including security tests
- **Structured diagnostics** with security event logging
- **Deterministic behavior** for reproducible security testing
- **Clear threat model** (STRIDE analysis in PRD)

Independent security audits are welcomed and encouraged.

## Security Contacts

- **Security Team**: [INSERT EMAIL]
- **Project Maintainer**: Tim Jarzombek (via GitHub)
- **Security Advisories**: https://github.com/jarz/SecureSpec.AspNetCore/security/advisories

## Security Hall of Fame

We recognize and thank security researchers who responsibly disclose vulnerabilities:

<!-- Names will be added here after disclosure -->
- *Coming soon*

## Additional Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [OWASP API Security Top 10](https://owasp.org/www-project-api-security/)
- [OpenAPI Security Best Practices](https://spec.openapis.org/oas/latest.html#security)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)

## Updates

This security policy may be updated as the project evolves. Check back regularly for changes.

---

**Last Updated**: 2025-10-19  
**Policy Version**: 1.0
