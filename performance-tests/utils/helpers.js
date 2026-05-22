/**
 * Helper Utilities
 * -----------------
 * Reusable utility functions for performance test scripts.
 * Includes think-time simulation, random selection, date formatting,
 * and data generation helpers.
 */

import { sleep } from 'k6';

/**
 * Simulates realistic user think time.
 * Random delay between min and max seconds.
 * @param {number} min - Minimum sleep time in seconds (default: 1)
 * @param {number} max - Maximum sleep time in seconds (default: 5)
 */
export function thinkTime(min = 1, max = 5) {
  const delay = Math.random() * (max - min) + min;
  sleep(delay);
}

/**
 * Returns a short think time for between API calls within a transaction.
 * Simulates real user interaction speed.
 */
export function shortPause() {
  sleep(Math.random() * 0.5 + 0.3);
}

/**
 * Selects a random element from an array.
 * @param {Array} arr - Array to select from
 * @returns {*} Random element
 */
export function randomItem(arr) {
  return arr[Math.floor(Math.random() * arr.length)];
}

/**
 * Generates a random integer between min and max (inclusive).
 * @param {number} min - Minimum value
 * @param {number} max - Maximum value
 * @returns {number} Random integer
 */
export function randomInt(min, max) {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}

/**
 * Formats a Date object to ISO 8601 date string (YYYY-MM-DD).
 * @param {Date} date - Date object
 * @returns {string} Formatted date string
 */
export function formatDate(date) {
  return date.toISOString().split('T')[0];
}

/**
 * Returns today's date as ISO string.
 * @returns {string} Today's date in YYYY-MM-DD format
 */
export function today() {
  return formatDate(new Date());
}

/**
 * Returns a date N days ago as ISO string.
 * @param {number} days - Number of days to subtract
 * @returns {string} Date string in YYYY-MM-DD format
 */
export function daysAgo(days) {
  const date = new Date();
  date.setDate(date.getDate() - days);
  return formatDate(date);
}

/**
 * Creates standard pagination parameters.
 * @param {number} pageNumber - Page number (default: 1)
 * @param {number} pageSize - Page size (default: 25)
 * @returns {Object} Pagination object
 */
export function paginate(pageNumber = 1, pageSize = 25) {
  return {
    pageNumber,
    pageSize,
    filterModels: [],
    sortModels: [],
  };
}

/**
 * Generates a unique test identifier for correlation tracking.
 * @returns {string} Unique ID
 */
export function generateTestId() {
  return `perf_${Date.now()}_${Math.random().toString(36).substring(2, 8)}`;
}

/**
 * Safely parses JSON response body. Returns null on failure.
 * @param {Object} response - k6 HTTP response
 * @returns {Object|null} Parsed JSON or null
 */
export function safeJsonParse(response) {
  try {
    return response.json();
  } catch (e) {
    console.error(`[JSON Parse Error] ${e.message}`);
    return null;
  }
}

/**
 * Measures execution time of a function and returns result + duration.
 * @param {Function} fn - Function to measure
 * @returns {Object} { result, duration } where duration is in milliseconds
 */
export function measure(fn) {
  const start = Date.now();
  const result = fn();
  const duration = Date.now() - start;
  return { result, duration };
}

export default {
  thinkTime,
  shortPause,
  randomItem,
  randomInt,
  formatDate,
  today,
  daysAgo,
  paginate,
  generateTestId,
  safeJsonParse,
  measure,
};
