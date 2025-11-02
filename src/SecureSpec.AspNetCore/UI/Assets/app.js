// SecureSpec UI - Main Application
import { Router } from './components/router.js';
import { StateManager } from './components/state.js';
import { OperationDisplay } from './components/operation-display.js';
import { SchemaViewer } from './components/schema-viewer.js';

class SecureSpecApp {
  constructor() {
    this.config = this.loadConfig();
    this.state = new StateManager(this.config);
    this.operationDisplay = new OperationDisplay(this.state);
    this.schemaViewer = new SchemaViewer(this.state);
    this.router = new Router(this.state, this.operationDisplay);
  }

  loadConfig() {
    const configElement = document.getElementById('ui-config');
    if (configElement) {
      try {
        return JSON.parse(configElement.textContent);
      } catch (error) {
        // eslint-disable-next-line no-console
        console.warn('SecureSpec UI configuration is not valid JSON, falling back to defaults.', error);
      }
    }

    return {};
  }

  async initialize() {
    await this.loadOpenAPIDocument();
    this.setupEventListeners();
    this.setupStateSubscriptions();
    this.router.initialize();
  }

  async loadOpenAPIDocument() {
    try {
      const response = await fetch('/openapi/v1.json');

      if (response.ok) {
        const document = await response.json();
        this.state.setOpenApiDocument(document);
        this.operationDisplay.loadOperations(document);
        this.renderOperations();
      } else {
        this.showPlaceholder();
      }
    } catch (error) {
      // eslint-disable-next-line no-console
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
    if (!content) {
      return;
    }

    const filter = this.state.getState().searchFilter || '';
    const html = this.operationDisplay.renderAll(filter);
    content.innerHTML = html;

    this.attachOperationListeners();
  }

  attachOperationListeners() {
    const tagHeaders = document.querySelectorAll('.tag-header');
    for (const header of tagHeaders) {
      header.addEventListener('click', event => {
        const tagName = event.currentTarget.closest('.tag-group').dataset.tag;
        this.state.toggleTag(tagName);
      });
    }

    const operationHeaders = document.querySelectorAll('.operation-header');
    for (const header of operationHeaders) {
      header.addEventListener('click', event => {
        const operationId = event.currentTarget.closest('.operation').dataset.operationId;
        this.state.toggleOperation(operationId);
      });
    }
  }

  setupStateSubscriptions() {
    const rerender = () => {
      this.renderOperations();
    };

    this.state.subscribe('expandedOperations', rerender);
    this.state.subscribe('expandedTags', rerender);
    this.state.subscribe('searchFilter', rerender);
  }

  setupEventListeners() {
    if (this.config.deepLinking) {
      globalThis.addEventListener('hashchange', () => {
        this.router.handleHashChange();
      });
    }

    if (this.config.enableFiltering) {
      this.setupFilterInput();
    }
  }

  setupFilterInput() {
    const nav = document.getElementById('navigation');
    // Defensive check: Prevents duplicate filter inputs if setupFilterInput() is called more than once.
    // This ensures idempotency.
    if (!nav || nav.querySelector('.filter-container')) {
      return;
    }

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
      let debounceTimer;
      filterInput.addEventListener('input', event => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
          this.state.setSearchFilter(event.target.value);
        }, 150);
      });
    }
  }
}

async function bootstrapApp() {
  const app = new SecureSpecApp();
  await app.initialize();
  return app;
}

const attachApp = () => {
  bootstrapApp()
    .then(instance => {
      globalThis.secureSpecApp = instance;
    })
    .catch(error => {
      // eslint-disable-next-line no-console
      console.error('SecureSpec UI failed to initialize', error);
    });
};

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', attachApp);
} else {
  attachApp();
}
