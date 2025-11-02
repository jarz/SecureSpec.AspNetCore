// SecureSpec UI - Router Component
import { sanitizeId, escapeHtml } from './utils.js';

export class Router {
  constructor(state, operationDisplay) {
    this.state = state;
    this.operationDisplay = operationDisplay;
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
    const hash = globalThis.location.hash.slice(1) || '/';
    this.navigate(hash);
  }

  navigate(path) {
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
    const patternParts = pattern.split('/').filter(Boolean);
    const pathParts = path.split('/').filter(Boolean);
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
    // Home route is handled by the main app rendering operations
  }

  handleOperation(params) {
    // AC 469: deepLinking enabled scrolls to anchor
    // AC 470: deepLinking disabled retains anchor but suppresses auto-scroll
    // AC 472: Hash fragment update triggers focus highlight
    const operationId = params.id;
    const config = this.state.config || {};

    // Find the operation element
    const sanitizedId = sanitizeId(operationId);
    const operationElement = document.getElementById(`operation-${sanitizedId}`);

    if (operationElement) {
      // Expand the operation if it's not already expanded
      const currentState = this.state.getState();
      if (!currentState.expandedOperations.has(operationId)) {
        this.state.toggleOperation(operationId);
      }

      // AC 469/470: Scroll to element if deepLinking is enabled (default true)
      if (config.deepLinking !== false) {
        operationElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }

      // AC 472: Add focus highlight
      this.highlightOperation(operationElement);
    }
  }

  handleSchema(params) {
    // Schema viewing can be implemented later
    const content = document.getElementById('content');
    if (content) {
      content.innerHTML = `<div class="schema-view"><h2>Schema: ${escapeHtml(params.id)}</h2></div>`;
    }
  }

  handle404() {
    const content = document.getElementById('content');
    if (content) {
      content.innerHTML = '<div class="not-found"><h2>404 - Page Not Found</h2></div>';
    }
  }

  /**
   * AC 472: Highlight an operation element when navigated to
   * @param {HTMLElement} element - The operation element to highlight
   */
  highlightOperation(element) {
    // Remove previous highlights
    const previousHighlights = document.querySelectorAll('.operation.highlighted');
    for (const highlighted of previousHighlights) {
      highlighted.classList.remove('highlighted');
    }

    // Add highlight class
    element.classList.add('highlighted');

    // Set focus for accessibility
    const header = element.querySelector('.operation-header');
    if (header) {
      header.focus();
    }

    // Remove highlight after animation
    setTimeout(() => {
      element.classList.remove('highlighted');
    }, 2000);
  }
}
