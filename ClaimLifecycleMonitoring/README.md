# ClaimLifecycleMonitoring

Enterprise-grade centralized **Claim Lifecycle Monitoring, Tracking, Alerting and Maintenance** platform for a healthcare billing system.

Built with **.NET 8**, **ASP.NET Core**, **Entity Framework Core**, **SignalR**, **MediatR**, **FluentValidation**, **Serilog** and **Azure Application Insights**, following **Clean Architecture** and **CQRS** principles.

---

## Business objective

The platform continuously monitors every healthcare claim from **Appointment Created** through **Completed** and answers, in near real time:

- Where is the claim now?
- Which stage is it in? Is it progressing?
- Has any processing failed? What exception occurred?
- Does it require manual intervention?
- Can the claim be retried?
- What is the full history of the claim?

Automatic detections include: stuck claims, timeouts, validation / eligibility / authorization / 837 generation / 837 submission failures, missing 999 / 277CA / 835 acknowledgements, payment posting failures, invoice failures, duplicate claims, cancelled and reprocessed claims, and SLA breaches.

---

## Claim lifecycle

```
Appointment Created → Eligibility Verification → Authorization →
Claim Created → Claim Validation → 837 Generated → 837 Submitted →
999 Received → 277CA Received → Payer Processing → 835 Received →
Payment Posting → Patient Invoice → Completed
```

Encoded in `ClaimStage` (Domain) with pipeline navigation in `ClaimLifecyclePipeline`.

---

## Solution layout

```
ClaimLifecycleMonitoring/
├── ClaimLifecycleMonitoring.sln
├── global.json                       # Pins the .NET 8 SDK
├── scripts/sql/                      # Schema + seed SQL scripts
├── src/
│   ├── ClaimLifecycleMonitoring.Domain/          # Aggregates, enums, exceptions
│   ├── ClaimLifecycleMonitoring.Contracts/       # DTOs / API contracts
│   ├── ClaimLifecycleMonitoring.Application/     # CQRS handlers, validators,
│   │                                             # rule engine, monitoring service
│   ├── ClaimLifecycleMonitoring.Persistence/     # EF Core, repositories, seeder
│   ├── ClaimLifecycleMonitoring.Infrastructure/  # Notification publisher, DI
│   ├── ClaimLifecycleMonitoring.API/             # REST API, SignalR hub, Swagger
│   └── ClaimLifecycleMonitoring.Worker/          # Background monitoring worker
└── tests/
    └── ClaimLifecycleMonitoring.Tests/           # xUnit + FluentAssertions
```

### Clean Architecture reference direction

```
API   ─────┐
Worker ────┤──▶ Infrastructure ──▶ Application ──▶ Domain
Persistence ┘             ▲            │
                          └── Contracts ┘
```

---

## Key building blocks

| Concern | Component |
|---|---|
| Aggregate root | `Domain.Entities.Claim` — enforces state transitions, retries, cancellation, duplicate detection, payments and alert raising |
| Lifecycle pipeline | `Domain.Common.ClaimLifecyclePipeline` — next-stage lookups |
| CQRS | MediatR commands (`CreateClaim`, `AdvanceClaimStage`, `RecordFailure`, `RetryClaim`, `CancelClaim`, `MarkDuplicate`, `ApplyPayment`) + queries (`GetClaimById`, `GetClaimHistory`, `SearchClaims`, `GetDashboardSnapshot`, alerts) |
| Validation | FluentValidation `Common.Behaviors.ValidationBehavior` pipeline behavior |
| Logging | `Common.Behaviors.LoggingBehavior` MediatR behavior + Serilog structured logging |
| Rule engine | `Application.Monitoring.Rules.CompositeRuleEngine` with rules: `StageSlaBreachRule`, `GlobalTimeoutRule`, `Missing999Rule`, `Missing277CaRule`, `Missing835Rule`, `UnalertedFailureRule` |
| Background monitor | `Worker.ClaimMonitoringWorker` (BackgroundService) → `Application.Monitoring.ClaimMonitoringService` |
| Real-time push | `API.Realtime.ClaimMonitoringHub` (SignalR) + `SignalRClaimBroadcaster` |
| Persistence | `ClaimLifecycleDbContext` (schema `monitoring`), SQL Server or In-Memory fallback |

---

## Configuration

`appsettings.json` (API and Worker):

```jsonc
{
  "ConnectionStrings": {
    "ClaimLifecycleDb": "Server=.;Database=ClaimLifecycle;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Persistence": {
    "UseInMemory": "false"          // set to "true" to force EF InMemory (default for dev)
  },
  "Monitoring": {
    "ScanIntervalSeconds": 60,
    "BatchSize": 500,
    "DefaultStageSlaHours": 24,
    "GlobalClaimTimeoutHours": 720,
    "Expected999Hours": 24,
    "Expected277CaHours": 72,
    "Expected835Hours": 720
  },
  "ApplicationInsights": {
    "ConnectionString": ""          // Populate to enable Azure Monitor / App Insights
  }
}
```

If the connection string is missing (or `Persistence:UseInMemory=true`), both the API and the Worker will boot against an **EF Core In-Memory** database — no SQL Server required to run locally.

