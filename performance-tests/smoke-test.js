/**
 * BH Billing Application - Smoke Test
 * ======================================
 * Minimal load test for baseline validation.
 * Runs 5 VUs for 5 minutes to verify system health and API availability.
 *
 * Execution:
 *   k6 run smoke-test.js -e ENV=qa
 */

import { group } from 'k6';
import { smokeScenario } from './config/scenarios.js';
import { smokeThresholds } from './config/thresholds.js';
import { getEnvironment } from './config/environments.js';
import { getUser, authenticate } from './services/auth-service.js';
import { executeSmokeJourney } from './scenarios/user-journey.js';
import { loginDuration, recordSuccess, recordFailure } from './utils/metrics.js';
import { thinkTime } from './utils/helpers.js';
import { generateReports } from './utils/reporters.js';

// k6 options for smoke test
export const options = {
  scenarios: smokeScenario,
  thresholds: smokeThresholds,
};

export function setup() {
  const env = getEnvironment();
  console.log(`[SMOKE TEST] Environment: ${env.name} | URL: ${env.apiBaseUrl}`);
  return { environment: env.name, testStartTime: new Date().toISOString() };
}

export default function () {
  const user = getUser();

  // Authenticate
  let authResult;
  group('Login', function () {
    const startTime = Date.now();
    authResult = authenticate(user);
    const duration = Date.now() - startTime;
    loginDuration.add(duration);
    if (authResult.success) {
      recordSuccess('Login', duration);
    } else {
      recordFailure('Login', duration);
    }
  });

  // Execute minimal journey
  if (authResult && authResult.success && authResult.token) {
    executeSmokeJourney(authResult.token, authResult.accountInfoId);
  }

  thinkTime(1, 2);
}

export function teardown(data) {
  console.log(`[SMOKE TEST] Completed at ${new Date().toISOString()}`);
}

export function handleSummary(data) {
  return generateReports(data, 'smoke-test');
}
