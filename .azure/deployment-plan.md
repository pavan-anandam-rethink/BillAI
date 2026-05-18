# BillingService Modernization Deployment Plan

Status: Ready for Validation

## Scope

Prepare the existing BillingService implementation for a gradual, non-breaking migration toward a Clean Architecture, AKS-ready, observable .NET 8 microservice.

## Current Application

- Existing host: `Microservices/BillingService/BillingService.Web`
- Existing domain/services: `Microservices/BillingService/BillingService.Domain`
- Existing shared dependencies: `Rethink.Services.Common`, `Authentication`, `SummationService.Domain`, `RethinkAutism.Contracts`
- Current deployment asset: `Microservices/BillingService/BillingService.Web/Dockerfile`

## Deployment Target

- AKS-hosted ASP.NET Core service
- Azure SQL
- Azure Cache for Redis
- Azure Service Bus
- Azure Key Vault
- Application Insights / Azure Monitor with OpenTelemetry

## Non-Breaking Constraints

- Preserve current controller routes and response contracts.
- Preserve custom JWT and XApiKey middleware behavior during migration.
- Preserve existing database schema and stored procedure behavior.
- Introduce new architecture through additive adapters and facades first.
- Keep risky behavior changes behind feature toggles.

## Planned Artifacts

- Kubernetes manifests for BillingService in `k8s/billingservice`.
- Helm chart values for initial, mid, and enterprise scale profiles in `helm/billingservice`.
- SQL index and rollback scripts for query optimization recommendations in `scripts/sql/billingservice`.
- OpenTelemetry, health, and autoscaling configuration guidance in the new infrastructure foundation.
- Regression-safety documentation and compatibility test inventory in `docs/billingservice-modernization`.
- k6 load, spike, and soak tests in `tests/LoadTests/billingservice`.

## Validation Gates

- Existing BillingService tests must pass before claiming modernization safety.
- New compatibility tests must fail before implementation and pass afterward.
- Container build must be validated locally when Docker is available before deployment.
- Azure deployment must be validated separately before any live deployment.
