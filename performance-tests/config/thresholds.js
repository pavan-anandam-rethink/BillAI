/**
 * SLA Thresholds Configuration
 * -----------------------------
 * Centralized threshold definitions for all performance scenarios.
 * SLA targets:
 *   - P95 response time < 3 seconds
 *   - Error rate < 1%
 */

// Default SLA thresholds applied to every test scenario
export const defaultThresholds = {
  // Global HTTP metrics
  http_req_duration: ['p(95)<3000', 'p(99)<5000', 'avg<2000'],
  http_req_failed: ['rate<0.01'],
  http_reqs: ['rate>0'],

  // Custom transaction metrics (populated dynamically)
  'transaction_duration{transaction:Launch}': ['p(95)<3000'],
  'transaction_duration{transaction:Login}': ['p(95)<5000'],
  'transaction_duration{transaction:Dashboard}': ['p(95)<3000'],
  'transaction_duration{transaction:PendingReview}': ['p(95)<3000'],
  'transaction_duration{transaction:ReadyToBill}': ['p(95)<3000'],
  'transaction_duration{transaction:BilledPending}': ['p(95)<3000'],
  'transaction_duration{transaction:CompletedClaims}': ['p(95)<3000'],
  'transaction_duration{transaction:RejectedClaims}': ['p(95)<3000'],
  'transaction_duration{transaction:DeniedClaims}': ['p(95)<3000'],
  'transaction_duration{transaction:PaymentPosting}': ['p(95)<3000'],
  'transaction_duration{transaction:PendingCollection}': ['p(95)<3000'],

  // Custom error rate
  'error_rate': ['rate<0.01'],
};

// Smoke test thresholds (stricter for baseline validation)
export const smokeThresholds = {
  http_req_duration: ['p(95)<2000', 'avg<1000'],
  http_req_failed: ['rate<0.01'],
};

// Stress test thresholds (relaxed under extreme load)
export const stressThresholds = {
  http_req_duration: ['p(95)<5000', 'p(99)<8000'],
  http_req_failed: ['rate<0.05'],
};

// Soak test thresholds (same as default, for endurance validation)
export const soakThresholds = {
  ...defaultThresholds,
};

export default defaultThresholds;
