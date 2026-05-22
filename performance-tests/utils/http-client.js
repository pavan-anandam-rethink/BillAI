/**
 * HTTP Client Wrapper
 * --------------------
 * Base HTTP client with built-in retry mechanism, correlation extraction,
 * authorization header injection, and error logging.
 * All API calls should go through this module for consistency.
 */

import http from 'k6/http';
import { check } from 'k6';
import { errorCount, apiErrors } from './metrics.js';

// Default HTTP request parameters
const DEFAULT_TIMEOUT = '60s';
const MAX_RETRIES = 3;
const RETRY_DELAY_MS = 1000;

/**
 * Creates a base set of HTTP headers with optional auth token.
 * @param {string} token - Bearer token for authorization
 * @returns {Object} Headers object
 */
export function getHeaders(token = '') {
  const headers = {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  return headers;
}

/**
 * Executes an HTTP GET request with retry logic.
 * @param {string} url - Request URL
 * @param {Object} params - k6 HTTP params (headers, tags, etc.)
 * @param {number} retries - Number of retry attempts
 * @returns {Object} k6 HTTP response object
 */
export function get(url, params = {}, retries = MAX_RETRIES) {
  params.timeout = params.timeout || DEFAULT_TIMEOUT;
  let response;

  for (let attempt = 1; attempt <= retries; attempt++) {
    response = http.get(url, params);

    if (response.status >= 200 && response.status < 400) {
      return response;
    }

    // Log failed attempt
    if (attempt < retries) {
      console.warn(
        `[HTTP GET] Retry ${attempt}/${retries} for ${url} - Status: ${response.status}`
      );
    }
  }

  // Record error after all retries exhausted
  logError('GET', url, response);
  return response;
}

/**
 * Executes an HTTP POST request with retry logic.
 * @param {string} url - Request URL
 * @param {string|Object} body - Request body (will be stringified if Object)
 * @param {Object} params - k6 HTTP params (headers, tags, etc.)
 * @param {number} retries - Number of retry attempts
 * @returns {Object} k6 HTTP response object
 */
export function post(url, body, params = {}, retries = MAX_RETRIES) {
  params.timeout = params.timeout || DEFAULT_TIMEOUT;
  const payload = typeof body === 'object' ? JSON.stringify(body) : body;
  let response;

  for (let attempt = 1; attempt <= retries; attempt++) {
    response = http.post(url, payload, params);

    if (response.status >= 200 && response.status < 400) {
      return response;
    }

    if (attempt < retries) {
      console.warn(
        `[HTTP POST] Retry ${attempt}/${retries} for ${url} - Status: ${response.status}`
      );
    }
  }

  logError('POST', url, response);
  return response;
}

/**
 * Executes an HTTP PUT request with retry logic.
 * @param {string} url - Request URL
 * @param {string|Object} body - Request body
 * @param {Object} params - k6 HTTP params
 * @param {number} retries - Number of retry attempts
 * @returns {Object} k6 HTTP response object
 */
export function put(url, body, params = {}, retries = MAX_RETRIES) {
  params.timeout = params.timeout || DEFAULT_TIMEOUT;
  const payload = typeof body === 'object' ? JSON.stringify(body) : body;
  let response;

  for (let attempt = 1; attempt <= retries; attempt++) {
    response = http.put(url, payload, params);

    if (response.status >= 200 && response.status < 400) {
      return response;
    }

    if (attempt < retries) {
      console.warn(
        `[HTTP PUT] Retry ${attempt}/${retries} for ${url} - Status: ${response.status}`
      );
    }
  }

  logError('PUT', url, response);
  return response;
}

/**
 * Extracts a value from an HTTP response using correlation.
 * Useful for extracting tokens, session IDs, or dynamic values.
 * @param {Object} response - k6 HTTP response
 * @param {string} jsonPath - JSON path to extract (e.g., 'data.token')
 * @returns {*} Extracted value or null
 */
export function extractJsonValue(response, jsonPath) {
  try {
    const body = response.json();
    const keys = jsonPath.split('.');
    let value = body;
    for (const key of keys) {
      if (value === null || value === undefined) return null;
      value = value[key];
    }
    return value;
  } catch (e) {
    console.error(`[Correlation] Failed to extract '${jsonPath}': ${e.message}`);
    return null;
  }
}

/**
 * Performs standard response validation checks.
 * @param {Object} response - k6 HTTP response
 * @param {string} name - Check name for reporting
 * @param {number} expectedStatus - Expected HTTP status code (default: 200)
 * @returns {boolean} Whether all checks passed
 */
export function validateResponse(response, name, expectedStatus = 200) {
  return check(response, {
    [`${name} - status is ${expectedStatus}`]: (r) => r.status === expectedStatus,
    [`${name} - response time < 3s`]: (r) => r.timings.duration < 3000,
    [`${name} - response body is not empty`]: (r) => r.body && r.body.length > 0,
  });
}

/**
 * Logs an error to console and increments error counters.
 * @param {string} method - HTTP method (GET, POST, etc.)
 * @param {string} url - Request URL
 * @param {Object} response - k6 HTTP response
 */
function logError(method, url, response) {
  const status = response ? response.status : 'N/A';
  const duration = response ? response.timings.duration : 'N/A';
  console.error(
    `[ERROR] ${method} ${url} | Status: ${status} | Duration: ${duration}ms`
  );
  errorCount.add(1);
  apiErrors.add(1);
}

export default { get, post, put, getHeaders, extractJsonValue, validateResponse };
