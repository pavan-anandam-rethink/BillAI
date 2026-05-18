import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  vus: 500,
  duration: "2h",
  thresholds: {
    http_req_duration: ["p(95)<1000"],
    http_req_failed: ["rate<0.01"]
  }
};

const baseUrl = __ENV.BASE_URL || "http://localhost:8081";

export default function () {
  const response = http.post(
    `${baseUrl}/Claim/GetClaimHeaders`,
    JSON.stringify({ accountInfoId: 1, pageIndex: 1, pageSize: 25 }),
    { headers: { "Content-Type": "application/json" } }
  );
  check(response, { "soak status 200": (r) => r.status === 200 });
  sleep(0.25);
}

