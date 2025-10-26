namespace SecureSpec.AspNetCore.UI;

/// <summary>
/// Provides static assets for the SecureSpec UI.
/// </summary>
public static class AssetProvider
{
    private static readonly Dictionary<string, string> _assets = new(StringComparer.OrdinalIgnoreCase);

    static AssetProvider()
    {
        // Register all static assets
        InitializeAssets();
    }

    /// <summary>
    /// Gets an asset by its path.
    /// </summary>
    /// <param name="path">The asset path (e.g., "assets/styles.css").</param>
    /// <returns>The asset content, or null if not found.</returns>
    public static string? GetAsset(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        // Normalize path (lowercase is appropriate for file paths)
#pragma warning disable CA1308 // Normalize strings to uppercase
        var normalizedPath = path.Replace('\\', '/').ToLowerInvariant();
#pragma warning restore CA1308

        return _assets.TryGetValue(normalizedPath, out var content) ? content : null;
    }

    /// <summary>
    /// Initializes all static assets.
    /// </summary>
    private static void InitializeAssets()
    {
        // Main stylesheet
        _assets["assets/styles.css"] = GetStylesContent();

        // Main application script
        _assets["assets/app.js"] = GetAppScriptContent();

        // Component modules
        _assets["assets/components/router.js"] = GetRouterContent();
        _assets["assets/components/state.js"] = GetStateManagerContent();
        _assets["assets/components/operation-display.js"] = GetOperationDisplayContent();
        _assets["assets/components/schema-viewer.js"] = GetSchemaViewerContent();
    }

