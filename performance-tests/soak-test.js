/**
 * BH Billing Application - Soak Test
 * =====================================
 * Endurance test running sustained load for extended periods.
 * Validates system stability, memory leaks, and resource exhaustion.
 * Runs 200 VUs for ~60 minutes with ramp-up and ramp-down.
 *
 * Execution:
 *   k6 run soak-test.js -e ENV=qa
 */

import { group } from 'k6';
import { soakScenario } from './config/scenarios.js';
import { soakThresholds } from './config/thresholds.js';
import { getEnvironment } from './config/environments.js';
import { getUser, authenticate } from './services/auth-service.js';
import { executeUserJourney } from './scenarios/user-journey.js';
import { loginDuration, recordSuccess, recordFailure } from './utils/metrics.js';
import { thinkTime } from './utils/helpers.js';
import { generateReports } from './utils/reporters.js';

export const options = {
  scenarios: soakScenario,
  thresholds: soakThresholds,
};

export function setup() {
  const env = getEnvironment();
  console.log(`[SOAK TEST] Environment: ${env.name} | Duration: ~60 min`);
  return { environment: env.name, testStartTime: new Date().toISOString() };
}

export default function () {
  const user = getUser();

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

  if (authResult && authResult.success && authResult.token) {
    executeUserJourney(authResult.token, authResult.accountInfoId);
  }

  thinkTime(2, 5);
}

export function teardown(data) {
  console.log(`[SOAK TEST] Completed at ${new Date().toISOString()}`);
}

export function handleSummary(data) {
  return generateReports(data, 'soak-test');
}
