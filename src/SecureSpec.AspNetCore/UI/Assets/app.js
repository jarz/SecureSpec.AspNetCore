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
        // Configuration parsing failed - use defaults
      }
    }
    return {};
  }
  
  async initialize() {
    // Initialize components
    await this.loadOpenAPIDocument();
    this.setupEventListeners();
    this.router.initialize();
  }
  
  async loadOpenAPIDocument() {
    // TODO: Load OpenAPI document from endpoint
    // For now, display a placeholder
    const content = document.getElementById('content');
    if (content) {
      content.innerHTML = '<div class="loading">Loading API documentation...</div>';
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
