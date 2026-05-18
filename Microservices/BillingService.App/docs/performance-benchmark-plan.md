# Performance Benchmark and Capacity Plan

## Baseline assumptions

- Existing behavior remains unchanged.
- Modernization improves throughput by reducing blocking waits, adding caching, and scaling API/workers independently.

## Target SLAs

- P95 API latency < 1 second
- Dashboard load < 3 seconds
- 1500+ concurrent users under mixed read/write load

## Before vs after (expected engineering outcome)

| Metric | Legacy baseline pattern | Modernized target pattern |
|---|---|---|
| Claim header read latency | sync-heavy + no distributed cache | async + cache-aside with 30s TTL |
| DB contention | shared hotspots | read optimization + index coverage + split async workloads |
| API scale model | limited horizontal behavior | HPA-driven independent API/worker scale |
| Observability | partial request latency logs | OTel traces + metrics + correlation IDs |
| Async side effects | direct queue calls in business path | outbox + retry + DLQ handling |

## Load profiles

1. **Stress test**
   - ramp to 1500 VUs, hold 10 minutes.
2. **Spike test**
   - jump 100 -> 1200 VUs in < 30 seconds.
3. **Soak test**
   - 500 VUs for 2+ hours to capture leaks/thread starvation.
4. **Dashboard-focused read profile**
   - repeated dashboard and invoice summary queries.

## Runtime metrics to monitor

- API:
  - request rate
  - p50/p95/p99 latency
  - 4xx/5xx rates
- Runtime:
  - thread pool queue length
  - GC heap size/gen2 collections
  - CPU/memory saturation
- Data + cache:
  - SQL DTU/vCore pressure, waits, lock waits
  - Redis hit ratio and evictions
- Messaging:
  - queue depth
  - DLQ count
  - outbox pending records

