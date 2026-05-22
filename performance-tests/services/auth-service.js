/**
 * Authentication Service Module
 * ------------------------------
 * Handles user authentication, token management, and session handling.
 * Supports dynamic user rotation across virtual users using SharedArray.
 * Tokens are cached and refreshed as needed.
 */

import http from 'k6/http';
import { check } from 'k6';
import { SharedArray } from 'k6/data';
import { getEnvironment } from '../config/environments.js';
import { loginErrors, authDuration } from '../utils/metrics.js';

// Load users from JSON file using SharedArray for memory efficiency
// SharedArray ensures data is loaded once and shared across all VUs
const users = new SharedArray('test-users', function () {
  return JSON.parse(open('../data/users.json'));
});

/**
 * Returns a user object based on the current VU ID.
 * Users are rotated across VUs using modulo operation.
 * This ensures even distribution of test users.
 * @returns {Object} User object with username, password, accountInfoId
 */
export function getUser() {
  const vuId = __VU || 1;
  const userIndex = (vuId - 1) % users.length;
  return users[userIndex];
}

/**
 * Authenticates a user and returns an auth token.
 * Makes a POST request to the authentication endpoint.
 * Extracts and returns the bearer token from the response.
 * @param {Object} user - User object with username and password
 * @returns {Object} Auth result: { token, accountInfoId, success }
 */
export function authenticate(user) {
  const env = getEnvironment();
  const authPayload = JSON.stringify({
    username: user.username,
    password: user.password,
  });

  const authHeaders = {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  };

  const startTime = Date.now();
  const response = http.post(`${env.authUrl}/login`, authPayload, {
    headers: authHeaders,
    tags: { name: 'Auth_Login' },
    timeout: '30s',
  });
  const duration = Date.now() - startTime;

  // Record authentication duration
  authDuration.add(duration);

  // Validate authentication response
  const authSuccess = check(response, {
    'Auth - login successful (200)': (r) => r.status === 200,
    'Auth - response contains token': (r) => {
      try {
        const body = r.json();
        return body && (body.token || body.accessToken || body.access_token);
      } catch (e) {
        return false;
      }
    },
  });

  if (!authSuccess) {
    loginErrors.add(1);
    console.error(
      `[AUTH ERROR] User: ${user.username} | Status: ${response.status} | Body: ${response.body ? response.body.substring(0, 200) : 'empty'}`
    );
    return { token: null, accountInfoId: user.accountInfoId, success: false };
  }

  // Extract token from response (supporting multiple response formats)
  let token = null;
  try {
    const body = response.json();
    token = body.token || body.accessToken || body.access_token || null;
  } catch (e) {
    console.error(`[AUTH ERROR] Failed to parse auth response: ${e.message}`);
  }

  return {
    token,
    accountInfoId: user.accountInfoId,
    success: true,
    username: user.username,
  };
}

/**
 * Refreshes an expired authentication token.
 * @param {string} currentToken - Current (possibly expired) token
 * @returns {string|null} New token or null on failure
 */
export function refreshToken(currentToken) {
  const env = getEnvironment();

  const response = http.post(
    `${env.authUrl}/refresh`,
    JSON.stringify({ token: currentToken }),
    {
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${currentToken}`,
      },
      tags: { name: 'Auth_Refresh' },
      timeout: '15s',
    }
  );

  if (response.status === 200) {
    try {
      const body = response.json();
      return body.token || body.accessToken || body.access_token || null;
    } catch (e) {
      console.error(`[TOKEN REFRESH] Parse error: ${e.message}`);
      return null;
    }
  }

  console.warn(`[TOKEN REFRESH] Failed with status: ${response.status}`);
  return null;
}

/**
 * Returns the total number of available test users.
 * @returns {number} User count
 */
export function getUserCount() {
  return users.length;
}

export default { getUser, authenticate, refreshToken, getUserCount };
