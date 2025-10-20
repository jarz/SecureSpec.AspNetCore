# GitHub Actions Workflows

This directory contains the CI/CD workflows for SecureSpec.AspNetCore.

## Workflows

### CI (ci.yml)

**Triggers:**
- Push to `main`, `develop`, or `copilot/*` branches
- Pull requests to `main` or `develop` branches

**Jobs:**

1. **build-and-test** - Builds and tests on multiple platforms
   - Runs on Ubuntu, Windows, and macOS
   - Builds in Release configuration
   - Executes all unit tests
   - Uploads test results as artifacts

2. **code-quality** - Enforces code quality standards
   - Builds with warnings as errors
   - Collects code coverage metrics
   - Uploads coverage reports

### Package (package.yml)

**Triggers:**
- Git tags matching `v*.*.*` (e.g., `v1.0.0`)
- Manual workflow dispatch with version input

**Jobs:**

1. **package** - Creates NuGet packages
   - Builds in Release configuration
   - Runs all tests
   - Packs NuGet package
   - Uploads package artifacts
   - Publishes to NuGet.org (if tagged and API key configured)

**Setup Required:**
- Add `NUGET_API_KEY` secret to repository settings for publishing

### Security (security.yml)

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop` branches
- Daily at 2 AM UTC (scheduled)

**Jobs:**

1. **dependency-check** - Scans for vulnerable dependencies
   - Checks for known vulnerabilities
   - Lists outdated packages

2. **codeql-analysis** - Static code analysis
   - Scans C# code for security issues
   - Requires security-events write permission
   - Results visible in Security tab

## Local Testing

To test workflows locally before pushing:

```bash
# Build
dotnet build --configuration Release

# Test
dotnet test --configuration Release --no-build

# Pack
dotnet pack src/SecureSpec.AspNetCore/SecureSpec.AspNetCore.csproj --configuration Release --no-build
```

## Maintenance

- Update .NET version in workflows when upgrading project
- Review and update action versions periodically
- Monitor security scan results in repository Security tab
