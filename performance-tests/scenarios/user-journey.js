/**
 * User Journey Scenario Module
 * ------------------------------
 * Defines the complete realistic user journey through the BH Billing Application.
 * Simulates a user logging in, navigating through all billing modules,
 * and performing typical operations with natural think times.
 */

import { group } from 'k6';
import http from 'k6/http';
import { check } from 'k6';
import { getEnvironment } from '../config/environments.js';
import { getHeaders } from '../utils/http-client.js';
import { thinkTime, shortPause } from '../utils/helpers.js';
import { launchDuration, recordSuccess, recordFailure } from '../utils/metrics.js';
import {
  getDashboard,
  getPendingReview,
  getReadyToBill,
  getBilledPending,
  getCompletedClaims,
  getRejectedClaims,
  getDeniedClaims,
  getPaymentPosting,
  getPendingCollection,
} from '../services/billing-api.js';

/**
 * Simulates the application launch (initial page load).
 * Transaction Group: Launch
 * @param {string} token - Bearer token
 * @returns {boolean} Whether launch was successful
 */
export function launchApplication(token) {
  const env = getEnvironment();

  return group('Launch', function () {
    const startTime = Date.now();
    const response = http.get(env.webAppUrl, {
      headers: getHeaders(token),
      tags: { name: 'App_Launch' },
      timeout: '30s',
    });
    const duration = Date.now() - startTime;

    launchDuration.add(duration);

    const success = check(response, {
      'Launch - page loads successfully': (r) => r.status === 200,
      'Launch - response time < 5s': (r) => r.timings.duration < 5000,
    });

    if (success) {
      recordSuccess('Launch', duration);
    } else {
      recordFailure('Launch', duration);
    }

    return success;
  });
}

/**
 * Executes the complete user journey through all billing modules.
 * This is the main scenario function called by each VU iteration.
 * Simulates realistic user behavior with think times between actions.
 *
 * Journey Flow:
 * 1. Launch Application
 * 2. Dashboard
 * 3. Pending Review
 * 4. Ready To Bill
 * 5. Billed Pending
 * 6. Completed Claims
 * 7. Rejected Claims
 * 8. Denied Claims
 * 9. Payment Posting
 * 10. Pending Collection
 *
 * @param {string} token - Bearer authentication token
 * @param {number} accountInfoId - User's account identifier
 */
export function executeUserJourney(token, accountInfoId) {
  // Step 1: Launch the application
  launchApplication(token);
  thinkTime(1, 3);

  // Step 2: Navigate to Dashboard
  getDashboard(token, accountInfoId);
  thinkTime(2, 4);

  // Step 3: Check Pending Review
  getPendingReview(token, accountInfoId);
  thinkTime(1, 3);

  // Step 4: Check Ready To Bill
  getReadyToBill(token, accountInfoId);
  thinkTime(1, 3);

  // Step 5: Check Billed Pending
  getBilledPending(token, accountInfoId);
  thinkTime(1, 2);

  // Step 6: Check Completed Claims
  getCompletedClaims(token, accountInfoId);
  thinkTime(1, 3);

  // Step 7: Check Rejected Claims
  getRejectedClaims(token, accountInfoId);
  thinkTime(1, 2);

  // Step 8: Check Denied Claims
  getDeniedClaims(token, accountInfoId);
  thinkTime(1, 2);

  // Step 9: Check Payment Posting
  getPaymentPosting(token, accountInfoId);
  thinkTime(1, 3);

  // Step 10: Check Pending Collection
  getPendingCollection(token, accountInfoId);
  thinkTime(2, 5);
}

/**
 * Executes a minimal journey for smoke testing.
 * Only hits the most critical endpoints.
 * @param {string} token - Bearer token
 * @param {number} accountInfoId - Account identifier
 */
export function executeSmokeJourney(token, accountInfoId) {
  launchApplication(token);
  shortPause();
  getDashboard(token, accountInfoId);
  shortPause();
  getPendingReview(token, accountInfoId);
  shortPause();
}

export default { launchApplication, executeUserJourney, executeSmokeJourney };
