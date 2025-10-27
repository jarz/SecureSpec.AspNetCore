// SecureSpec UI - Schema Viewer Component
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
      return '<span class="schema-collapsed">...</span>';
    }
    
    return `
      <div class="schema-property" style="margin-left: ${depth * 1.5}rem">
        <span class="schema-type">${schema.type || 'object'}</span>
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
    return `<span class="schema-type">${property.type || 'object'}</span>`;
  }
}
