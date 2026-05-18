# BillingService.App Migration Roadmap (Incremental, Non-Breaking)

## Guiding principle

No behavioral rewrite. Migrate one vertical slice at a time while all non-migrated APIs continue to execute through the legacy implementation.

## Phase sequence

1. **Phase 1 - Baseline and guardrails** (done in this foundation)
   - System analysis, dependency maps, risk register.
   - Contract/regression test scaffolding.
2. **Phase 2 - Clean Architecture shell**
   - New `BillingService.App` projects with API/Application/Domain/Infrastructure/Persistence boundaries.
   - Compatibility reverse proxy + feature toggles.
3. **Phase 3 - CQRS incremental migration**
   - Migrate highest-read endpoints first (dashboard/claim listing) with strict parity checks.
   - Introduce MediatR pipeline behaviors (validation, performance, correlation).
4. **Phase 4 - Performance and data optimization**
   - AsNoTracking, compiled queries, projection mapping.
   - SQL index additions (safe non-schema-breaking).
5. **Phase 5 - Event-driven hardening**
   - Outbox, retry policies, DLQ and idempotency.
6. **Phase 6 - Cloud-native runtime**
   - AKS manifests, Helm chart, HPA, probes, PDB, rolling updates.
7. **Phase 7 - Observability and security**
   - OpenTelemetry, structured logs, correlation IDs, rate limiting, JWT/RBAC hardening.
8. **Phase 8 - Progressive cutover**
   - Disable proxy route per endpoint as migrated parity is proven.

## Target endpoint migration ordering

1. Read-heavy low-risk:
   - claim listing/search
   - dashboard summaries
   - invoice summaries
2. Medium-risk:
   - settings and lookup maintenance
3. High-risk transactional:
   - claim submission
   - payment posting
   - clearinghouse and EDI paths

## Compatibility controls

- Endpoint toggle naming:
  - `BillingModernization:ClaimHeadersMigrationMode` (`Off`/`Shadow`/`On`)
  - `Features:UseCqrsDashboard`
  - `Features:UseLegacyProxyFallback`
- Rollout modes:
  - off -> legacy only
  - shadow -> execute both, compare, return legacy
  - on -> return modernized handler

## Azure scaling recommendations

### Initial phase (300 concurrent users)

- AKS: 3 nodes, `Standard_D4s_v5` equivalent (4 vCPU / 16 GB each)
- API replicas: min 3, max 8
- SQL: Business Critical 4 vCore equivalent
- Redis: C2/C3 equivalent, non-clustered with persistence

### Mid phase (700 concurrent users)

- AKS: split node pools
  - app pool: 5-8 nodes
  - worker pool: 3-5 nodes
- API replicas: min 6, max 20 (CPU+RPS autoscaling)
- SQL: 8-12 vCore with read replica for reporting
- Redis: clustered (2 shard) with replica

### Enterprise phase (1500+ concurrent users)

- AKS multi-zone node pools:
  - api general pool
  - async worker pool
  - burst/spot analytics pool
- API replicas: min 12, max 50
- SQL: 16+ vCore business critical + read replicas + geo-replica
- Redis: clustered premium, 3+ shards with replicas
- Service Bus premium namespace with partitioned entities

## HA/DR topology

- Single-tenant per environment
- AZ-level redundancy for AKS + SQL + Redis + Service Bus
- Blue/green or canary releases with APIM routing
- RPO/RTO targets validated with chaos drills and failover runbooks

