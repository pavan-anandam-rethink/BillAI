import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  scenarios: {
    spike: {
      executor: "ramping-vus",
      startVUs: 100,
      stages: [
        { duration: "1m", target: 100 },
        { duration: "30s", target: 1200 },
        { duration: "2m", target: 1200 },
        { duration: "30s", target: 100 },
        { duration: "2m", target: 100 }
      ]
    }
  }
};

const baseUrl = __ENV.BASE_URL || "http://localhost:8081";

export default function () {
  const response = http.post(
    `${baseUrl}/Claim/GetClaimHeaders`,
    JSON.stringify({ accountInfoId: 1, pageIndex: 1, pageSize: 10 }),
    { headers: { "Content-Type": "application/json" } }
  );
  check(response, { "spike status 200": (r) => r.status === 200 });
  sleep(0.1);
}

