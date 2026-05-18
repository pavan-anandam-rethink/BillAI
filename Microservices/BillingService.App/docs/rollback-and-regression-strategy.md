# Rollback and Regression Safety Strategy

## Rollback levels

1. **Level 0 - Endpoint toggle rollback (seconds)**
   - Disable migrated endpoint flag.
   - Route traffic back to legacy proxy.
2. **Level 1 - Deployment rollback (minutes)**
   - Helm rollback to previous revision.
3. **Level 2 - Data safety rollback**
   - Revert non-destructive index scripts.
   - Keep schema untouched during early phases.

## Regression gates (release blocking)

1. Contract tests pass for all migrated endpoints.
2. Snapshot response parity for representative payloads.
3. DB parity checks for key billing workflows.
4. Load profile passes target thresholds:
   - P95 < 1s for key APIs
   - dashboard < 3s
5. Observability baselines:
   - no unexplained error-rate increase
   - queue depth and dead-letter stable

## SQL migration safety

- All DB scripts are forward-only and reversible where possible.
- Index migrations include explicit rollback scripts.
- No table/column contract changes in compatibility phase.

## Service Bus safety

- Use outbox with idempotency key.
- Retry with capped exponential backoff.
- Dead-letter handling with replay tooling.

## Redis safety

- Cache-aside for read paths only.
- TTL + selective invalidation on write events.
- Cache failure must not fail business transaction.

## Backward compatibility tests

- Legacy route contract tests (status, shape, key fields).
- Auth compatibility tests:
  - JWT flow
  - XApiKey flow
- Integration tests with existing frontend request patterns.

