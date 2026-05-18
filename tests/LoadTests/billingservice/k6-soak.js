import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: Number(__ENV.VUS || 700),
  duration: __ENV.DURATION || '30m',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<1000'],
  },
};

const baseUrl = __ENV.BILLING_BASE_URL || 'http://localhost:8080';

export default function () {
  const response = http.get(`${baseUrl}/api/health`);
  check(response, {
    'service stays healthy during soak': (r) => r.status === 200,
  });
  sleep(1);
}
