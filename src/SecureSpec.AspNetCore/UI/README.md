# SecureSpec UI Framework

This directory contains the SecureSpec UI framework implementation, providing an interactive web-based interface for exploring OpenAPI documentation.

## Architecture

The UI framework follows a modular, component-based architecture with strict security controls.

### Components

#### `SecureSpecUIMiddleware`
ASP.NET Core middleware that serves the SecureSpec UI and handles routing.

**Features:**
- Configurable route prefix (default: `/securespec`)
- Strict Content Security Policy (CSP) headers
- Static asset delivery
- Integration with UIOptions configuration

**Security Headers:**
- `Content-Security-Policy`: Strict policy with no unsafe-eval
- `X-Content-Type-Options`: nosniff
- `X-Frame-Options`: DENY
- `Referrer-Policy`: no-referrer

#### `UITemplateGenerator`
Generates the main HTML page with embedded configuration.

**Features:**
- Server-side HTML generation
- XSS prevention through HTML escaping
- Configuration injection as JSON
- Locale-invariant rendering

#### `AssetProvider`
In-memory static asset management system.

**Features:**
- Pre-loaded assets for optimal performance
- Case-insensitive path matching
- Organized asset structure:
  - `assets/styles.css` - Main stylesheet
  - `assets/app.js` - Main application script
  - `assets/components/router.js` - Routing component
  - `assets/components/state.js` - State management
  - `assets/components/operation-display.js` - Operation rendering
  - `assets/components/schema-viewer.js` - Schema visualization
  - `assets/components/links-callbacks.js` - Links and Callbacks display (Phase 3.10)

#### `SecureSpecUIExtensions`
Extension methods for easy middleware registration.

**Usage:**
```csharp
// Simple usage
app.UseSecureSpecUI();

// With custom route prefix
app.UseSecureSpecUI("api-docs");

// With configuration
app.UseSecureSpecUI(options =>
{
    options.DocumentTitle = "My API";
    options.DeepLinking = true;
});
```

## JavaScript Architecture

### Module Structure

The UI uses ES6 modules for clean separation of concerns:

```
assets/
├── app.js                      # Main application entry point
├── styles.css                  # Base styles
└── components/
    ├── router.js               # Hash-based routing
    ├── state.js                # Centralized state management
    ├── operation-display.js    # Operation component
    ├── schema-viewer.js        # Schema component
    └── links-callbacks.js      # Links and Callbacks component
```

### Router Component

Implements hash-based routing for deep linking support:
- Pattern matching with parameter extraction
- Route registration and navigation
- Hash change handling

### State Manager

Provides centralized state management:
- Immutable state updates
- Pub/sub pattern for reactivity
- Configuration management

### Component System

Each component follows a consistent pattern:
- Constructor receives state manager
- `render()` method returns HTML
- Event handling through state updates

#### Links and Callbacks Component

The `LinksCallbacksDisplay` component provides read-only rendering of OpenAPI Links and Callbacks with comprehensive edge case handling (AC 493-497):

**Links Rendering:**
- Displays links in operation responses
- Resolves operationId or operationRef references
- Detects circular link references (LNK001 diagnostic)
- Handles missing references gracefully (LNK002, LNK003)
- Safely omits broken $ref references (LNK004)

**Callbacks Rendering:**
- Displays callbacks as read-only sections (CBK001)
- Shows webhook URLs and HTTP methods
- No Try It Out functionality (by design)
- Handles broken $ref references (CBK002)

**Edge Cases:**
- AC 493: Circular link detection logs diagnostic and inserts placeholder
- AC 494: Missing operationId but valid operationRef uses operationRef only
- AC 495: Missing both operationId & operationRef logs warning and renders stub
- AC 496: Callback section read-only (no Try It Out) logged informational
- AC 497: Broken $ref in link emits error and omits broken reference safely

## Configuration

The UI respects all UIOptions settings:

```csharp
options.UI.DocumentTitle = "API Documentation";
options.UI.DeepLinking = true;
options.UI.DisplayOperationId = true;
options.UI.DefaultModelsExpandDepth = 2;
options.UI.EnableFiltering = true;
options.UI.EnableTryItOut = true;
```

These options are:
1. Rendered into the HTML template
2. Embedded as JSON in a script tag
3. Loaded by the JavaScript application
4. Used to configure UI behavior

## Security

### Content Security Policy

The UI enforces a strict CSP:
- `default-src 'none'` - Deny by default
- `script-src 'self'` - Only local scripts
- `style-src 'self' 'unsafe-inline'` - Local styles + inline for dynamic styling
- `img-src 'self' data:` - Local images + data URIs
- `font-src 'self'` - Local fonts only
- `connect-src 'self'` - API calls to same origin only

### XSS Prevention

- All user-provided content is HTML-escaped
- No `eval()` or `Function()` usage
- No inline event handlers
- Script injection prevented through CSP

### WASM Sandbox Integration

Future implementation will include:
- WASM-based request execution
- Memory and CPU limits
- Network isolation
- No DOM access from sandbox

## Testing

Comprehensive test coverage includes:
- Middleware behavior tests (11 tests)
- Template generation tests (9 tests)
- Asset provider tests (13 tests)
- Extension method tests (6 tests)
- Diagnostic codes tests (34 tests)
- Links and Callbacks tests (17 tests)

All tests validate:
- Null argument handling
- Security header presence
- Content generation
- Configuration application
- Error handling
- Edge case handling for Links and Callbacks

## Performance

The UI is optimized for performance:
- In-memory asset storage (no file I/O)
- Minimal CSS and JavaScript
- Efficient routing (hash-based, no server round-trips)
- Lazy component rendering
- No external dependencies

## Browser Compatibility

The UI uses modern web standards:
- ES6 modules
- CSS custom properties
- Flexbox layout
- Async/await
- Fetch API

Supports:
- Chrome/Edge 90+
- Firefox 88+
- Safari 14+

## Accessibility

The UI follows WCAG 2.1 AA guidelines:
- Semantic HTML structure
- ARIA labels and roles (to be implemented in Phase 3.2+)
- Keyboard navigation support
- Focus management
- Screen reader compatibility

## Future Enhancements

Completed in Phase 3.10:
- ✅ Links and Callbacks display with edge case handling

Planned for subsequent phases:
- Operation display and navigation (Phase 3.2)
- Schema models panel (Phase 3.3)
- Try It Out functionality with WASM sandbox (Phase 3.4)
- Rate limiting integration (Phase 4)
- Diagnostics display (Phase 5)
