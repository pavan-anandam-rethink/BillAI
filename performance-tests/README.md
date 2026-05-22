# BH Billing Application - Performance Testing Framework

Production-grade k6 load testing framework for the BH Billing Application.

## Architecture

```
performance-tests/
├── config/                     # Configuration modules
│   ├── environments.js         # QA, STG, PROD environment configs
│   ├── scenarios.js            # k6 scenario definitions (load profiles)
│   └── thresholds.js           # SLA threshold definitions
├── data/                       # Test data files
│   └── users.json              # Test user credentials (rotated per VU)
├── scenarios/                  # User journey definitions
│   └── user-journey.js         # Complete billing workflow simulation
├── services/                   # API abstraction layer
│   ├── auth-service.js         # Authentication & token management
│   └── billing-api.js          # Billing API endpoint wrappers
├── utils/                      # Utility modules
│   ├── helpers.js              # Common helper functions
│   ├── http-client.js          # Base HTTP client with retry logic
│   ├── metrics.js              # Custom k6 metrics (Trend, Counter, Rate)
│   └── reporters.js            # HTML/JSON/CSV report generation
├── scripts/                    # Execution scripts
│   ├── run-tests.sh            # Single test runner
│   └── run-all-tiers.sh        # Run all concurrency tiers (100/200/300)
├── results/                    # Test results output (JSON, CSV)
├── reports/                    # Generated HTML reports
├── load-test.js                # Load test (100/200/300 VUs)
├── smoke-test.js               # Smoke test (5 VUs, 5 min)
├── stress-test.js              # Stress test (up to 500 VUs)
├── spike-test.js               # Spike test (sudden 500 VU burst)
├── soak-test.js                # Soak/endurance test (~60 min)
├── Dockerfile                  # Container image for k6 tests
├── docker-compose.yml          # k6 + InfluxDB + Grafana stack
├── azure-pipelines.yml         # Azure DevOps CI/CD pipeline
├── Jenkinsfile                 # Jenkins CI/CD pipeline
├── package.json                # NPM scripts for convenience
└── README.md                   # This file
```

## Prerequisites

### Install k6

```bash
# macOS
brew install k6

# Ubuntu/Debian
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg \
  --keyserver hkp://keyserver.ubuntu.com:80 \
  --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" \
  | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update && sudo apt-get install k6

# Windows
choco install k6

# Docker
docker pull grafana/k6
```

### Verify Installation

```bash
k6 version
```

## Quick Start

```bash
cd performance-tests

# Run smoke test (quick validation)
k6 run smoke-test.js -e ENV=qa

# Run load test with 100 concurrent users
k6 run load-test.js -e CONCURRENCY=100 -e ENV=qa

# Run load test with 300 concurrent users
k6 run load-test.js -e CONCURRENCY=300 -e ENV=qa

# Run with JSON + CSV output
k6 run load-test.js -e CONCURRENCY=300 --out json=results/load-300.json --out csv=results/load-300.csv
```

## Test Scenarios

| Scenario | Command | Description |
|----------|---------|-------------|
| **Smoke** | `k6 run smoke-test.js` | 5 VUs, 5 min — baseline validation |
| **Load (100)** | `k6 run load-test.js -e CONCURRENCY=100` | 100 VUs with ramp-up/down |
| **Load (200)** | `k6 run load-test.js -e CONCURRENCY=200` | 200 VUs with ramp-up/down |
| **Load (300)** | `k6 run load-test.js -e CONCURRENCY=300` | 300 VUs with ramp-up/down |
| **Stress** | `k6 run stress-test.js` | Ramp to 500 VUs to find limits |
| **Spike** | `k6 run spike-test.js` | Sudden burst to 500 VUs |
| **Soak** | `k6 run soak-test.js` | 200 VUs sustained for ~60 min |

## Execution Scripts

```bash
# Make scripts executable
chmod +x scripts/*.sh

# Run individual test
./scripts/run-tests.sh load 300 qa

# Run all tiers sequentially (100, 200, 300)
./scripts/run-all-tiers.sh qa
```

## Environment Configuration

Set the target environment via the `ENV` variable:

```bash
k6 run load-test.js -e ENV=qa       # QA environment (default)
k6 run load-test.js -e ENV=stg      # Staging environment
k6 run load-test.js -e ENV=prod     # Production environment
```

### Environment URLs

| Environment | Web URL | API URL |
|-------------|---------|---------|
| QA | `https://qa01-web.rethinkbhbeta.com` | `https://qa01-api.rethinkbhbeta.com` |
| STG | `https://stg-web.rethinkbhbeta.com` | `https://stg-api.rethinkbhbeta.com` |
| PROD | `https://web.rethinkbh.com` | `https://api.rethinkbh.com` |

## Transaction Groups

The framework simulates a complete user journey through these billing modules:

1. **Launch** — Application initial load
2. **Login** — User authentication
3. **Dashboard** — Main claims dashboard
4. **Pending Review** — Claims awaiting review
5. **Ready To Bill** — Claims ready for billing
6. **Billed Pending** — Submitted claims pending response
7. **Completed Claims** — Successfully processed claims
8. **Rejected Claims** — Claims rejected by payer
9. **Denied Claims** — Claims denied by payer
10. **Payment Posting** — Payment reconciliation
11. **Pending Collection** — Claims in collection

