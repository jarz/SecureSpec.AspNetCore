#!/bin/bash
# Script to create GitHub issues from the SecureSpec.AspNetCore PRD
# Usage: ./create-issues.sh [--dry-run]

set -e

REPO="jarz/SecureSpec.AspNetCore"
DRY_RUN=false

if [[ "$1" == "--dry-run" ]]; then
  DRY_RUN=true
  echo "Running in DRY RUN mode - no issues will be created"
fi

# Check if gh is installed
if ! command -v gh &> /dev/null; then
  echo "Error: GitHub CLI (gh) is not installed"
  echo "Install it from: https://cli.github.com/"
  exit 1
fi

# Check if authenticated
if ! gh auth status &> /dev/null; then
  echo "Error: Not authenticated with GitHub CLI"
  echo "Run: gh auth login"
  exit 1
fi

create_issue() {
  local title="$1"
  local body="$2"
  local labels="$3"
  local milestone="$4"
  
  if [ "$DRY_RUN" = true ]; then
    echo "Would create issue: $title"
    echo "  Labels: $labels"
    echo "  Milestone: $milestone"
    echo ""
    return
  fi
  
  echo "Creating issue: $title"
  gh issue create \
    --repo "$REPO" \
    --title "$title" \
    --body "$body" \
    --label "$labels" \
    ${milestone:+--milestone "$milestone"} || echo "Failed to create issue: $title"
  
  sleep 1  # Rate limiting
}

# Phase 1 Issues
echo "Creating Phase 1 issues..."

create_issue \
  "Phase 1.1: Implement Canonical Serializer with Deterministic Hash Generation" \
  "$(cat <<'EOF'
## Description
Implement the canonical JSON/YAML serializer that produces deterministic output with stable SHA256 hashes across rebuilds.

## Acceptance Criteria
- AC 1-10: Canonical serialization with UTF-8, LF endings, no BOM, normalized whitespace
- AC 19-21: SHA256 hash generation and ETag format (W/"sha256:<first16hex>")
- AC 499: SHA256 hashing after normalization (LF, UTF-8)
- Component arrays sorted lexically (AC 493)
- Numeric serialization locale invariance (AC 45)

## Estimated Effort
3-5 days

## Dependencies
None

## Technical Notes
- Must be deterministic across rebuilds
- Support both JSON and YAML formats
- Implement proper canonicalization before hashing
- Generate weak ETags with SHA256 hash prefix
EOF
)" \
  "phase-1,core,serialization,priority-critical" \
  "Phase 1"

create_issue \
  "Phase 1.2: Implement SchemaId Strategy with Collision Handling" \
  "$(cat <<'EOF'
## Description
Implement SchemaId generation strategy with deterministic collision suffix handling (_schemaDup{N}).

## Acceptance Criteria
- AC 401: SchemaId generic naming deterministic
- AC 402: Collision applies `_schemaDup{N}` suffix starting at 1
- AC 403: Collision suffix numbering stable across rebuilds
- AC 404: SchemaId strategy override applied before collision detection
- AC 405: Collision diagnostic SCH001 emitted per duplicate
- AC 406: Generic nested types canonical form `Outer«Inner»`
- AC 407: Nullable generic arguments retain canonical ordering
- AC 408: Removing type reclaims suffix sequence deterministically

## Estimated Effort
3-4 days

## Dependencies
- Issue 1.1: Canonical Serializer

## Technical Notes
- Default pattern: `Namespace.TypeName`
- Generics expand to canonical form
- Collision suffix is deterministic
- Same input set yields identical SchemaIds across rebuilds
EOF
)" \
  "phase-1,core,schema,priority-critical" \
  "Phase 1"

create_issue \
  "Phase 1.3: Implement CLR Primitive Type Mapping" \
  "$(cat <<'EOF'
## Description
Implement complete CLR primitive to OpenAPI type mapping with all standard .NET types.

## Acceptance Criteria
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

## Estimated Effort
2-3 days

## Dependencies
- Issue 1.2: SchemaId Strategy

## Technical Notes
- Support both OpenAPI 3.0 and 3.1
- Handle all built-in .NET types
- Proper format strings
- Nullable handling differs between versions
EOF
)" \
  "phase-1,core,schema,type-mapping,priority-critical" \
  "Phase 1"

create_issue \
  "Phase 1.4: Implement Nullability Semantics (OpenAPI 3.0 & 3.1)" \
  "$(cat <<'EOF'
## Description
Implement nullability representation for both OpenAPI 3.0 (nullable: true) and 3.1 (union types).

## Acceptance Criteria
- AC 420: Reference type optional absent nullable:true in 3.0
- AC 421: Reference type nullable emits nullable:true (3.0) / union (3.1)
- AC 422: Array nullable means array may be null
- AC 423: Nullable items don't require array union
- AC 424: Dictionary value nullable represented inside additionalProperties
- AC 425: OneOf variant nullable with separate variant or union (3.1)
- AC 426: Mixed nullable inside AllOf retains original union semantics

## Estimated Effort
2-3 days

