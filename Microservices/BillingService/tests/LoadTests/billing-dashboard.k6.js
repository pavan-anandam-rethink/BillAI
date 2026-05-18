import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

export const dashboardLatency = new Trend('billing_dashboard_latency');
export const billingErrors = new Rate('billing_errors');

export const options = {
  scenarios: {
    smoke: {
      executor: 'constant-vus',
      vus: 5,
      duration: '1m',
      exec: 'dashboardScenario',
      tags: { phase: 'smoke' },
    },
    load_300_users: {
      executor: 'ramping-vus',
      stages: [
        { duration: '5m', target: 300 },
        { duration: '10m', target: 300 },
        { duration: '3m', target: 0 },
      ],
      exec: 'dashboardScenario',
      startTime: '90s',
      tags: { phase: 'initial' },
    },
    stress_1500_users: {
      executor: 'ramping-vus',
      stages: [
        { duration: '10m', target: 700 },
        { duration: '10m', target: 1500 },
        { duration: '10m', target: 1500 },
        { duration: '5m', target: 0 },
      ],
      exec: 'dashboardScenario',
      startTime: '21m',
      tags: { phase: 'enterprise' },
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<1000'],
    billing_dashboard_latency: ['p(95)<1000'],
  },
};

const baseUrl = __ENV.BILLING_BASE_URL || 'https://billing.example.com';
const bearerToken = __ENV.BILLING_BEARER_TOKEN || '';
const apiKey = __ENV.BILLING_X_API_KEY || '';

export function dashboardScenario() {
  const headers = {
    'Content-Type': 'application/json',
    'X-Correlation-ID': `k6-${__VU}-${Date.now()}`,
  };

  if (bearerToken) {
    headers.Authorization = `Bearer ${bearerToken}`;
  }

  if (apiKey) {
    headers.XApiKey = apiKey;
  }

  const payload = JSON.stringify({
    pageNumber: 1,
    pageSize: 25,
    searchText: '',
  });

  const response = http.post(`${baseUrl}/Claim/Search`, payload, { headers });
  dashboardLatency.add(response.timings.duration);
  billingErrors.add(response.status >= 400);

  check(response, {
    'search returned success': (r) => r.status >= 200 && r.status < 400,
    'p95 target candidate': (r) => r.timings.duration < 1000,
  });

  sleep(Math.random() * 2 + 1);
}
