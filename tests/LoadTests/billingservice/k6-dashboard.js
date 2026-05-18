import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  scenarios: {
    dashboard_concurrency: {
      executor: 'ramping-vus',
      stages: [
        { duration: '2m', target: 300 },
        { duration: '5m', target: 700 },
        { duration: '5m', target: 1500 },
        { duration: '2m', target: 0 },
      ],
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<1000', 'p(99)<2500'],
  },
};

const baseUrl = __ENV.BILLING_BASE_URL || 'http://localhost:8080';
const bearerToken = __ENV.BILLING_BEARER_TOKEN || '';

export default function () {
  const headers = bearerToken
    ? { Authorization: `Bearer ${bearerToken}`, 'Content-Type': 'application/json' }
    : { 'Content-Type': 'application/json' };

  const response = http.post(
    `${baseUrl}/Claim/GetClaimHeadersAsync`,
    JSON.stringify({
      accountInfoId: Number(__ENV.ACCOUNT_INFO_ID || 1),
      memberId: Number(__ENV.MEMBER_ID || 1),
      pageNumber: 1,
      pageSize: 25,
      filterModels: [],
      sortModels: [],
    }),
    { headers },
  );

  check(response, {
    'dashboard endpoint returns success': (r) => r.status >= 200 && r.status < 300,
    'dashboard endpoint p95 candidate under 1s': (r) => r.timings.duration < 1000,
  });

  sleep(1);
}
