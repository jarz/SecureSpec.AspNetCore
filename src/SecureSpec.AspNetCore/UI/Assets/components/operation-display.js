// SecureSpec UI - Operation Display Component
import { LinksCallbacksDisplay } from './links-callbacks.js';
import { sanitizeId, escapeHtml } from './utils.js';

const HTTP_METHODS = ['get', 'post', 'put', 'delete', 'patch', 'options', 'head', 'trace'];

export class OperationDisplay {
  constructor(state) {
    this.state = state;
    this.operations = [];
    this.tags = new Map(); // tag name -> operations[]
    this.linksCallbacksDisplay = new LinksCallbacksDisplay(state);
  }
  
  /**
   * Load operations from OpenAPI document
   * @param {object} openApiDoc - The OpenAPI document
   */
  loadOperations(openApiDoc) {
    this.resetOperationsState();

    if (!this.hasPathDefinitions(openApiDoc)) {
      return;
    }

    for (const [path, pathItem] of Object.entries(openApiDoc.paths)) {
      this.processPathItem(path, pathItem);
    }

    this.sortTags();
  }

  resetOperationsState() {
    this.operations = [];
    this.tags.clear();
  }

  hasPathDefinitions(openApiDoc) {
    return Boolean(openApiDoc?.paths);
  }

  processPathItem(path, pathItem) {
    if (!pathItem) {
      return;
    }

    for (const method of HTTP_METHODS) {
      const operationDoc = pathItem[method];
      if (!operationDoc) {
        continue;
      }

      const operation = this.createOperation(path, method, operationDoc);
      this.addOperation(operation);
    }
  }

  createOperation(path, method, operationDoc) {
    return {
      ...operationDoc,
      method,
      path,
      operationId: operationDoc.operationId || `${method}_${path.replaceAll('/', '_')}`
    };
  }

  addOperation(operation) {
    this.operations.push(operation);
    this.groupOperationByTags(operation);
  }

  groupOperationByTags(operation) {
    const opTags = operation.tags && operation.tags.length > 0 ? operation.tags : ['default'];
    for (const tag of opTags) {
      if (!this.tags.has(tag)) {
        this.tags.set(tag, []);
      }
      this.tags.get(tag).push(operation);
    }
  }

  sortTags() {
    this.tags = new Map([...this.tags.entries()].sort((a, b) => a[0].localeCompare(b[0])));
  }

  /**
   * Render all operations grouped by tags
   * @param {string} filter - Optional filter string
   * @returns {string} HTML string
   */
  renderAll(filter = '') {
    if (this.tags.size === 0) {
      return '<div class="no-operations">No operations available</div>';
    }

    const filterLower = filter.toLowerCase();
    let html = '';

    for (const [tagName, operations] of this.tags) {
      const filteredOps = this.filterOperations(operations, filterLower);
      
      if (filteredOps.length === 0) {
        continue; // Skip tags with no matching operations
      }

      html += this.renderTag(tagName, filteredOps);
    }

    return html || '<div class="no-operations">No operations match the filter</div>';
  }

  /**
   * Filter operations based on search criteria
   * @param {Array} operations - Operations to filter
   * @param {string} filter - Filter string (lowercase)
   * @returns {Array} Filtered operations
   */
  filterOperations(operations, filter) {
    if (!filter) {
      return operations;
    }

    return operations.filter(op => {
      const searchText = [
        op.path,
        op.method,
        op.summary || '',
        op.description || '',
        op.operationId || ''
      ].join(' ').toLowerCase();

      return searchText.includes(filter);
    });
  }

  /**
   * Render a tag group with its operations
   * @param {string} tagName - Tag name
   * @param {Array} operations - Operations in this tag
   * @returns {string} HTML string
   */
  renderTag(tagName, operations) {
    const tagId = sanitizeId(tagName);
    const isExpanded = this.state.getState().expandedTags?.has(tagName) ?? true;
    const expandedClass = isExpanded ? 'expanded' : '';
    const arrowIcon = isExpanded ? '▼' : '▶';

    return `
      <div class="tag-group" data-tag="${tagName}">
        <div class="tag-header ${expandedClass}" data-tag-id="${tagId}">
          <span class="tag-arrow">${arrowIcon}</span>
          <h2 class="tag-name">${escapeHtml(tagName)}</h2>
          <span class="tag-count">${operations.length} operation${operations.length === 1 ? '' : 's'}</span>
        </div>
        <div class="tag-operations ${expandedClass}">
          ${operations.map(op => this.renderOperation(op)).join('')}
        </div>
      </div>
    `;
  }

