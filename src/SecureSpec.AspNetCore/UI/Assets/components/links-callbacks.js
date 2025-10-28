// SecureSpec UI - Links and Callbacks Display Component
export class LinksCallbacksDisplay {
  constructor(state) {
    this.state = state;
    this.visitedLinks = new Set(); // Track visited links for circular detection
  }

  /**
   * Renders links for a response
   * @param {Object} links - The links object from OpenAPI response
   * @param {Object} document - The full OpenAPI document for operationId resolution
   * @returns {string} HTML string for links display
   */
  renderLinks(links, document) {
    if (!links || Object.keys(links).length === 0) {
      return '';
    }

    const linkEntries = Object.entries(links).map(([linkName, link]) => {
      return this.renderLink(linkName, link, document);
    }).filter(html => html !== '');

    if (linkEntries.length === 0) {
      return '';
    }

    return `
      <div class="links-section">
        <h4>Links</h4>
        <div class="links-list">
          ${linkEntries.join('')}
        </div>
      </div>
    `;
  }

  /**
   * Renders a single link
   * @param {string} linkName - The name of the link
   * @param {Object} link - The link object
   * @param {Object} document - The full OpenAPI document
   * @returns {string} HTML string for the link
   */
  renderLink(linkName, link, document) {
    // Handle broken $ref (AC 497)
    if (link.$ref) {
      const resolvedLink = this.resolveReference(link.$ref, document);
      if (!resolvedLink) {
        console.error(`[LNK004] Broken $ref in link: ${link.$ref}`);
        return ''; // Omit broken reference safely
      }
      link = resolvedLink;
    }

    // Check for circular links (AC 493)
    // Include link description or parameters as additional identifying information to avoid false positives
    const linkIdentifier = JSON.stringify({
      name: linkName,
      operationId: link.operationId || '',
      operationRef: link.operationRef || '',
      description: link.description || '',
      paramCount: link.parameters ? Object.keys(link.parameters).length : 0
    });
    
    if (this.visitedLinks.has(linkIdentifier)) {
      console.warn(`[LNK001] Circular link detection: ${linkName}`);
      return `
        <div class="link-item circular">
          <strong>${this.escapeHtml(linkName)}</strong>
          <span class="circular-placeholder">(Circular reference detected)</span>
        </div>
      `;
    }
    
    this.visitedLinks.add(linkIdentifier);

    // Determine operation reference (AC 494, AC 495)
    let operationRef = '';
    let diagnosticCode = '';
    
    if (link.operationId) {
      operationRef = link.operationId;
    } else if (link.operationRef) {
      // AC 494: Missing operationId but valid operationRef uses operationRef only
      operationRef = link.operationRef;
      diagnosticCode = 'LNK002';
      console.info(`[LNK002] Using operationRef fallback for link: ${linkName}`);
    } else {
      // AC 495: Missing both operationId & operationRef logs warning & renders stub
      console.warn(`[LNK003] Missing both operationId and operationRef for link: ${linkName}`);
      return `
        <div class="link-item stub">
          <strong>${this.escapeHtml(linkName)}</strong>
          <span class="link-stub">(Missing reference)</span>
          ${link.description ? `<p class="link-description">${this.escapeHtml(link.description)}</p>` : ''}
        </div>
      `;
    }

    // Render parameters if present
    const parametersHtml = link.parameters 
      ? this.renderLinkParameters(link.parameters)
      : '';

    return `
      <div class="link-item">
        <strong>${this.escapeHtml(linkName)}</strong>
        <code class="link-operation">${this.escapeHtml(operationRef)}</code>
        ${diagnosticCode ? `<span class="diagnostic-badge info">${diagnosticCode}</span>` : ''}
        ${link.description ? `<p class="link-description">${this.escapeHtml(link.description)}</p>` : ''}
        ${parametersHtml}
      </div>
    `;
  }

  /**
   * Renders parameters for a link
   * @param {Object} parameters - The parameters object
   * @returns {string} HTML string for parameters
   */
  renderLinkParameters(parameters) {
    const paramEntries = Object.entries(parameters).map(([paramName, paramValue]) => {
      return `
        <li>
          <code>${this.escapeHtml(paramName)}</code>: 
          <span class="param-value">${this.escapeHtml(String(paramValue))}</span>
        </li>
      `;
    }).join('');

    return `
      <div class="link-parameters">
        <h5>Parameters:</h5>
        <ul>${paramEntries}</ul>
      </div>
    `;
  }

