# Observability and Telemetry Matrix

## Traces

- Incoming HTTP span per request.
- Outbound HTTP span to legacy BillingService and upstream dependencies.
- Service Bus publish spans from outbox worker.

## Metrics

- API latency (`p50/p95/p99`) by endpoint.
- Error rates by status code family.
- Redis cache hit/miss ratio.
- SQL query latency and timeout count.
- Outbox pending count and publish throughput.
- Service Bus DLQ count and active message count.
- Runtime: GC collections, heap size, thread pool queue length.

## Logs

- Structured logs with correlation ID (`X-Correlation-Id`).
- Request and CQRS execution duration logs.
- Retry and dispatch failures with event ID and outbox message ID.

## Alert recommendations

1. `p95 latency > 1s` for 5 minutes.
2. `5xx rate > 2%` for 5 minutes.
3. `cache hit ratio < 60%` sustained.
4. `DLQ messages > 0` for critical topics.
5. `outbox pending > threshold` for 10 minutes.