  /**
   * Render a single operation
   * @param {object} operation - Operation to render
   * @returns {string} HTML string
   */
  renderOperation(operation) {
    const method = operation.method.toLowerCase();
    const methodClass = `method-${method}`;
    const opId = sanitizeId(operation.operationId);
    const isExpanded = this.state.getState().expandedOperations?.has(operation.operationId) ?? false;
    const expandedClass = isExpanded ? 'expanded' : '';
    
    return `
      <div class="operation" id="operation-${opId}" data-operation-id="${operation.operationId}">
        <div class="operation-header ${expandedClass}" data-operation="${opId}" tabindex="0" role="button" aria-expanded="${isExpanded}">
          <div class="operation-summary">
            <span class="operation-method ${methodClass}">${method.toUpperCase()}</span>
            <code class="operation-path">${escapeHtml(operation.path)}</code>
            ${operation.summary ? `<span class="operation-title">${escapeHtml(operation.summary)}</span>` : ''}
          </div>
          ${this.shouldDisplayOperationId() ? `<span class="operation-id">${escapeHtml(operation.operationId)}</span>` : ''}
        </div>
        <div class="operation-content ${expandedClass}">
          ${operation.description ? `<p class="operation-description">${escapeHtml(operation.description)}</p>` : ''}
          ${this.renderParameters(operation.parameters)}
          ${this.renderRequestBody(operation.requestBody)}
          ${this.renderResponsesBasic(operation.responses)}
        </div>
      </div>
    `;
  }
  
  renderWithDocument(operation, document) {
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
  
  /**
   * Check if operation IDs should be displayed
   * @returns {boolean}
   */
  shouldDisplayOperationId() {
    return this.state.config?.displayOperationId ?? true;
  }

  /**
   * Render parameters section
   * @param {Array} parameters - Operation parameters
   * @returns {string} HTML string
   */
  renderParameters(parameters) {
    if (!parameters || parameters.length === 0) {
      return '';
    }
    
    return `
      <div class="parameters-section">
        <h3>Parameters</h3>
        <table class="parameters-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>In</th>
              <th>Type</th>
              <th>Required</th>
              <th>Description</th>
            </tr>
          </thead>
          <tbody>
            ${parameters.map(p => `
              <tr>
                <td><code>${escapeHtml(p.name)}</code></td>
                <td><span class="param-in">${escapeHtml(p.in)}</span></td>
                <td><code>${escapeHtml(p.schema?.type || 'object')}</code></td>
                <td>${p.required ? '<span class="required">✓</span>' : ''}</td>
                <td>${escapeHtml(p.description || '')}</td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    `;
  }
  
  /**
   * Render request body section
   * @param {object} requestBody - Request body definition
   * @returns {string} HTML string
   */
  renderRequestBody(requestBody) {
    if (!requestBody) {
      return '';
    }
    
    return `
      <div class="request-body-section">
        <h3>Request Body ${requestBody.required ? '<span class="required">required</span>' : ''}</h3>
        ${requestBody.description ? `<p>${escapeHtml(requestBody.description)}</p>` : ''}
        ${this.renderMediaTypes(requestBody.content)}
      </div>
    `;
  }

  /**
   * Render media types
   * @param {object} content - Content object with media types
   * @returns {string} HTML string
   */
  renderMediaTypes(content) {
    if (!content) {
      return '';
    }

    return `
      <div class="media-types">
        ${Object.entries(content).map(([mediaType, mediaTypeObj]) => `
          <div class="media-type">
            <strong>${escapeHtml(mediaType)}</strong>
          </div>
        `).join('')}
      </div>
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
  
  renderResponsesBasic(responses) {
    if (!responses) {
      return '';
    }
    
    return `
      <div class="responses-section">
        <h3>Responses</h3>
        <table class="responses-table">
          <thead>
            <tr>
              <th>Code</th>
              <th>Description</th>
            </tr>
          </thead>
          <tbody>
            ${Object.entries(responses).map(([code, response]) => `
              <tr>
                <td><code class="response-code response-${code[0]}xx">${escapeHtml(code)}</code></td>
                <td>${escapeHtml(response.description || '')}</td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    `;
  }
  
  renderCallbacks(callbacks, document) {
    if (!callbacks) {
      return '';
    }
    
    return this.linksCallbacksDisplay.renderCallbacks(callbacks, document);
  }
}