## Dependencies
- Issue 1.3: CLR Primitive Type Mapping

## Technical Notes
- OpenAPI 3.0: Use `nullable: true`
- OpenAPI 3.1: Use union types `[type, "null"]`
- Handle nullable reference types (NRT)
- Distinguish nullable array vs nullable items
EOF
)" \
  "phase-1,core,schema,nullability,priority-critical" \
  "Phase 1"

create_issue \
  "Phase 1.5: Implement Recursion Detection and Depth Limits" \
  "$(cat <<'EOF'
## Description
Implement cycle detection and maximum traversal depth (default 32) with proper diagnostics.

## Acceptance Criteria
- AC 427: Max depth constant enforces cut-off at level 32
- AC 428: Depth exceed logs SCH001-DEPTH diagnostic
- AC 429: Cycle detection prevents infinite traversal
- AC 430: Multiple cycles produce single placeholder per cycle root
- AC 431: Depth change recalculates schema traversal deterministically

## Estimated Effort
2 days

## Dependencies
- Issue 1.2: SchemaId Strategy

## Technical Notes
- Use visited identity set for cycle detection
- First revisit produces placeholder `<recursive…>`
- Max depth configurable (default 32)
- Proper diagnostic logging
EOF
)" \
  "phase-1,core,schema,safety,priority-high" \
  "Phase 1"

create_issue \
  "Phase 1.6: Implement Dictionary and AdditionalProperties Handling" \
  "$(cat <<'EOF'
## Description
Implement Dictionary<string,T> mapping to OpenAPI additionalProperties with constraint support.

## Acceptance Criteria
- AC 432: Dictionary emits additionalProperties referencing value schema
- AC 433: DataAnnotations on value type applied inside additionalProperties
- AC 434: Conflict between explicit property and dictionary key logs ANN001
- AC 435: Header/value Unicode normalized before constraint evaluation
- AC 436: Dictionary value schema keys lexical ordering
- AC 437: additionalProperties:false blocks extension injection attempts

## Estimated Effort
2 days

## Dependencies
- Issue 1.3: CLR Primitive Type Mapping

## Technical Notes
- Dictionary<string,T> → type: object with additionalProperties
- Apply value type constraints inside additionalProperties
- Detect and log conflicts with explicit properties
EOF
)" \
  "phase-1,core,schema,priority-high" \
  "Phase 1"

create_issue \
  "Phase 1.7: Implement DataAnnotations Ingestion" \
  "$(cat <<'EOF'
## Description
Implement ingestion of DataAnnotations attributes with conflict detection.

## Acceptance Criteria
- AC 31-40: DataAnnotations mapping (Required, Range, MinLength, MaxLength, StringLength, RegularExpression)
- AC 433: DataAnnotations conflict detection
- Proper diagnostic logging (ANN001)

## Estimated Effort
2-3 days

## Dependencies
- Issue 1.3: CLR Primitive Type Mapping

## Technical Notes
- Map Required → required array
- Map Range → minimum/maximum
- Map MinLength/MaxLength/StringLength → minLength/maxLength
- Map RegularExpression → pattern
- Detect conflicts and log with ANN001
EOF
)" \
  "phase-1,core,schema,validation,priority-high" \
  "Phase 1"

create_issue \
  "Phase 1.8: Implement Enum Advanced Behavior" \
  "$(cat <<'EOF'
## Description
Implement enum handling with virtualization for large enums (>10K values) and proper ordering.

## Acceptance Criteria
- AC 438: Enum declaration order stable across rebuilds
- AC 439: Enum switching integer→string toggles representation
- AC 440: Enum >10K triggers virtualization + VIRT001 diagnostic
- AC 441: Enum search returns results across virtualized segments
- AC 442: Enum naming policy modifies emitted value casing
- AC 443: Enum nullable adds "null" union in 3.1 only

## Estimated Effort
2-3 days

## Dependencies
- Issue 1.3: CLR Primitive Type Mapping

## Technical Notes
- Preserve declaration order
- Support both string and integer representation
- Virtualize large enums (>10K values)
- Implement search across virtualized segments
EOF
)" \
  "phase-1,core,schema,enums,priority-high" \
  "Phase 1"

# Phase 2 Issues
echo "Creating Phase 2 issues..."

create_issue \
  "Phase 2.1: Implement HTTP Bearer Security Scheme" \
  "$(cat <<'EOF'
## Description
Implement HTTP Bearer token authentication scheme without Basic auth inference.

## Acceptance Criteria
- AC 189-195: HTTP Bearer implementation
- AC 221: Basic auth inference blocked with diagnostic AUTH001
- Proper header sanitization

## Estimated Effort
1-2 days

## Dependencies
- Issue 1.1: Canonical Serializer

## Technical Notes
- No Basic auth inference (security hardening)
- Proper Authorization header handling
- Sanitize all header values
EOF
)" \
  "phase-2,security,authentication,priority-critical" \
  "Phase 2"

create_issue \
  "Phase 2.2: Implement API Key Security Schemes (Header & Query)" \
  "$(cat <<'EOF'
