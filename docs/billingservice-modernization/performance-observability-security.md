# BillingService Performance, Observability, and Security Plan

## Performance Engineering Controls

- Use `AsNoTracking` and DTO projections for dashboard/search/read-only queries.
- Add compiled queries only after query-shape stability is confirmed by regression tests.
- Use pagination for all search APIs and reject unbounded page sizes at the application boundary after contract review.
- Batch external lookups by account and user identifiers.
- Move reporting, notifications, audit fan-out, and analytics to Service Bus-backed workers through the outbox.
- Keep write workflows transactional and synchronous only for user-visible state changes.

## Redis Caching Strategy

| Data | TTL | Consistency |
| --- | --- | --- |
| Dashboard summaries | 2 minutes | Invalidate on claim/payment/invoice writes |
| Billing metrics | 5 minutes | Invalidate on billing write completion |
| Invoice summaries | 3 minutes | Invalidate on invoice/payment writes |
| Lookup data | 6 hours | Invalidate on settings import/update |
| Search results | 1 minute | Short-lived only |
| User permissions | 10 minutes | Invalidate on auth/role changes |

Keys use the `billing:{tenant:<accountInfoId>}:<area>:<parts>` convention for tenant-scoped data and `billing:{global:shared}:<area>:<parts>` for global lookup data.

## Observability Signals

- API latency: P50/P95/P99 by route and status code.
- Database latency: SQL command duration, exceptions, connection pool pressure.
- Cache: hit ratio, miss ratio, serialization failures, Redis latency.
- Queues: active messages, dead-letter count, oldest message age.
- Runtime: CPU, memory, GC pause time, thread pool queue length, exception rate.
- Business: claim submission count, payment posting count, invoice generation count, EDI failure count.

## Security and Resilience

- Preserve current custom JWT/XApiKey flows until a versioned standard JWT bearer policy migration is ready.
- Use managed identity and Key Vault for AKS-hosted secrets.
- Apply API Management rate limits at the edge and ASP.NET Core rate limiting after endpoint baselines are captured.
- Apply retries only to idempotent external calls.
- Use circuit breakers for downstream HTTP and Service Bus operations.
- Keep destructive write retries behind explicit idempotency keys.

## Before vs After Expectations

| Area | Current Risk | Modernized Target |
| --- | --- | --- |
| Dashboard reads | Repeated DB aggregation | Redis-backed summaries and read-model queries |
| Heavy side effects | Request thread performs fan-out | Outbox and workers |
| Query shape | Include-heavy EF materialization | DTO projections and targeted indexes |
| Deployment | Web App/container-focused | AKS with HPA, PDB, probes |
| Observability | App Insights logging | OpenTelemetry distributed tracing and runtime metrics |
