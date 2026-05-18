import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 100 },
    { duration: '30s', target: 1500 },
    { duration: '2m', target: 1500 },
    { duration: '30s', target: 100 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.02'],
    http_req_duration: ['p(95)<1500'],
  },
};

const baseUrl = __ENV.BILLING_BASE_URL || 'http://localhost:8080';

export default function () {
  const response = http.get(`${baseUrl}/api/health`);
  check(response, {
    'health endpoint is reachable': (r) => r.status === 200,
  });
  sleep(1);
}