## Description
Implement API Key authentication in both header and query parameter locations with name sanitization.

## Acceptance Criteria
- AC 196-198: API Key header and query implementation
- Name sanitization and validation
- Proper diagnostic logging

## Estimated Effort
1-2 days

## Dependencies
- Issue 1.1: Canonical Serializer

## Technical Notes
- Support both header and query locations
- Sanitize custom key names
- Validate key name formats
EOF
)" \
  "phase-2,security,authentication,priority-critical" \
  "Phase 2"

create_issue \
  "Phase 2.3: Implement OAuth Authorization Code Flow with PKCE" \
  "$(cat <<'EOF'
## Description
Implement OAuth 2.0 Authorization Code flow with required PKCE (code challenge/verifier).

## Acceptance Criteria
- AC 199-208: Authorization Code flow with PKCE
- AC 431-435: PKCE auto code challenge/verifier generation
- CSRF double-submit and rotation (AC 199-200)
- Token exchange and refresh handling

## Estimated Effort
3-5 days

## Dependencies
- Issue 1.1: Canonical Serializer

## Technical Notes
- PKCE is REQUIRED (security hardening)
- Auto-generate code challenge and verifier
- Implement CSRF protection
- Handle token refresh flows
EOF
)" \
  "phase-2,security,oauth,priority-critical" \
  "Phase 2"

create_issue \
  "Phase 2.4: Implement OAuth Client Credentials Flow" \
  "$(cat <<'EOF'
## Description
Implement OAuth 2.0 Client Credentials flow with scope support.

## Acceptance Criteria
- AC 209-213: Client Credentials flow
- Scoped client authentication
- Token management

## Estimated Effort
2-3 days

## Dependencies
- Issue 1.1: Canonical Serializer

## Technical Notes
- Support scope-based access control
- Proper client authentication
- Token caching and management
EOF
)" \
  "phase-2,security,oauth,priority-high" \
  "Phase 2"

create_issue \
  "Phase 2.5: Implement Mutual TLS Security Scheme" \
  "$(cat <<'EOF'
## Description
Implement Mutual TLS scheme display (display only, no cert upload).

## Acceptance Criteria
- AC 214-216: Mutual TLS display
- No certificate upload capability
- Documentation on external cert management

## Estimated Effort
1 day

## Dependencies
- Issue 1.1: Canonical Serializer

## Technical Notes
- Display only (no interactive cert upload for security)
- Document external certificate management
- Show mTLS configuration requirements
EOF
)" \
  "phase-2,security,authentication,priority-medium" \
  "Phase 2"

create_issue \
  "Phase 2.6: Implement Security Requirement AND/OR Semantics" \
  "$(cat <<'EOF'
## Description
Implement security requirement logic: AND within requirement, OR across objects.

## Acceptance Criteria
- AC 217-220: Security AND/OR semantics
- Proper requirement evaluation
- Clear documentation and examples

## Estimated Effort
2 days

## Dependencies
- Issues 2.1-2.5: Security schemes

## Technical Notes
- AND: All schemes in a requirement must be satisfied
- OR: At least one requirement object must be satisfied
- Proper boolean logic evaluation
EOF
)" \
  "phase-2,security,logic,priority-high" \
  "Phase 2"

create_issue \
  "Phase 2.7: Implement Per-Operation Security Overrides" \
  "$(cat <<'EOF'
## Description
Implement per-operation security array that overrides global when present (no merge).

## Acceptance Criteria
- AC 464: Operation-level security present overrides global
- AC 465: Empty operation security array clears global requirements
- AC 466: Security arrays ordering lexical by scheme key
- AC 467: Multiple operation security objects preserve declaration order
- AC 468: Operation security mutation logged

## Estimated Effort
2 days

## Dependencies
- Issues 2.1-2.6: Security implementation

## Technical Notes
- No merge behavior (override only)
- Empty array explicitly clears global
- Deterministic ordering
- Proper mutation logging
EOF
)" \
  "phase-2,security,operations,priority-high" \
  "Phase 2"

create_issue \
  "Phase 2.8: Implement Policy and Role to Scope Mappings" \
  "$(cat <<'EOF'
## Description
Implement PolicyToScope and RoleToScope mapping hooks with diagnostics.

## Acceptance Criteria
- AC 222-223: Policy/Role mapping hooks
- Diagnostic logging (POL001, ROLE001)
- Configuration examples

## Estimated Effort
2 days

## Dependencies
- Issue 2.6: Security requirement semantics

## Technical Notes
- Configurable mapping functions
- Map ASP.NET Core policies/roles to OAuth scopes
- Diagnostic logging for applied mappings
EOF
)" \
  "phase-2,security,authorization,priority-medium" \
  "Phase 2"

echo ""
echo "Issue creation complete!"
echo "Review created issues at: https://github.com/$REPO/issues"
echo ""
echo "Note: This script only creates Phase 1 and Phase 2 issues."
echo "See docs/ISSUES.md for the complete list of all phases."
echo "Additional issues for Phases 3-6 should be created following the same pattern."