---

## Running

```bash
# From the ClaimLifecycleMonitoring folder
dotnet build ClaimLifecycleMonitoring.sln

# API (Swagger at https://localhost:5001/swagger, health at /health/ready)
dotnet run --project src/ClaimLifecycleMonitoring.API

# Background monitoring worker
dotnet run --project src/ClaimLifecycleMonitoring.Worker

# Unit tests
dotnet test
```

The API creates the schema and seeds demo data on startup (SQL Server: `EnsureCreated`; production deployments should use the SQL scripts under `scripts/sql/`).

---

## REST endpoints (v1)

| Method | Route | Purpose |
|---|---|---|
| `POST`  | `/api/v1/claims`                              | Create a new claim |
| `POST`  | `/api/v1/claims/{claimNumber}/advance`        | Advance to next stage |
| `POST`  | `/api/v1/claims/{claimNumber}/failure`        | Record a failure |
| `POST`  | `/api/v1/claims/{claimNumber}/retry`          | Retry a failed claim |
| `POST`  | `/api/v1/claims/{claimNumber}/cancel`         | Cancel a claim |
| `POST`  | `/api/v1/claims/{claimNumber}/duplicate`      | Mark as duplicate |
| `POST`  | `/api/v1/claims/{claimNumber}/payments`       | Apply a payment |
| `GET`   | `/api/v1/claims/{claimNumber}`                | Get claim by number |
| `GET`   | `/api/v1/claims/{claimNumber}/history`        | Get claim history |
| `GET`   | `/api/v1/claims`                              | Paged search |
| `GET`   | `/api/v1/dashboard`                           | Dashboard snapshot |
| `GET`   | `/api/v1/alerts`                              | List open alerts |
| `POST`  | `/api/v1/alerts/{alertId}/acknowledge`        | Acknowledge alert |
| `POST`  | `/api/v1/monitoring/scan`                     | Trigger monitoring scan on demand |
| `GET`   | `/health/live`                                | Liveness probe |
| `GET`   | `/health/ready`                               | Readiness probe (includes DB) |

### SignalR hub

`/hubs/claim-monitoring` — pushes `ClaimUpdated` and `AlertRaised` events to connected dashboards.

---

## Dashboard metrics

The `GET /api/v1/dashboard` endpoint returns totals for: total claims, in-progress, completed, failed, waiting, rejected, pending payment, pending 999/277CA/835, stuck claims, claims requiring manual action, top failure reasons, claims-by-customer, claims-by-account and claims-by-status.

---

## Detection rules

| Rule | Fires when |
|---|---|
| `StageSlaBreachRule`  | Time in current stage exceeds its configured SLA; escalates to *stuck* when > 150% of SLA |
| `GlobalTimeoutRule`   | Total claim age exceeds `Monitoring:GlobalClaimTimeoutHours` |
| `Missing999Rule`      | Claim sat in `EdiSubmitted837` beyond `Expected999Hours` |
| `Missing277CaRule`    | Claim sat in `Received999` beyond `Expected277CaHours` |
| `Missing835Rule`      | Claim sat in `PayerProcessing` beyond `Expected835Hours` |
| `UnalertedFailureRule`| A recorded failure has no matching recent alert |

Rules run in the background worker every `ScanIntervalSeconds` and are re-runnable on demand via `POST /api/v1/monitoring/scan`.

---

## Observability

- **Serilog** with console + rolling file sinks (`logs/api-*.log`, `logs/worker-*.log`) and enrichers for machine name, process id and thread id.
- **Serilog request logging** middleware for HTTP correlation.
- **Correlation ID** propagated end-to-end via `X-Correlation-Id` (see `CorrelationIdMiddleware`).
- **Global exception handling** middleware converts domain / validation / not-found exceptions into RFC 7807-style JSON responses.
- **Azure Application Insights** telemetry (API + Worker) — enabled by populating `ApplicationInsights:ConnectionString`.
- **Health checks** — DB context health check and (when SQL Server is configured) explicit SQL check, exposed at `/health/live` and `/health/ready`.

---

## Testing

`dotnet test` runs an xUnit test suite covering:

- Claim aggregate state machine (create, advance, failure, retry, payment, cancel, duplicate)
- Rule engine behaviors (`StageSlaBreachRule`, `GlobalTimeoutRule`, `Missing999Rule`, `Missing835Rule`, `UnalertedFailureRule`)

Uses FluentAssertions and, where handlers are exercised, EF Core In-Memory.

---

## SQL scripts

- `scripts/sql/001-create-schema.sql` — creates the `monitoring` schema and all tables with indexes and foreign keys.
- `scripts/sql/002-seed-sla-configuration.sql` — seeds baseline per-stage SLAs.

Apply in order using `sqlcmd`, EF Core migrations or your deployment pipeline of choice.

---

## Coding standards

- SOLID + Clean Code
- Async / await throughout
- Enterprise design patterns: CQRS, Repository, Unit of Work, Pipeline Behaviors, Composite (rule engine)
- XML documentation on all public members
- Options pattern for configuration
- Constructor DI, sealed classes, immutable records for commands / queries
- No `TODO`s, no placeholders — code is production-ready and the solution builds warning-free
