// SecureSpec UI - Operation Display Component
export class OperationDisplay {
  constructor(state) {
    this.state = state;
  }
  
  render(operation) {
    const method = operation.method.toLowerCase();
    const methodClass = `method-${method}`;
    
    return `
      <div class="operation" data-operation-id="${operation.operationId || ''}">
        <div class="operation-header">
          <div>
            <span class="operation-method ${methodClass}">${method}</span>
            <strong>${operation.path}</strong>
          </div>
          <span>${operation.summary || ''}</span>
        </div>
        <div class="operation-content">
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
