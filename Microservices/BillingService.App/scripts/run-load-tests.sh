#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:8081}"
BEARER_TOKEN="${BEARER_TOKEN:-}"

echo "Running k6 load test against ${BASE_URL}"
k6 run \
  -e BASE_URL="${BASE_URL}" \
  -e BEARER_TOKEN="${BEARER_TOKEN}" \
  tests/LoadTests/k6-billingservice.js

