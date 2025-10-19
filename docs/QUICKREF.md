# Quick Reference: Creating Issues from PRD

## TL;DR

```bash
# Option 1: Using Python (Recommended)
cd docs
pip install PyGithub
export GITHUB_TOKEN=your_token_here
python create-issues.py

# Option 2: Using GitHub CLI
cd docs
gh auth login
./create-issues.sh
```

## What Gets Created

**54 GitHub issues** organized into:
- 6 implementation phases (Phases 1-6)
- 5 cross-cutting concerns
- All mapped to 500 acceptance criteria from PRD.md

## Files Overview

| File | Size | Purpose |
|------|------|---------|
| `ISSUES.md` | 30 KB | Human-readable issue descriptions |
| `issues.json` | 30 KB | Machine-readable issue data |
| `create-issues.py` | 6 KB | Python script (uses GitHub API) |
| `create-issues.sh` | 15 KB | Bash script (uses GitHub CLI) |
| `README.md` | 7.5 KB | Complete documentation |
| `SUMMARY.md` | 6.5 KB | Statistics and overview |
| `PRD.md` | 37 KB | Original requirements document |

## Issue Breakdown by Phase

```
Phase 1: Core OpenAPI Generation        →  8 issues (Weeks 1-2)
Phase 2: Security & OAuth              →  8 issues (Weeks 3-4)
Phase 3: UI & Interaction              → 10 issues (Weeks 5-6)
Phase 4: Performance & Virtualization  →  7 issues (Week 7)
Phase 5: Diagnostics & Concurrency     →  6 issues (Week 8)
Phase 6: Accessibility & Hardening     → 10 issues (Week 9)
Cross-Cutting: Tools, Tests, Docs      →  5 issues (Ongoing)
                                          ─────────────────
                                Total:    54 issues
```

## Priority Distribution

- **Critical** (20): Blocking, core features
- **High** (24): Important features
- **Medium** (9): Nice to have
- **Low** (1): Optional

## Commands Cheat Sheet

### Python Script
```bash
# Install dependencies
pip install PyGithub

# Create token at: https://github.com/settings/tokens
# Required scopes: repo (all permissions)

# Dry run (preview)
python create-issues.py --dry-run

# Create issues
export GITHUB_TOKEN=ghp_...
python create-issues.py

# Different repo
python create-issues.py --repo owner/repo
```

### GitHub CLI
```bash
# Install: https://cli.github.com/
# Authenticate
gh auth login

# Dry run (preview)
./create-issues.sh --dry-run

# Create issues (Phase 1 & 2 only)
./create-issues.sh
```

### Manual Editing
```bash
# Edit issue definitions
vim issues.json

# Validate JSON
python -m json.tool issues.json

# Re-run script to create
python create-issues.py
```

## Key Features Covered

### ✅ Security
- Bearer, API Key, OAuth 2.0 (PKCE), mTLS
- CSP, Integrity (SHA256+SRI), Sanitization

### ✅ OpenAPI
- Canonical serialization, SchemaId, Types
- Nullability, Recursion, DataAnnotations
- Polymorphism, Examples, XML docs

### ✅ UI
- Framework, Operations, Try It Out
- Models, Search, Deep linking

### ✅ Performance
- Virtualization, Throttling, Caching
- WASM sandbox, Thread safety

### ✅ Quality
- Diagnostics, Rate limiting
- Tests, Docs, Monitoring
- WCAG 2.1 AA accessibility

## Common Issues & Solutions

**Problem**: "PyGithub not installed"
```bash
pip install PyGithub
```

**Problem**: "Not authenticated with GitHub CLI"
```bash
gh auth login
```

**Problem**: Rate limited
- Wait 60 seconds (automatic retry)
- Script includes 1s delay between requests

**Problem**: Token has no permissions
- Create new token with `repo` scope
- https://github.com/settings/tokens

## Best Practices

1. ✅ Always dry-run first: `--dry-run`
2. ✅ Create milestones for each phase
3. ✅ Use GitHub Projects to track
4. ✅ Review ISSUES.md before creating
5. ✅ Start with Phase 1 critical issues

## Critical Path

```
Phase 1.1-1.3 → Phase 2.1-2.4 → Phase 3.1-3.4 → Phase 4.5 → Phase 6.2-6.3
  (Week 1)        (Week 3)         (Week 5)      (Week 7)      (Week 9)
Serializer      Security         UI + Try It   WASM          CSP +
+ SchemaId      + OAuth          Out           Sandbox       Integrity
```

## Success Criteria

- ✓ All 54 issues created
- ✓ All 500 AC from PRD met
- ✓ All tests passing
- ✓ WCAG 2.1 AA compliant
- ✓ Documentation complete
- ✓ Performance targets met

## Support

- **Full Docs**: See `README.md`
- **Issue Details**: See `ISSUES.md`
- **Statistics**: See `SUMMARY.md`
- **Requirements**: See `PRD.md`

---

**Need help?** Check `README.md` for detailed instructions and troubleshooting.
