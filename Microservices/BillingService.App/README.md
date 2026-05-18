# BillingService.App

Enterprise modernization foundation for Billing Service with **non-breaking compatibility guarantees**.

## Why this exists

The current billing system already supports live business workflows. A big-bang rewrite would be high risk.
This application provides a **Clean Architecture target** while preserving existing behavior through:

1. **Compatibility proxying** to legacy BillingService endpoints for non-migrated APIs.
2. **Incremental CQRS migration** for selected read/write flows.
3. **Feature flags** to control rollout and fast rollback.

## Target structure

```text
BillingService.App/
├── src/
│   ├── BillingService.API
│   ├── BillingService.Application
│   ├── BillingService.Domain
│   ├── BillingService.Infrastructure
│   ├── BillingService.Persistence
│   ├── BillingService.Contracts
│   ├── BillingService.SharedKernel
│   ├── BillingService.Workers
│   └── BillingService.LegacyAdapters
├── tests/
│   ├── UnitTests
│   ├── IntegrationTests
│   ├── ContractTests
│   ├── RegressionTests
│   └── LoadTests
├── docker/
├── k8s/
├── helm/
└── scripts/
```

## Non-breaking strategy

- Existing frontend and integrations continue using the same endpoint paths and auth headers.
- Legacy behavior is preserved by forwarding all non-migrated routes to legacy BillingService.
- Migrated flows are validated with regression and contract tests before enabling rollout flags.
- Rollback path is immediate: disable migrated route flag and route back to legacy.

## Production capabilities implemented in this foundation

- .NET 8
- Clean Architecture boundaries
- CQRS + MediatR pipeline
- Redis distributed cache abstraction
- Service Bus outbox publisher contract
- OpenTelemetry tracing/metrics/log correlation
- Docker + Kubernetes + Helm deployment artifacts
- k6 load test baseline

## Documentation

- `docs/phase1-current-system-analysis.md`
- `docs/migration-roadmap.md`
- `docs/rollback-and-regression-strategy.md`

