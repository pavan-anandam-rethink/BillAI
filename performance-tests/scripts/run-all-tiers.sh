#!/bin/bash
# ============================================================================
# Run All Load Test Tiers (100, 200, 300 VUs)
# ============================================================================
# Executes load tests sequentially at each concurrency level.
# Results are saved with unique timestamps per run.
#
# Usage:
#   ./scripts/run-all-tiers.sh [environment]
#   ./scripts/run-all-tiers.sh qa
# ============================================================================

set -e

ENVIRONMENT="${1:-qa}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "============================================"
echo "  Running All Load Test Tiers"
echo "  Environment: ${ENVIRONMENT}"
echo "============================================"

# Tier 1: 100 VUs
echo ""
echo ">>> TIER 1: 100 Concurrent Users <<<"
"${SCRIPT_DIR}/run-tests.sh" load 100 "${ENVIRONMENT}" || true

# Cool-down between tiers
echo "Cooling down for 60 seconds..."
sleep 60

# Tier 2: 200 VUs
echo ""
echo ">>> TIER 2: 200 Concurrent Users <<<"
"${SCRIPT_DIR}/run-tests.sh" load 200 "${ENVIRONMENT}" || true

# Cool-down between tiers
echo "Cooling down for 60 seconds..."
sleep 60

# Tier 3: 300 VUs
echo ""
echo ">>> TIER 3: 300 Concurrent Users <<<"
"${SCRIPT_DIR}/run-tests.sh" load 300 "${ENVIRONMENT}" || true

echo ""
echo "============================================"
echo "  ALL TIERS COMPLETED"
echo "  Results saved in results/ directory"
echo "============================================"
