/**
 * Scenario Configuration Module
 * ------------------------------
 * Defines k6 executor scenarios for different load profiles.
 * Supports 100, 200, and 300 concurrent users via CONCURRENCY env var.
 */

/**
 * Returns load test scenario configuration based on concurrency level.
 * Uses ramping-vus executor for realistic ramp-up and ramp-down.
 * @param {number} concurrency - Target number of concurrent virtual users
 * @param {string} duration - Total test duration (default: '30m')
 * @returns {Object} k6 scenarios configuration object
 */
export function getLoadScenario(concurrency = 100, duration = '30m') {
  return {
    billing_load_test: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        // Ramp-up phase: 20% of duration
        { duration: '5m', target: Math.round(concurrency * 0.5) },
        { duration: '3m', target: concurrency },
        // Steady-state phase: 60% of duration
        { duration: '17m', target: concurrency },
        // Ramp-down phase: 20% of duration
        { duration: '3m', target: Math.round(concurrency * 0.5) },
        { duration: '2m', target: 0 },
      ],
      gracefulRampDown: '30s',
    },
  };
}

// Smoke test scenario: minimal load for baseline validation
export const smokeScenario = {
  smoke_test: {
    executor: 'constant-vus',
    vus: 5,
    duration: '5m',
  },
};

// Stress test scenario: push beyond normal capacity
export const stressScenario = {
  stress_test: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '3m', target: 100 },
      { duration: '3m', target: 200 },
      { duration: '5m', target: 300 },
      { duration: '5m', target: 400 },
      { duration: '5m', target: 500 },
      { duration: '3m', target: 300 },
      { duration: '3m', target: 100 },
      { duration: '3m', target: 0 },
    ],
    gracefulRampDown: '30s',
  },
};

// Spike test scenario: sudden burst of traffic
export const spikeScenario = {
  spike_test: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '1m', target: 50 },
      { duration: '30s', target: 500 },   // Spike up
      { duration: '3m', target: 500 },     // Hold spike
      { duration: '30s', target: 50 },     // Drop down
      { duration: '2m', target: 50 },      // Recovery
      { duration: '1m', target: 0 },
    ],
    gracefulRampDown: '30s',
  },
};

// Soak test scenario: sustained load for endurance testing
export const soakScenario = {
  soak_test: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '5m', target: 200 },
      { duration: '50m', target: 200 },   // Sustained load for ~1 hour
      { duration: '5m', target: 0 },
    ],
    gracefulRampDown: '30s',
  },
};

export default { getLoadScenario, smokeScenario, stressScenario, spikeScenario, soakScenario };
