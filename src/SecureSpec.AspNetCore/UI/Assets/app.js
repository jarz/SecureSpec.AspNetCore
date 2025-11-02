// SecureSpec UI - Main Application
import { Router } from './components/router.js';
import { StateManager } from './components/state.js';
import { OperationDisplay } from './components/operation-display.js';
import { SchemaViewer } from './components/schema-viewer.js';

function loadConfig() {
  const configElement = document.getElementById('ui-config');
  if (!configElement) {
    return {};
  }

  try {
    return JSON.parse(configElement.textContent);
  } catch (error) {
    // Configuration parsing failed - use defaults
    // eslint-disable-next-line no-console
    console.warn('SecureSpec UI configuration is not valid JSON, falling back to defaults.', error);
    return {};
  }
}

function createSecureSpecApp() {
  const config = loadConfig();
  const state = new StateManager(config);
  const router = new Router(state);
  const operationDisplay = new OperationDisplay(state);
  const schemaViewer = new SchemaViewer(state);

  const setupEventListeners = () => {
    if (config.deepLinking) {
      globalThis.addEventListener('hashchange', () => {
        router.handleHashChange();
      });
    }
  };

  const loadOpenAPIDocument = async () => {
    const content = document.getElementById('content');
    if (content) {
      content.innerHTML = '<div class="loading">Loading API documentation...</div>';
    }
  };

  const initialize = async () => {
    await loadOpenAPIDocument();
    setupEventListeners();
    router.initialize();
  };

  return {
    config,
    state,
    router,
    operationDisplay,
    schemaViewer,
    initialize
  };
}

async function bootstrapApp() {
  const app = createSecureSpecApp();
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

// Initialize the application when DOM is ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', attachApp);
} else {
  attachApp();
}
