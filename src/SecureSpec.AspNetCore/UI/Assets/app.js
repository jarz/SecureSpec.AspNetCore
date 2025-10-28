// SecureSpec UI - Main Application
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
    // Initialize components
    await this.loadOpenAPIDocument();
    this.setupEventListeners();
    this.setupStateSubscriptions();
    this.router.initialize();
  }
  
  async loadOpenAPIDocument() {
    try {
      // Try to load OpenAPI document from the first available endpoint
      // This should match the configured document names
      const response = await fetch('/openapi/v1.json');
      
      if (response.ok) {
        const document = await response.json();
        this.state.setOpenApiDocument(document);
        this.operationDisplay.loadOperations(document);
        this.renderOperations();
      } else {
        // Fallback: show placeholder
        this.showPlaceholder();
      }
    } catch (error) {
      console.error('Failed to load OpenAPI document:', error);
      this.showPlaceholder();
    }
  }

  showPlaceholder() {
    const content = document.getElementById('content');
    if (content) {
      content.innerHTML = `
        <div class="loading">
          <h2>SecureSpec API Documentation</h2>
          <p>Loading API documentation...</p>
          <p class="hint">Make sure your API is properly configured with SecureSpec.</p>
        </div>
      `;
    }
  }

  renderOperations() {
    const content = document.getElementById('content');
    if (!content) return;

    const filter = this.state.getState().searchFilter || '';
    const html = this.operationDisplay.renderAll(filter);
    content.innerHTML = html;

    // Attach event listeners to operation headers and tag headers
    this.attachOperationListeners();
  }

  attachOperationListeners() {
    // Tag header click handlers
    const tagHeaders = document.querySelectorAll('.tag-header');
    tagHeaders.forEach(header => {
      header.addEventListener('click', (e) => {
        const tagName = e.currentTarget.closest('.tag-group').dataset.tag;
        this.state.toggleTag(tagName);
      });
    });

    // Operation header click handlers
    const operationHeaders = document.querySelectorAll('.operation-header');
    operationHeaders.forEach(header => {
      header.addEventListener('click', (e) => {
        const operationId = e.currentTarget.closest('.operation').dataset.operationId;
        this.state.toggleOperation(operationId);
      });
    });
  }

  setupStateSubscriptions() {
    // Re-render when expanded operations change
    this.state.subscribe('expandedOperations', () => {
      this.renderOperations();
    });

    // Re-render when expanded tags change
    this.state.subscribe('expandedTags', () => {
      this.renderOperations();
    });

    // Re-render when search filter changes
    this.state.subscribe('searchFilter', () => {
      this.renderOperations();
    });
  }
  
  setupEventListeners() {
    // Setup hash change listener for deep linking
    if (this.config.deepLinking) {
      window.addEventListener('hashchange', () => {
        this.router.handleHashChange();
      });
    }

    // Setup filter input if filtering is enabled
    if (this.config.enableFiltering) {
      this.setupFilterInput();
    }
  }

  setupFilterInput() {
    // Create filter input in navigation area
    const nav = document.getElementById('navigation');
    if (!nav) return;

    const filterContainer = document.createElement('div');
    filterContainer.className = 'filter-container';
    filterContainer.innerHTML = `
      <input 
        type="text" 
        id="operation-filter" 
        class="filter-input" 
        placeholder="Filter operations..."
        aria-label="Filter operations"
      />
    `;
    nav.insertBefore(filterContainer, nav.firstChild);

    const filterInput = document.getElementById('operation-filter');
    if (filterInput) {
      // Debounce filter input
      let debounceTimer;
      filterInput.addEventListener('input', (e) => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
          this.state.setSearchFilter(e.target.value);
        }, 150); // 150ms debounce as per AC 484
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