    /// <summary>
    /// Gets the main stylesheet content.
    /// </summary>
    private static string GetStylesContent()
    {
        return @"/* SecureSpec UI Styles */
:root {
  --primary-color: #2563eb;
  --text-color: #1f2937;
  --background-color: #ffffff;
  --border-color: #e5e7eb;
  --hover-color: #f3f4f6;
  --code-background: #f9fafb;
}

* {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
  color: var(--text-color);
  background-color: var(--background-color);
  line-height: 1.6;
}

#securespec-ui {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

header {
  background-color: var(--primary-color);
  color: white;
  padding: 1.5rem 2rem;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

header h1 {
  font-size: 1.75rem;
  font-weight: 600;
}

#navigation {
  width: 250px;
  border-right: 1px solid var(--border-color);
  padding: 1rem;
  overflow-y: auto;
  background-color: var(--background-color);
}

main {
  flex: 1;
  padding: 2rem;
  overflow-y: auto;
}

.nav-item {
  padding: 0.5rem 0.75rem;
  cursor: pointer;
  border-radius: 0.375rem;
  transition: background-color 0.2s;
}

.nav-item:hover {
  background-color: var(--hover-color);
}

.nav-item.active {
  background-color: var(--primary-color);
  color: white;
}

.operation {
  margin-bottom: 2rem;
  border: 1px solid var(--border-color);
  border-radius: 0.5rem;
  overflow: hidden;
}

.operation-header {
  padding: 1rem;
  background-color: var(--code-background);
  cursor: pointer;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.operation-method {
  padding: 0.25rem 0.75rem;
  border-radius: 0.25rem;
  font-weight: 600;
  font-size: 0.875rem;
  text-transform: uppercase;
}

.method-get { background-color: #10b981; color: white; }
.method-post { background-color: #3b82f6; color: white; }
.method-put { background-color: #f59e0b; color: white; }
.method-delete { background-color: #ef4444; color: white; }
.method-patch { background-color: #8b5cf6; color: white; }

.operation-content {
  padding: 1.5rem;
  display: none;
}

.operation-content.expanded {
  display: block;
}

.schema-property {
  margin-left: 1.5rem;
  padding: 0.5rem;
  border-left: 2px solid var(--border-color);
}

.schema-type {
  color: var(--primary-color);
  font-weight: 500;
  font-family: 'Courier New', monospace;
}

code {
  background-color: var(--code-background);
  padding: 0.125rem 0.375rem;
  border-radius: 0.25rem;
  font-size: 0.875rem;
}

pre {
  background-color: var(--code-background);
  padding: 1rem;
  border-radius: 0.375rem;
  overflow-x: auto;
}

.loading {
  text-align: center;
  padding: 2rem;
  color: #6b7280;
}
";
    }

    /// <summary>
    /// Gets the main application script content.
    /// </summary>
    private static string GetAppScriptContent()
    {
        return @"// SecureSpec UI - Main Application
import { Router } from './components/router.js';
import { StateManager } from './components/state.js';
import { OperationDisplay } from './components/operation-display.js';
import { SchemaViewer } from './components/schema-viewer.js';

class SecureSpecApp {
  constructor() {
    this.config = this.loadConfig();
    this.state = new StateManager(this.config);
    this.router = new Router(this.state);
    this.operationDisplay = new OperationDisplay(this.state);
    this.schemaViewer = new SchemaViewer(this.state);
    
    this.initialize();
  }
  
  loadConfig() {
    const configElement = document.getElementById('ui-config');
    if (configElement) {
      try {
        return JSON.parse(configElement.textContent);
      } catch (e) {
        console.error('Failed to parse UI configuration:', e);
      }
    }
    return {};
  }
  
  async initialize() {
    console.log('Initializing SecureSpec UI...');
    console.log('Configuration:', this.config);
    
    // Initialize components
    await this.loadOpenAPIDocument();
    this.setupEventListeners();
    this.router.initialize();
    
    console.log('SecureSpec UI initialized successfully');
  }
  
  async loadOpenAPIDocument() {
    // TODO: Load OpenAPI document from endpoint
    // For now, display a placeholder
    const content = document.getElementById('content');
    if (content) {
      content.innerHTML = '<div class=""loading"">Loading API documentation...</div>';
    }
  }
  
  setupEventListeners() {
    // Setup hash change listener for deep linking
    if (this.config.deepLinking) {
      window.addEventListener('hashchange', () => {
        this.router.handleHashChange();
      });
    }
  }
}

// Initialize the application when DOM is ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => {
    window.secureSpecApp = new SecureSpecApp();
  });
} else {
  window.secureSpecApp = new SecureSpecApp();
}
";
    }

    /// <summary>
    /// Gets the router component content.
    /// </summary>
    private static string GetRouterContent()
    {
        return @"// SecureSpec UI - Router Component
export class Router {
  constructor(state) {
    this.state = state;
    this.routes = new Map();
    this.currentRoute = null;
  }
  
  initialize() {
    // Register routes
    this.registerRoute('/', this.handleHome.bind(this));
    this.registerRoute('/operation/:id', this.handleOperation.bind(this));
    this.registerRoute('/schema/:id', this.handleSchema.bind(this));
    
    // Handle initial route
    this.handleHashChange();
  }
  
  registerRoute(pattern, handler) {
    this.routes.set(pattern, handler);
  }
  
  handleHashChange() {
    const hash = window.location.hash.slice(1) || '/';
    this.navigate(hash);
  }
  
  navigate(path) {
    console.log('Navigating to:', path);
    
    // Find matching route
    for (const [pattern, handler] of this.routes) {
      const params = this.matchRoute(pattern, path);
      if (params !== null) {
        this.currentRoute = { path, pattern, params };
        handler(params);
        return;
      }
    }
    
    // No route matched, show 404
    this.handle404();
  }
  
  matchRoute(pattern, path) {
    const patternParts = pattern.split('/').filter(p => p);
    const pathParts = path.split('/').filter(p => p);
    
    if (patternParts.length !== pathParts.length) {
      return null;
    }
    
    const params = {};
    for (let i = 0; i < patternParts.length; i++) {
      if (patternParts[i].startsWith(':')) {
        const paramName = patternParts[i].slice(1);
        params[paramName] = decodeURIComponent(pathParts[i]);
      } else if (patternParts[i] !== pathParts[i]) {
        return null;
      }
    }
    
    return params;
  }
  
  handleHome(params) {
    const content = document.getElementById('content');
    if (content) {
      content.innerHTML = '<h2>Welcome to SecureSpec API Documentation</h2>';
    }
  }
  
  handleOperation(params) {
    const content = document.getElementById('content');
    if (content) {
      content.innerHTML = `<h2>Operation: ${params.id}</h2>`;
    }
  }
  
  handleSchema(params) {
    const content = document.getElementById('content');
    if (content) {
      content.innerHTML = `<h2>Schema: ${params.id}</h2>`;
    }
  }
  
  handle404() {
    const content = document.getElementById('content');
    if (content) {
      content.innerHTML = '<h2>404 - Page Not Found</h2>';
    }
  }
}
";
    }

    /// <summary>
    /// Gets the state manager component content.
    /// </summary>
    private static string GetStateManagerContent()
    {
        return @"// SecureSpec UI - State Manager Component
export class StateManager {
  constructor(config) {
    this.config = config;
    this.state = {
      openApiDocument: null,
      currentOperation: null,
      expandedOperations: new Set(),
      searchFilter: '',
      selectedDocument: null
    };
    this.listeners = new Map();
  }
  
  getState() {
    return { ...this.state };
  }
  
  setState(updates) {
    const prevState = { ...this.state };
    this.state = { ...this.state, ...updates };
    
    // Notify listeners
    this.notifyListeners(prevState, this.state);
  }
  
  subscribe(key, callback) {
    if (!this.listeners.has(key)) {
      this.listeners.set(key, []);
    }
    this.listeners.get(key).push(callback);
    
    // Return unsubscribe function
    return () => {
      const callbacks = this.listeners.get(key);
      const index = callbacks.indexOf(callback);
      if (index > -1) {
        callbacks.splice(index, 1);
      }
    };
  }
  
  notifyListeners(prevState, newState) {
    for (const [key, callbacks] of this.listeners) {
      if (prevState[key] !== newState[key]) {
        callbacks.forEach(callback => callback(newState[key], prevState[key]));
      }
    }
  }
  
  setOpenApiDocument(document) {
    this.setState({ openApiDocument: document });
  }
  
  setCurrentOperation(operationId) {
    this.setState({ currentOperation: operationId });
  }
  
  toggleOperation(operationId) {
    const expanded = new Set(this.state.expandedOperations);
    if (expanded.has(operationId)) {
      expanded.delete(operationId);
    } else {
      expanded.add(operationId);
    }
    this.setState({ expandedOperations: expanded });
  }
  
  setSearchFilter(filter) {
    this.setState({ searchFilter: filter });
  }
}
";
    }

    /// <summary>
    /// Gets the operation display component content.
    /// </summary>
    private static string GetOperationDisplayContent()
    {
        return @"// SecureSpec UI - Operation Display Component
export class OperationDisplay {
  constructor(state) {
    this.state = state;
  }
  
  render(operation) {
    const method = operation.method.toLowerCase();
    const methodClass = `method-${method}`;
    
    return `
      <div class=""operation"" data-operation-id=""${operation.operationId || ''}"">
        <div class=""operation-header"">
          <div>
            <span class=""operation-method ${methodClass}"">${method}</span>
            <strong>${operation.path}</strong>
          </div>
          <span>${operation.summary || ''}</span>
        </div>
        <div class=""operation-content"">
          <p>${operation.description || ''}</p>
          ${this.renderParameters(operation.parameters)}
          ${this.renderRequestBody(operation.requestBody)}
          ${this.renderResponses(operation.responses)}
        </div>
      </div>
    `;
  }
  
  renderParameters(parameters) {
    if (!parameters || parameters.length === 0) {
      return '';
    }
    
    return `
      <h3>Parameters</h3>
      <ul>
        ${parameters.map(p => `
          <li>
            <strong>${p.name}</strong> (${p.in}${p.required ? ', required' : ''}) 
            - <code>${p.schema?.type || 'object'}</code>
          </li>
        `).join('')}
      </ul>
    `;
  }
  
  renderRequestBody(requestBody) {
    if (!requestBody) {
      return '';
    }
    
    return `
      <h3>Request Body</h3>
      <p>${requestBody.description || ''}</p>
    `;
  }
  
  renderResponses(responses) {
    if (!responses) {
      return '';
    }
    
    return `
      <h3>Responses</h3>
      <ul>
        ${Object.entries(responses).map(([code, response]) => `
          <li>
            <strong>${code}</strong> - ${response.description || ''}
          </li>
        `).join('')}
      </ul>
    `;
  }
}
";
    }

    /// <summary>
    /// Gets the schema viewer component content.
    /// </summary>
    private static string GetSchemaViewerContent()
    {
        return @"// SecureSpec UI - Schema Viewer Component
export class SchemaViewer {
  constructor(state) {
    this.state = state;
  }
  
  render(schema, depth = 0) {
    if (!schema) {
      return '';
    }
    
    const maxDepth = this.state.config.defaultModelsExpandDepth || 1;
    
    if (depth > maxDepth) {
      return '<span class=""schema-collapsed"">...</span>';
    }
    
    return `
      <div class=""schema-property"" style=""margin-left: ${depth * 1.5}rem"">
        <span class=""schema-type"">${schema.type || 'object'}</span>
        ${this.renderProperties(schema.properties, depth)}
      </div>
    `;
  }
  
  renderProperties(properties, depth) {
    if (!properties) {
      return '';
    }
    
    return Object.entries(properties).map(([name, prop]) => `
      <div>
        <strong>${name}</strong>: ${this.renderProperty(prop, depth + 1)}
      </div>
    `).join('');
  }
  
  renderProperty(property, depth) {
    return `<span class=""schema-type"">${property.type || 'object'}</span>`;
  }
}
";
    }
}
