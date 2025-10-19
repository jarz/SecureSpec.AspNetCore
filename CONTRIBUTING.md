# Contributing to SecureSpec.AspNetCore

Thank you for your interest in contributing to SecureSpec.AspNetCore! This document provides guidelines and instructions for contributing to the project.

## Code of Conduct

This project adheres to a Code of Conduct that all contributors are expected to follow. Please read [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) before contributing.

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Git
- A GitHub account
- Familiarity with C#, ASP.NET Core, and OpenAPI/Swagger

### Setting Up Your Development Environment

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/SecureSpec.AspNetCore.git
   cd SecureSpec.AspNetCore
   ```
3. **Add upstream remote**:
   ```bash
   git remote add upstream https://github.com/jarz/SecureSpec.AspNetCore.git
   ```
4. **Build the project**:
   ```bash
   dotnet build
   ```
5. **Run tests**:
   ```bash
   dotnet test
   ```

## How to Contribute

### Reporting Bugs

Before creating a bug report:
- Check the [issue tracker](https://github.com/jarz/SecureSpec.AspNetCore/issues) to see if the issue already exists
- Ensure you're using the latest version

When creating a bug report, include:
- A clear, descriptive title
- Steps to reproduce the issue
- Expected behavior vs actual behavior
- Your environment (OS, .NET version, etc.)
- Code samples or test cases if applicable
- Screenshots or error messages

Use the bug report template when creating an issue.

### Suggesting Enhancements

Enhancement suggestions are welcome! Before submitting:
- Check if the enhancement has already been suggested
- Review the [PRD.md](docs/PRD.md) to understand project scope and goals

Include in your enhancement suggestion:
- A clear, descriptive title
- Detailed description of the proposed feature
- Rationale for the enhancement
- Examples of how it would be used
- Any potential drawbacks or alternatives considered

### Pull Requests

#### Before Submitting a Pull Request

1. **Check existing issues**: Look for related issues or create one to discuss your changes
2. **Follow the roadmap**: Align with the project's [ROADMAP.md](docs/ROADMAP.md) and phase priorities
3. **Keep changes focused**: One feature or fix per pull request
4. **Write tests**: All new code should have corresponding tests
5. **Update documentation**: Update relevant documentation for your changes

#### Pull Request Process

1. **Create a feature branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes**:
   - Follow the coding standards (see below)
   - Write clear, concise commit messages
   - Add tests for new functionality
   - Update documentation as needed

3. **Ensure all tests pass**:
   ```bash
   dotnet build
   dotnet test
   ```

4. **Commit your changes**:
   ```bash
   git add .
   git commit -m "Brief description of changes"
   ```
   
   Use conventional commit format:
   - `feat:` for new features
   - `fix:` for bug fixes
   - `docs:` for documentation changes
   - `test:` for test additions/changes
   - `refactor:` for code refactoring
   - `perf:` for performance improvements
   - `chore:` for maintenance tasks

5. **Push to your fork**:
   ```bash
   git push origin feature/your-feature-name
   ```

6. **Create a Pull Request** on GitHub:
   - Use a clear, descriptive title
   - Reference related issues (e.g., "Fixes #123")
   - Describe your changes in detail
   - Explain why the changes are needed
   - Note any breaking changes

7. **Respond to feedback**:
   - Address review comments promptly
   - Make requested changes in new commits
   - Mark conversations as resolved when addressed

## Coding Standards

### C# Style Guidelines

Follow the [.NET coding conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions):

- Use PascalCase for class names and method names
- Use camelCase for local variables and parameters
- Use meaningful, descriptive names
- Prefer explicit types over `var` when type isn't obvious
- Use XML documentation comments for public APIs

### Code Organization

- Keep files focused and single-purpose
- Limit file length (ideally < 500 lines)
- Use regions sparingly, only when they improve readability
- Follow existing project structure and namespacing

### Testing

- Write unit tests for all new functionality
- Aim for >90% code coverage for critical paths
- Use descriptive test names: `MethodName_Scenario_ExpectedResult`
- Follow the Arrange-Act-Assert pattern
- Mock external dependencies
- Tests should be fast and deterministic

### Security Considerations

This is a security-focused project. All contributions must:

- Never introduce security vulnerabilities
- Follow secure coding practices
- Sanitize all user inputs
- Use parameterized queries/commands
- Validate and escape output
- Handle errors securely (no information leakage)
- Consider performance implications (DoS prevention)

Refer to [docs/PRD.md](docs/PRD.md) Section 7 (Threat Model) for security requirements.

### Performance

- Be mindful of performance implications
- Profile code for performance bottlenecks
- Consider memory allocations and GC pressure
- Use async/await appropriately
- Refer to [docs/PRD.md](docs/PRD.md) Section 5 for performance targets

## Documentation

### Code Documentation

- Add XML documentation comments to all public APIs
- Explain non-obvious code with inline comments
- Document complex algorithms and business logic
- Keep comments up-to-date with code changes

### User Documentation

When adding features, update:
- README.md (if affecting getting started)
- Relevant files in `docs/` directory
- Code examples and samples
- Migration guides (if breaking changes)

## Issue and Pull Request Labels

We use labels to organize and prioritize work:

### Phase Labels
- `phase-1` through `phase-6`: Indicates implementation phase
- `cross-cutting`: Applies to multiple phases

### Type Labels
- `bug`: Something isn't working
- `enhancement`: New feature or request
- `documentation`: Documentation improvements
- `question`: Further information requested
- `security`: Security-related issue

### Priority Labels
- `priority-critical`: Blocking, must be done
- `priority-high`: Important features
- `priority-medium`: Nice to have
- `priority-low`: Optional enhancements

### Status Labels
- `good-first-issue`: Good for newcomers
- `help-wanted`: Extra attention needed
- `in-progress`: Currently being worked on
- `needs-review`: Ready for review

## Review Process

All submissions require review before merging:

1. **Automated checks**: CI/CD pipeline runs tests and checks
2. **Code review**: At least one maintainer reviews the code
3. **Testing**: Verify functionality works as expected
4. **Documentation review**: Ensure docs are updated
5. **Security review**: For security-sensitive changes

Reviews may take several days. Be patient and responsive to feedback.

## Community

- Be respectful and inclusive
- Welcome newcomers and help them get started
- Provide constructive feedback
- Focus on the code, not the person
- Acknowledge good work and contributions

## Questions?

- Check the [documentation](docs/INDEX.md)
- Search [existing issues](https://github.com/jarz/SecureSpec.AspNetCore/issues)
- Start a [discussion](https://github.com/jarz/SecureSpec.AspNetCore/discussions)
- Reach out to maintainers

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to SecureSpec.AspNetCore! 🎉
