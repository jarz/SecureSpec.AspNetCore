// SecureSpec UI - State Manager Component
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
    return { 
      ...this.state, 
      expandedOperations: new Set(this.state.expandedOperations)
    };
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