  /**
   * Renders callbacks for an operation
   * @param {Object} callbacks - The callbacks object from OpenAPI operation
   * @param {Object} document - The full OpenAPI document
   * @returns {string} HTML string for callbacks display
   */
  renderCallbacks(callbacks, document) {
    if (!callbacks || Object.keys(callbacks).length === 0) {
      return '';
    }

    // AC 496: Callback section read-only (no Try It Out) logged informational
    console.info('[CBK001] Callback section rendered read-only');

    const callbackEntries = Object.entries(callbacks).map(([callbackName, callback]) => {
      return this.renderCallback(callbackName, callback, document);
    }).filter(html => html !== '');

    if (callbackEntries.length === 0) {
      return '';
    }

    return `
      <div class="callbacks-section">
        <h4>Callbacks</h4>
        <div class="callbacks-note info">
          <span class="diagnostic-badge info">CBK001</span>
          Callbacks are read-only and do not support Try It Out
        </div>
        <div class="callbacks-list">
          ${callbackEntries.join('')}
        </div>
      </div>
    `;
  }

  /**
   * Renders a single callback
   * @param {string} callbackName - The name of the callback
   * @param {Object} callback - The callback object
   * @param {Object} document - The full OpenAPI document
   * @returns {string} HTML string for the callback
   */
  renderCallback(callbackName, callback, document) {
    // Handle broken $ref
    if (callback.$ref) {
      const resolvedCallback = this.resolveReference(callback.$ref, document);
      if (!resolvedCallback) {
        console.error(`[CBK002] Broken $ref in callback: ${callback.$ref}`);
        return ''; // Omit broken reference safely
      }
      callback = resolvedCallback;
    }

    // Render callback expressions (URLs)
    const expressionEntries = Object.entries(callback).map(([expression, pathItem]) => {
      return this.renderCallbackExpression(expression, pathItem);
    }).join('');

    return `
      <div class="callback-item">
        <strong>${this.escapeHtml(callbackName)}</strong>
        <div class="callback-expressions">
          ${expressionEntries}
        </div>
      </div>
    `;
  }

  /**
   * Renders a callback expression (URL pattern with operations)
   * @param {string} expression - The URL expression
   * @param {Object} pathItem - The path item containing operations
   * @returns {string} HTML string for the callback expression
   */
  renderCallbackExpression(expression, pathItem) {
    const operations = [];
    
    // Check for HTTP methods in the pathItem
    const methods = ['get', 'post', 'put', 'patch', 'delete', 'head', 'options'];
    methods.forEach(method => {
      if (pathItem[method]) {
        const operation = pathItem[method];
        operations.push({
          method: method.toUpperCase(),
          description: operation.description || operation.summary || ''
        });
      }
    });

    const operationsHtml = operations.map(op => `
      <div class="callback-operation">
        <span class="method-${op.method.toLowerCase()}">${op.method}</span>
        ${op.description ? `<span>${this.escapeHtml(op.description)}</span>` : ''}
      </div>
    `).join('');

    return `
      <div class="callback-expression">
        <code class="callback-url">${this.escapeHtml(expression)}</code>
        ${operationsHtml}
      </div>
    `;
  }

  /**
   * Resolves a $ref reference in the document
   * @param {string} ref - The reference path (e.g., "#/components/links/MyLink")
   * @param {Object} document - The full OpenAPI document
   * @returns {Object|null} The resolved object or null if not found
   */
  resolveReference(ref, document) {
    if (!ref) {
      return null;
    }
    
    // Only handle internal references starting with '#/'
    if (!ref.startsWith('#/')) {
      console.warn(`[LNK004] External or invalid reference not supported: ${ref}`);
      return null;
    }

    const parts = ref.substring(2).split('/'); // Remove '#/' and split
    let current = document;

    for (const part of parts) {
      if (!current || typeof current !== 'object') {
        return null;
      }
      current = current[part];
    }

    return current || null;
  }

  /**
   * Escapes HTML special characters
   * @param {string} text - Text to escape
   * @returns {string} Escaped text
   */
  escapeHtml(text) {
    if (!text) {
      return '';
    }
    
    // Use string replacement instead of DOM manipulation for better compatibility
    return String(text)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  /**
   * Clears the visited links set (call this before rendering a new operation)
   */
  clearVisitedLinks() {
    this.visitedLinks.clear();
  }
}
