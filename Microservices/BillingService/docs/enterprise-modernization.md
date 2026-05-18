# BillingService Enterprise Modernization Plan

## Compatibility Contract

BillingService.App maps to the existing `BillingService.Web` ASP.NET Core host and `BillingService.Domain` service layer in this repository. The modernization keeps all existing controllers, route attributes, action names, request/response models, authentication middleware, EF contexts, repositories, and integration clients in place. New architecture is introduced at the host boundary and through additive deployment artifacts so existing consumers continue to call the same URLs and receive the same payload shapes.

## Current System Inventory

### API Contract Inventory

The Web host uses controller routes rather than minimal APIs. Most controllers use `[Route("[controller]/[action]")]`, preserving legacy URLs such as `/Claim/Search` and `/PaymentPosting/Get...`. Exceptions that must remain unchanged are:

- `ClientChargeHistoryController`: `[Route("[controller]")]` with explicit routes such as `history/{accountInfoId}` and `Search`.
- `PusherAuthController`: `[Route("pusher/auth")]`.
- `BillingSettingsController`: mixed action routes and REST-style route parameters.
- Health endpoint: `/api/health`.
- Swagger endpoint: `/swagger/v1/swagger.json`.

Controllers inventoried under `BillingService.Web/Controllers` include appointments, reports, claims, claim posting/update/notes/attachments, EDI files, clearing house, charge entry/payment, payment posting/notes/attachments, write-offs, patient invoices, billing settings, funder settings, client charge history, service-line adjustments, notifications, and Pusher auth.

### Business Workflow Map

- Claim creation/update/search/submission workflows are implemented in `ClaimService`, `ClaimCreateService`, `ClaimUpdateService`, `ClaimSyncService`, `ClaimValidationService`, `ClaimSearchService`, `ClaimManagerService`, `ClaimVersionService`, and `ClaimChangeTrackingService`.
- Payment workflows are implemented in `PaymentPostingService`, `PaymentClaimService`, `BulkPaymentPostingService`, `PaymentServiceLineAdjustmentService`, `PaymentAttachmentService`, `PaymentNoteService`, and `PaymentMethodService`.
- Invoice workflows are implemented in `PatientInvoiceService`, PDF rendering services, Razor templates, and billing storage/blob abstractions.
- EDI/clearinghouse workflows are implemented through `EdiGenerator`, EDI segment builders, `ClearingHouseService`, and `StediProviderEnrollmentService`.
- Report and dashboard-style reads flow through reporting repositories, `AppointmentReportService`, `ReportService`, search services, and summary endpoints.
- Master-data lookups flow through `RethinkMasterDataMicroServices`, `IRethinkMasterDataSessionCache`, Redis, and downstream HTTP clients.

### Database Dependency Map

- Primary EF contexts: `BillingDbContext` and `ReportingDbContext`.
- Connection strings are assembled in `IoCContainer.GetDBConnectionString`.
- Repository abstraction: `IRepository<,>` implemented by `Repository<,>`.
- Primary SQL workload categories: claim search, invoice aggregation, payment posting, reporting joins, dashboard summaries, attachment metadata, claim submission/funder sequencing, and write-off/service-line adjustment updates.

### External Integrations

- Azure Key Vault for secrets.
- Azure Blob Storage for billing files and generated artifacts.
- Redis through `StackExchange.Redis.IConnectionMultiplexer`.
- Azure Service Bus through existing message bus abstractions and legacy management client setup.
- Downstream Rethink microservice HTTP clients for accounts, curriculum, demographics, health plans, insurance, medical records, practice operations, and appointments.
- Pusher for realtime claim notifications.
- EDI Fabric, clearinghouse/Stedi, Rethink Print, and Rethink Mail integrations.
- Application Insights and existing Rethink logging.

### Authentication Flow

The service uses legacy custom middleware:

- Requests without `XApiKey` flow through `JwtMiddleware` and `BillingMasterDataRequestMiddleware`.
- Requests with `XApiKey` flow through `ApiKeyMiddleware`.
- Swagger documents both `Bearer` and `XApiKey` schemes.

This modernization does not replace those flows. Future JWT bearer/RBAC adoption should be versioned or placed behind feature flags.

## Dependency Graph

```text
BillingService.Web
  -> Authentication
  -> BillingService.Domain
      -> Rethink.Services.Common
          -> BillingDbContext / ReportingDbContext / Repository
      -> Rethink.Services.Domain
          -> MessageBus / HTTP client registration / Key Vault helpers
      -> SummationService.Domain
      -> EdiFabric.Templates
      -> Rethink.Billing.FolderStructure.Core
  -> Azure Blob Storage
  -> Azure Key Vault
  -> Redis
  -> Azure Service Bus
  -> Pusher
  -> Downstream Rethink APIs
```

## Risk Assessment

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Route or payload drift | Breaks frontend/reporting integrations | Do not change controllers, route attributes, DTOs, JSON serializers, or action signatures without contract tests. |
| Database schema changes | Breaks reports and legacy stored procedures | Use additive indexes only; no destructive schema changes without explicit migration window and rollback. |
| Cache inconsistency | Incorrect dashboard or billing values | Use short TTLs, explicit invalidation after writes, and cache-aside decorators only for read models. |
| Service Bus async migration | Duplicate or lost operations | Use outbox, idempotency keys, retry policies, and DLQ handling before moving transactional work async. |
| Startup blocking and secret calls | Cold-start latency/thread starvation | Move secret and management-client setup toward hosted startup checks over multiple safe iterations. |
| Rate limiting | Unexpected 429s | Disabled by default; enable per environment after load testing. |
| Response compression | Proxy/client compatibility | Disabled by default; enable only after smoke testing. |

## Regression Protection Strategy

