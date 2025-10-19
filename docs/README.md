# SecureSpec.AspNetCore Issues Creation Guide

This directory contains tools and documentation for creating GitHub issues based on the PRD (Product Requirements Document).

## Files

- **PRD.md** - The complete Product Requirements Document with all specifications and acceptance criteria (AC 1-500)
- **ISSUES.md** - Human-readable breakdown of all issues organized by implementation phase
- **issues.json** - Machine-readable issue definitions for programmatic import
- **create-issues.sh** - Bash script for creating issues using GitHub CLI
- **create-issues.py** - Python script for creating issues using GitHub API

## Quick Start

Choose one of the following methods to create the issues:

### Method 1: GitHub CLI (Recommended for manual review)

1. Install GitHub CLI: https://cli.github.com/
2. Authenticate: `gh auth login`
3. Run the script:
   ```bash
   cd docs
   ./create-issues.sh
   ```
4. For a dry run (see what would be created):
   ```bash
   ./create-issues.sh --dry-run
   ```

**Note**: The bash script currently only creates Phase 1 and Phase 2 issues as examples. You'll need to extend it for additional phases.

### Method 2: Python Script (Recommended for batch creation)

1. Install PyGithub:
   ```bash
   pip install PyGithub
   ```

2. Create a GitHub Personal Access Token:
   - Go to https://github.com/settings/tokens
   - Click "Generate new token (classic)"
   - Select scopes: `repo` (all permissions)
   - Copy the token

3. Run the script:
   ```bash
   cd docs
   export GITHUB_TOKEN=your_token_here
   python create-issues.py
   ```

4. For a dry run:
   ```bash
   python create-issues.py --dry-run
   ```

5. For a different repository:
   ```bash
   python create-issues.py --repo owner/repo-name
   ```

### Method 3: Manual Creation via GitHub Web UI

Use ISSUES.md as a reference and manually create issues through the GitHub web interface.

## Issue Structure

Each issue contains:

- **Title**: Descriptive title with phase prefix
- **Description**: What needs to be implemented
- **Estimated Effort**: Days of work expected
- **Priority**: Critical, High, Medium, or Low
- **Dependencies**: References to other issues that must be completed first
- **Acceptance Criteria**: Specific requirements from PRD (AC numbers)
- **Phase**: Which implementation phase the issue belongs to
- **Labels**: For filtering and organization

## Implementation Phases

The project is organized into 6 phases plus cross-cutting concerns:

### Phase 1: Core OpenAPI Generation & Schema Fidelity (Weeks 1-2)
- 8 issues
- Focus: Canonical serialization, SchemaId, type mapping, nullability
- Critical foundation for all subsequent work

### Phase 2: Security Schemes & OAuth Flows (Weeks 3-4)
- 8 issues
- Focus: Authentication schemes, OAuth flows with PKCE, security requirements
- Critical for secure API exploration

### Phase 3: UI & Interactive Exploration (Weeks 5-6)
- 10 issues
- Focus: UI framework, operation display, Try It Out, search/filter
- User-facing functionality

### Phase 4: Performance, Guards & Virtualization (Week 7)
- 7 issues
- Focus: Performance optimization, virtualization, WASM sandbox, caching
- Ensures scalability and security

### Phase 5: Diagnostics, Retention & Concurrency (Week 8)
- 6 issues
- Focus: Diagnostics system, rate limiting, filter pipeline, thread safety
- Production-ready concerns

### Phase 6: Accessibility, CSP & Final Hardening (Week 9)
- 10 issues
- Focus: WCAG 2.1 AA, CSP, integrity enforcement, sanitization
- Security hardening and compliance

### Cross-Cutting Concerns
- 5 issues
- Focus: CLI tools, testing, documentation, monitoring
- Ongoing throughout development

**Total**: 54 issues

## Issue Labels

Issues are tagged with the following labels for organization:

### Phase Labels
- `phase-1` through `phase-6`
- `cross-cutting`

### Category Labels
- `core` - Core functionality
- `security` - Security features
- `ui` - User interface
- `performance` - Performance optimizations
- `diagnostics` - Logging and monitoring
- `accessibility` - Accessibility features
- `documentation` - Documentation work
- `testing` - Test creation

### Component Labels
- `serialization` - Serialization logic
- `schema` - Schema generation
- `authentication` - Auth schemes
- `oauth` - OAuth flows
- `operations` - Operation handling
- `caching` - Caching systems
- `sandbox` - WASM sandbox
- `rate-limiting` - Rate limiting

### Priority Labels
- `priority-critical` - Blocking work, must be done
- `priority-high` - Important features
- `priority-medium` - Nice to have
- `priority-low` - Optional enhancements

## Acceptance Criteria (AC) Reference

Each issue references specific Acceptance Criteria (AC) from the PRD.md:

- **AC 1-400**: Original PRD criteria
- **AC 401-500**: Additional parity confirmations (added in final PRD version)

Examples:
- AC 1-10: Canonical serialization
- AC 401-408: SchemaId and collision handling
- AC 409-419: CLR primitive mapping
- AC 420-426: Nullability semantics
- AC 427-431: Recursion and depth limits

Refer to PRD.md for complete details on each AC.

## Dependencies

Issues list their dependencies explicitly. The scripts handle dependency ordering automatically:

- Issues are created in dependency order
- Phase 1 issues have minimal dependencies
- Later phase issues depend on earlier phases
- Cross-cutting issues reference specific features they support

## Customization

### Modifying Issues

To modify the issue list:

1. Edit `issues.json` - This is the source of truth for the Python script
2. Edit `ISSUES.md` - Human-readable reference
3. Edit `create-issues.sh` - If using the bash script

### Adding New Issues

To add new issues:

1. Add to `issues.json` following the existing structure
2. Document in `ISSUES.md` for clarity
3. Update scripts if needed
4. Ensure correct phase and dependency information

### Changing Repository

To create issues in a different repository:

**Python script**:
```bash
python create-issues.py --repo your-org/your-repo
```

**Bash script**:
Edit the `REPO` variable at the top of `create-issues.sh`

## Troubleshooting

### GitHub CLI Authentication Issues
```bash
gh auth logout
gh auth login
```

### Rate Limiting
The Python script includes automatic rate limiting (1 second between requests). If you hit GitHub's rate limit:
- Wait 60 seconds
- The script will automatically retry

### PyGithub Installation Issues
```bash
pip install --upgrade pip
pip install PyGithub
```

### Permission Errors
Ensure your GitHub token has the `repo` scope with full permissions.

## Best Practices

1. **Test with dry-run first**: Always run with `--dry-run` to preview before creating issues
2. **Create milestones**: Consider creating GitHub milestones for each phase
3. **Use project boards**: GitHub Projects can help track progress
4. **Review dependencies**: Ensure dependency chains are correct before bulk creation
5. **Update as needed**: Issues can be edited after creation via GitHub UI

## Support

For questions about:
- **PRD content**: See PRD.md Section 22 (Engineering Hand-Off Checklist)
- **Issue creation**: Check this README
- **Implementation details**: Refer to specific AC in PRD.md

## Additional Resources

- [GitHub CLI Documentation](https://cli.github.com/manual/)
- [PyGithub Documentation](https://pygithub.readthedocs.io/)
- [GitHub API Documentation](https://docs.github.com/en/rest)
- [GitHub Issues Documentation](https://docs.github.com/en/issues)
