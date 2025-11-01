// SecureSpec UI - Shared Utility Functions

/**
 * Sanitize ID for use in HTML
 * @param {string} id - ID to sanitize
 * @returns {string} Sanitized ID
 */
export function sanitizeId(id) {
  return id.replace(/[^a-zA-Z0-9_-]/g, '_');
}

/**
 * Escape HTML special characters
 * @param {string} text - Text to escape
 * @returns {string} Escaped text
 */
export function escapeHtml(text) {
  if (!text) {
    return '';
  }

  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}
