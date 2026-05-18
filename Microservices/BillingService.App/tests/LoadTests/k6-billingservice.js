import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  scenarios: {
    smoke: {
      executor: "ramping-vus",
      startVUs: 10,
      stages: [
        { duration: "2m", target: 300 },
        { duration: "3m", target: 700 },
        { duration: "5m", target: 1500 },
        { duration: "2m", target: 0 }
      ]
    }
  },
  thresholds: {
    http_req_duration: ["p(95)<1000"],
    http_req_failed: ["rate<0.01"]
  }
};

const baseUrl = __ENV.BASE_URL || "http://localhost:8081";
const token = __ENV.BEARER_TOKEN || "";

export default function () {
  const headers = {
    "Content-Type": "application/json",
    "Authorization": token ? `Bearer ${token}` : ""
  };

  const claimPayload = JSON.stringify({
    accountInfoId: 1,
    pageIndex: 1,
    pageSize: 25
  });

  const res = http.post(`${baseUrl}/Claim/GetClaimHeaders`, claimPayload, { headers });
  check(res, {
    "status is 200": (r) => r.status === 200
  });

  sleep(0.25);
}

