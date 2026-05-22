/**
 * BH Billing Application - Load Test
 * =====================================
 * Main load test script supporting 100, 200, and 300 concurrent users.
 * Uses ramping-vus executor with configurable ramp-up/ramp-down phases.
 *
 * Execution:
 *   k6 run load-test.js -e CONCURRENCY=300 -e ENV=qa
 *   k6 run load-test.js -e CONCURRENCY=100 --out json=results/load-results.json
 *
 * Environment Variables:
 *   CONCURRENCY - Target number of concurrent VUs (default: 100)
 *   ENV         - Target environment: qa, stg, prod (default: qa)
 *   DURATION    - Test duration (default: 30m)
 */

import { group } from 'k6';
import { getLoadScenario } from './config/scenarios.js';
import { defaultThresholds } from './config/thresholds.js';
import { getEnvironment } from './config/environments.js';
import { getUser, authenticate } from './services/auth-service.js';
import { executeUserJourney } from './scenarios/user-journey.js';
import { loginDuration, recordSuccess, recordFailure, activeUsers } from './utils/metrics.js';
import { thinkTime } from './utils/helpers.js';
import { generateReports } from './utils/reporters.js';

// ---- Configuration ----
// Read concurrency from environment variable, default to 100
const concurrency = Number(__ENV.CONCURRENCY || 100);
const duration = __ENV.DURATION || '30m';

// k6 options: scenarios, thresholds, and global settings
export const options = {
  scenarios: getLoadScenario(concurrency, duration),
  thresholds: defaultThresholds,
  // Discard response bodies over 1MB to save memory
  discardResponseMessageBody: false,
  // DNS caching for performance
  dns: {
    ttl: '5m',
    select: 'roundRobin',
  },
};

// ---- Setup Phase ----
// Runs once before test execution begins.
// Used for test-wide initialization and logging.
export function setup() {
  const env = getEnvironment();
  console.log('='.repeat(80));
  console.log(`  BH BILLING - LOAD TEST`);
  console.log(`  Environment: ${env.name}`);
  console.log(`  Target Concurrency: ${concurrency} VUs`);
  console.log(`  Duration: ${duration}`);
  console.log(`  API Base URL: ${env.apiBaseUrl}`);
  console.log('='.repeat(80));

  return {
    environment: env.name,
    concurrency: concurrency,
    testStartTime: new Date().toISOString(),
  };
}

// ---- Main VU Execution ----
// This function runs for each virtual user iteration.
export default function (data) {
  // Step 1: Get user credentials for this VU (rotated via modulo)
  const user = getUser();

  // Step 2: Authenticate and obtain token
  let authResult;
  group('Login', function () {
    const startTime = Date.now();
    authResult = authenticate(user);
    const duration = Date.now() - startTime;
    loginDuration.add(duration);

    if (authResult.success) {
      recordSuccess('Login', duration);
      activeUsers.add(1);
    } else {
      recordFailure('Login', duration);
    }
  });

  // Step 3: Execute user journey only if authentication succeeded
  if (authResult && authResult.success && authResult.token) {
    executeUserJourney(authResult.token, authResult.accountInfoId);
  } else {
    console.warn(`[VU ${__VU}] Authentication failed for ${user.username}, skipping journey`);
    thinkTime(2, 5);
  }
}

// ---- Teardown Phase ----
// Runs once after all VUs have completed.
export function teardown(data) {
  console.log('='.repeat(80));
  console.log('  LOAD TEST COMPLETED');
  console.log(`  Environment: ${data.environment}`);
  console.log(`  Concurrency: ${data.concurrency} VUs`);
  console.log(`  Started: ${data.testStartTime}`);
  console.log(`  Ended: ${new Date().toISOString()}`);
  console.log('='.repeat(80));
}

// ---- Custom Summary Handler ----
// Generates HTML, JSON, and console reports
export function handleSummary(data) {
  return generateReports(data, `load-test-${concurrency}vu`);
}
