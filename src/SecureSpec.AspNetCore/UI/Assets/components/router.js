// SecureSpec UI - Router Component
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
