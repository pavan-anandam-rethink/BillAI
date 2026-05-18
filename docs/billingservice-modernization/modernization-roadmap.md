# BillingService Non-Breaking Modernization Roadmap

## Architecture Target

The target architecture is Clean Architecture with the existing `BillingService.Web` controllers retained as compatibility endpoints until each workflow can move safely to CQRS handlers.

```text
API controllers
  -> Application commands/queries
  -> Domain model and policies
  -> Persistence interfaces
  -> Infrastructure adapters
  -> LegacyAdapters while migration is in progress
```

## Migration Phases

1. Baseline and protection
   - Inventory contracts, workflows, queries, integrations.
   - Add compatibility tests and golden-master regression fixtures.
2. Additive architecture foundation
   - Add SharedKernel, Contracts, Application, Infrastructure, Persistence, Workers, LegacyAdapters.
   - Register only safe adapters by default.
3. CQRS extraction
   - Start with read-only dashboard/search queries.
   - Use DTO projections, `AsNoTracking`, pagination, and compiled queries.
4. Redis decorators
   - Cache dashboard summaries, billing metrics, invoice summaries, lookup data, search results, and permissions.
   - Apply short TTLs and explicit invalidation on write workflows.
5. Database modernization
   - Apply additive indexes first.
   - Introduce read replicas for read-heavy dashboards/reports.
   - Introduce archival and partitioning only after usage baselines are captured.
6. Event-driven workflows
   - Add durable outbox table.
   - Move reporting, notifications, audit, and analytics to async workers.
7. AKS production readiness
   - Deploy stateless API pods, externalize config/secrets, enable HPA/KEDA, probes, PDBs, and topology spread.
8. Observability and resilience
   - Enable OpenTelemetry traces, structured logs, slow query metrics, queue depth, cache hit ratio, thread pool, and GC metrics.

## Rollback Strategy

- Keep existing controllers and domain services intact.
- Disable new paths with `BillingService:Modernization` feature flags.
- Roll back Helm release to prior revision for infrastructure-only changes.
- Use SQL rollback scripts for every index migration.
- Drain Service Bus subscriptions before disabling new worker consumers.

## Performance Expectations

| Change | Expected Gain |
| --- | --- |
| DTO projections and `AsNoTracking` | Lower EF memory and CPU per request |
| Dashboard cache | Lower dashboard DB read pressure and faster P95 |
| Query batching | Fewer database round-trips |
| Outbox + workers | Shorter synchronous request path for heavy side effects |
| HPA/KEDA | Independent scaling for API and async consumers |
| Read replica routing | Reduced contention between reporting and OLTP writes |

## Azure Capacity Guidance

### Initial Phase: 300 concurrent users

- AKS: 3 user nodes, `D4ds_v5` or equivalent, 2-4 API replicas.
- SQL: General Purpose or Business Critical tier sized from DTU/vCore telemetry; start with 4-8 vCores.
- Redis: Standard C1/C2 equivalent with persistence disabled for pure cache.
- Service Bus: Standard tier unless strict isolation/throughput requires Premium.

### Mid Phase: 700 concurrent users

- AKS: 3-zone node pool, 4-8 user nodes, HPA target CPU 65%, memory 75%.
- SQL: 8-16 vCores, read replica for dashboards/reports.
- Redis: Standard/Premium cache with zone redundancy where available.
- Service Bus: Premium namespace for predictable latency if queue depth grows.

### Enterprise Phase: 1500+ concurrent users

- AKS: separate system/user/worker node pools; 3-zone user pool with autoscaler; worker pool can use KEDA and dedicated CPU/memory requests.
- SQL: Business Critical or Hyperscale based on write/read split, zone redundancy, read replicas, PITR, geo-replication.
- Redis: Premium Enterprise tier with clustering if data volume requires it.
- HA/DR: zone-redundant AKS, SQL geo-replication, Redis geo-replication where supported, Service Bus geo-disaster recovery alias, Key Vault soft delete and purge protection.