1. Generate a route snapshot from MVC action descriptors and compare it in CI before route changes.
2. Add API contract tests for critical frontend/reporting endpoints using existing controller tests as the starting point.
3. Add golden master regression tests for claim calculations, payment allocation, invoice totals, write-offs, and EDI generation.
4. Capture representative SQL result sets for dashboard/reporting queries and validate row counts/totals before query rewrites.
5. Introduce cache decorators only around query/read services with tests proving fallback to source-of-truth and invalidation on writes.
6. Add outbox integration tests before moving notifications, audit logging, analytics, and reports to asynchronous workers.
7. Run k6 smoke, load, spike, and soak tests for dashboard/search/payment-posting paths against staging before enabling scaling policies.

## Target Clean Architecture

The next safe migration step is strangler-style layering around the existing domain:

- `BillingService.Contracts`: stable request/response contracts and integration event contracts.
- `BillingService.Application`: CQRS commands/queries, validators, pipeline behaviors, idempotency, and application services.
- `BillingService.Domain`: aggregates, value objects, domain services, and domain events extracted from current services.
- `BillingService.Persistence`: EF Core repositories, unit of work, specifications, compiled queries, and read-model projections.
- `BillingService.Infrastructure`: Redis, Service Bus, Blob, Key Vault, HTTP clients, OpenTelemetry, Polly policies.
- `BillingService.LegacyAdapters`: adapters that call existing services/controllers while commands/queries are migrated incrementally.
- `BillingService.Workers`: hosted workers for outbox dispatch, report generation, notifications, audit, and analytics.
- `BillingService.API`: future thin API host that delegates to Application while preserving legacy endpoints through adapters.

An additive scaffold for these boundaries now exists under `Microservices/BillingService/src`. It is intentionally not wired into legacy controllers yet; that keeps the current production path stable while giving future migration work concrete projects, contracts, and abstractions to use.

## Performance Engineering Plan

- Use `AsNoTracking` and DTO projections for read-only dashboard/reporting/search paths.
- Use compiled queries for hot lookup and dashboard summary queries.
- Add pagination boundaries for large search APIs while preserving existing defaults.
- Batch Service Bus messages and database reads where business order does not matter.
- Keep Redis as a singleton multiplexer and use TTL on every cache key.
- Replace per-request synchronous wait patterns with asynchronous startup/hosted initialization in later safe iterations.
- Monitor P95/P99 latency, DB duration, Redis hit ratio, queue depth, GC pressure, thread-pool starvation, and connection-pool usage.

## Redis Strategy

Key pattern: `billing:{environment}:{accountInfoId}:{entity}:{queryHash}`.

| Cache | TTL | Consistency |
| --- | ---: | --- |
| Dashboard summaries | 60-180 seconds | Invalidate after claim/payment/invoice write paths. |
| Billing metrics | 300 seconds | Rebuild on schedule and after bulk posting. |
| Invoice summaries | 120 seconds | Invalidate on invoice/payment/write-off changes. |
| Lookup data | 1-12 hours | Invalidate on admin setting/funder setting changes. |
| Search results | 30-60 seconds | Short TTL only; never cache mutable write responses. |
| User permissions | 5-15 minutes | Tie to auth/session invalidation. |

## Event-Driven Modernization Plan

Use an outbox table in the billing database with an AKS worker:

1. The request transaction writes business data and an outbox row.
2. `BillingService.Workers` reads undispatched rows with lease/idempotency controls.
3. Worker publishes to Azure Service Bus topics/queues.
4. Consumers use idempotency keys and dead-letter queues.
5. Operational dashboards track outbox age, dispatch failures, retry counts, and DLQ depth.

Initial async candidates: notifications, audit logging, analytics, report generation, and cache warming. Payment posting and claim submission stay synchronous until full regression coverage exists.

## Azure Capacity Recommendations

### Initial Phase: 300 Concurrent Users

- AKS: Standard tier, 3 zones, 2 system nodes (`D4s_v5`), 2-4 user nodes (`D4s_v5`), 3 Billing API replicas.
- SQL: Azure SQL Business Critical or General Purpose vCore sized from observed DTU/vCore load; start around 4-8 vCores with Query Store enabled.
- Redis: Azure Cache for Redis Standard/Premium C1-C2 depending payload size; TLS/auth required.
- Service Bus: Standard for low volume, Premium if predictable low latency or high message volume is needed.

### Mid Phase: 700 Concurrent Users

- AKS: 4-8 user nodes, HPA target CPU 65% and memory 75%, KEDA for queue workers.
- API replicas: 6-10 based on P95 latency and CPU.
- SQL: scale to 8-16 vCores, add read replicas for reports/dashboards where supported.
- Redis: Premium C2-C3 with clustering only if keyspace/throughput requires it.

### Enterprise Phase: 1500+ Concurrent Users

- AKS: separate system, API, worker, and optional spot/batch node pools across 3 zones; cluster autoscaler enabled.
- API replicas: 12+ with topology spread constraints and PDB minimum availability.
- SQL: Business Critical, zone redundant, read scale-out/read replicas for dashboards and reports, Query Store forced plans for hot paths.
- Redis: Premium/Enterprise tier with zone redundancy and private endpoints.
- HA/DR: paired-region backups, tested database restore, Service Bus geo-disaster recovery plan, ACR geo-replication, and GitOps rollback.

## Rollback Strategy

- Application: keep existing Web host and controllers as the rollback baseline; all new toggles default to compatibility-safe values.
- Kubernetes: use Helm revision rollback and keep previous image tags available in ACR.
- SQL: additive index scripts include matching rollback scripts.
- Cache: disable Redis decorators by configuration if hit ratios or consistency fail.
- Messaging: disable outbox dispatchers before rerouting synchronous workflows.