## SLA Thresholds

| Metric | Target |
|--------|--------|
| P95 Response Time | < 3,000ms |
| Error Rate | < 1% |
| P99 Response Time | < 5,000ms |
| Average Response Time | < 2,000ms |

## Test Data

### Users Configuration

Edit `data/users.json` with actual test user credentials:

```json
[
  {
    "username": "testuser@rethinkbh.com",
    "password": "password",
    "accountInfoId": 1
  }
]
```

Users are automatically rotated across VUs using modulo-based allocation. Each VU gets assigned a user based on `(VU_ID - 1) % total_users`.

## Report Generation

### Output Formats

All test scripts generate reports automatically via `handleSummary()`:

- **Console** — Real-time metrics during execution
- **HTML** — Self-contained report in `reports/` directory
- **JSON** — Raw metrics data in `results/` directory
- **CSV** — Tabular metrics in `results/` directory (via `--out csv=...`)

### Viewing Reports

```bash
# Open HTML report in browser
open reports/load-test-300vu_*.html

# Parse JSON results
cat results/load-test-300vu_*.json | jq '.metrics.http_req_duration.values'
```

## Docker Execution

### Standalone

```bash
# Build image
docker build -t bh-billing-perf-tests .

# Run load test
docker run -e ENV=qa -e CONCURRENCY=300 \
  -v $(pwd)/results:/app/results \
  -v $(pwd)/reports:/app/reports \
  bh-billing-perf-tests
```

### With Grafana + InfluxDB Monitoring

```bash
# Start monitoring stack
docker compose up -d influxdb grafana

# Run k6 test with InfluxDB output
docker compose run k6 run /scripts/load-test.js \
  -e ENV=qa -e CONCURRENCY=300

# Access Grafana dashboard
open http://localhost:3000
```

#### Grafana Dashboard Setup

1. Navigate to http://localhost:3000
2. Add InfluxDB data source: `http://influxdb:8086`, database: `k6`
3. Import k6 dashboard (ID: `2587`) from Grafana marketplace

## CI/CD Integration

### Azure DevOps

The `azure-pipelines.yml` file provides a parameterized pipeline:

1. Import the pipeline YAML into Azure DevOps
2. Run with parameters: Test Type, Concurrency, Environment
3. Results are published as build artifacts

### Jenkins

The `Jenkinsfile` provides a declarative pipeline:

1. Create a Pipeline job pointing to `performance-tests/Jenkinsfile`
2. Build with Parameters to select test configuration
3. Results are archived as build artifacts

## Scaling to 1000+ Users

### Distributed Execution

For large-scale tests exceeding single-machine capacity:

```bash
# Option 1: Use k6 Cloud
k6 cloud load-test.js -e CONCURRENCY=1000

# Option 2: Multiple Docker containers
# Run on multiple machines and aggregate results via InfluxDB
docker run -e ENV=qa -e CONCURRENCY=500 \
  -e K6_INFLUXDB_URL=http://influxdb-host:8086/k6 \
  bh-billing-perf-tests

# Option 3: Kubernetes
# Deploy k6 operator for distributed execution
# See: https://github.com/grafana/k6-operator
```

### Resource Requirements

| VUs | CPU Cores | RAM | Network |
|-----|-----------|-----|---------|
| 100 | 2 | 4 GB | 100 Mbps |
| 300 | 4 | 8 GB | 500 Mbps |
| 1000 | 8 | 16 GB | 1 Gbps |
| 5000+ | Distributed | Distributed | Distributed |

## Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| `k6: command not found` | Install k6 (see Prerequisites) |
| `ERRO[0000] open data/users.json` | Run from `performance-tests/` directory |
| `dial tcp: lookup failed` | Check network/DNS, verify environment URLs |
| `x509: certificate signed by unknown authority` | Add `--insecure-skip-tls-verify` flag |
| Authentication failures | Verify credentials in `data/users.json` |
| High error rates | Check API availability, increase timeouts |
| Memory issues at high VUs | Increase machine RAM, use distributed execution |
| `too many open files` | Increase ulimit: `ulimit -n 65536` |

### Debug Mode

```bash
# Run with verbose HTTP debug output
k6 run load-test.js --http-debug=full -e ENV=qa -e CONCURRENCY=5

# Run with k6 debug logging
K6_LOG_LEVEL=debug k6 run smoke-test.js
```

### Increase File Descriptors (Linux)

```bash
# Temporary
ulimit -n 65536

# Permanent: add to /etc/security/limits.conf
# * soft nofile 65536
# * hard nofile 65536
```

## Framework Features

- ✅ Dynamic user allocation with SharedArray optimization
- ✅ Environment variable support (QA/STG/PROD)
- ✅ Retry mechanism (3 retries with logging)
- ✅ Correlation extraction (JSON path-based)
- ✅ Authorization token management
- ✅ Custom metrics (Trend, Counter, Rate, Gauge)
- ✅ Transaction grouping (11 billing modules)
- ✅ SLA thresholds with pass/fail reporting
- ✅ HTML report generation
- ✅ JSON + CSV export
- ✅ Error logging and failure tracking
- ✅ Realistic think time simulation
- ✅ CI/CD ready (Azure DevOps + Jenkins)
- ✅ Docker compatible
- ✅ Grafana + InfluxDB integration support
- ✅ Modular, reusable architecture
