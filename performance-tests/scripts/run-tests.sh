#!/bin/bash
# ============================================================================
# BH Billing Application - Performance Test Execution Script
# ============================================================================
# Usage:
#   ./scripts/run-tests.sh [test-type] [concurrency] [environment]
#
# Examples:
#   ./scripts/run-tests.sh smoke                  # Smoke test on QA
#   ./scripts/run-tests.sh load 100 qa            # 100 VU load test on QA
#   ./scripts/run-tests.sh load 300 stg           # 300 VU load test on STG
#   ./scripts/run-tests.sh stress                 # Stress test on QA
#   ./scripts/run-tests.sh spike                  # Spike test on QA
#   ./scripts/run-tests.sh soak                   # Soak test on QA
# ============================================================================

set -e

# Default parameters
TEST_TYPE="${1:-smoke}"
CONCURRENCY="${2:-100}"
ENVIRONMENT="${3:-qa}"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# Colors for console output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}============================================================${NC}"
echo -e "${BLUE}  BH BILLING - PERFORMANCE TEST RUNNER${NC}"
echo -e "${BLUE}============================================================${NC}"
echo -e "  Test Type:    ${GREEN}${TEST_TYPE}${NC}"
echo -e "  Concurrency:  ${GREEN}${CONCURRENCY} VUs${NC}"
echo -e "  Environment:  ${GREEN}${ENVIRONMENT}${NC}"
echo -e "  Timestamp:    ${GREEN}${TIMESTAMP}${NC}"
echo -e "${BLUE}============================================================${NC}"

# Ensure output directories exist
mkdir -p results reports

# Determine which test script to run
case $TEST_TYPE in
  smoke)
    SCRIPT="smoke-test.js"
    ;;
  load)
    SCRIPT="load-test.js"
    ;;
  stress)
    SCRIPT="stress-test.js"
    ;;
  spike)
    SCRIPT="spike-test.js"
    ;;
  soak)
    SCRIPT="soak-test.js"
    ;;
  *)
    echo -e "${RED}Error: Unknown test type '${TEST_TYPE}'${NC}"
    echo "Valid options: smoke, load, stress, spike, soak"
    exit 1
    ;;
esac

# Build the k6 command
K6_CMD="k6 run ${SCRIPT}"
K6_CMD="${K6_CMD} -e ENV=${ENVIRONMENT}"
K6_CMD="${K6_CMD} -e CONCURRENCY=${CONCURRENCY}"
K6_CMD="${K6_CMD} --out json=results/${TEST_TYPE}_${CONCURRENCY}vu_${TIMESTAMP}.json"
K6_CMD="${K6_CMD} --out csv=results/${TEST_TYPE}_${CONCURRENCY}vu_${TIMESTAMP}.csv"

# Add InfluxDB output if configured
if [ -n "$K6_INFLUXDB_URL" ]; then
  K6_CMD="${K6_CMD} --out influxdb=${K6_INFLUXDB_URL}"
  echo -e "  InfluxDB:     ${GREEN}${K6_INFLUXDB_URL}${NC}"
fi

echo ""
echo -e "${YELLOW}Executing: ${K6_CMD}${NC}"
echo ""

# Execute the test
eval $K6_CMD

# Check exit code
EXIT_CODE=$?
if [ $EXIT_CODE -eq 0 ]; then
  echo -e "\n${GREEN}============================================================${NC}"
  echo -e "${GREEN}  TEST COMPLETED SUCCESSFULLY${NC}"
  echo -e "${GREEN}============================================================${NC}"
else
  echo -e "\n${RED}============================================================${NC}"
  echo -e "${RED}  TEST COMPLETED WITH THRESHOLD FAILURES (Exit Code: ${EXIT_CODE})${NC}"
  echo -e "${RED}============================================================${NC}"
fi

echo -e "\n  Results: results/${TEST_TYPE}_${CONCURRENCY}vu_${TIMESTAMP}.json"
echo -e "  CSV:     results/${TEST_TYPE}_${CONCURRENCY}vu_${TIMESTAMP}.csv"
echo -e "  Reports: reports/"

exit $EXIT_CODE
