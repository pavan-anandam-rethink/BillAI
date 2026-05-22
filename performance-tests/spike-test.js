/**
 * BH Billing Application - Spike Test
 * ======================================
 * Simulates sudden traffic spikes to validate auto-scaling
 * and system behavior under unexpected load bursts.
 *
 * Execution:
 *   k6 run spike-test.js -e ENV=qa
 */

import { group } from 'k6';
import { spikeScenario } from './config/scenarios.js';
import { stressThresholds } from './config/thresholds.js';
import { getEnvironment } from './config/environments.js';
import { getUser, authenticate } from './services/auth-service.js';
import { executeUserJourney } from './scenarios/user-journey.js';
import { loginDuration, recordSuccess, recordFailure } from './utils/metrics.js';
import { thinkTime } from './utils/helpers.js';
import { generateReports } from './utils/reporters.js';

export const options = {
  scenarios: spikeScenario,
  thresholds: stressThresholds,
};

export function setup() {
  const env = getEnvironment();
  console.log(`[SPIKE TEST] Environment: ${env.name} | Spike to 500 VUs`);
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

  thinkTime(1, 3);
}

export function teardown(data) {
  console.log(`[SPIKE TEST] Completed at ${new Date().toISOString()}`);
}

export function handleSummary(data) {
  return generateReports(data, 'spike-test');
}
