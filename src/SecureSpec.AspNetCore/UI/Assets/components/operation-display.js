// SecureSpec UI - Operation Display Component
import { LinksCallbacksDisplay } from './links-callbacks.js';

export class OperationDisplay {
  constructor(state) {
    this.state = state;
    this.linksCallbacksDisplay = new LinksCallbacksDisplay(state);
  }
  
  render(operation, document) {
    const method = operation.method.toLowerCase();
    const methodClass = `method-${method}`;
    
    // Clear visited links for circular detection on each operation render
    this.linksCallbacksDisplay.clearVisitedLinks();
    
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
          ${this.renderCallbacks(operation.callbacks, document)}
          ${this.renderResponses(operation.responses, document)}
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
  
  renderResponses(responses, document) {
    if (!responses) {
      return '';
    }
    
    return `
      <h3>Responses</h3>
      <ul>
        ${Object.entries(responses).map(([code, response]) => `
          <li>
            <strong>${code}</strong> - ${response.description || ''}
            ${this.linksCallbacksDisplay.renderLinks(response.links, document)}
          </li>
        `).join('')}
      </ul>
    `;
  }
  
  renderCallbacks(callbacks, document) {
    if (!callbacks) {
      return '';
    }
    
    return this.linksCallbacksDisplay.renderCallbacks(callbacks, document);
  }
}
