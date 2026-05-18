# BillingService Modernization Deployment Plan

Status: Ready for Validation

## Objective

Prepare the existing Billing service for safe, non-breaking, enterprise AKS deployment with Azure SQL, Azure Redis, Azure Service Bus, Key Vault, Application Insights, Azure Monitor, Docker, Kubernetes, Helm, and OpenTelemetry.

## Current Scope

- Mode: Modernize existing application.
- Target app discovered: `Microservices/BillingService/BillingService.Web` with `BillingService.Domain` and shared `Rethink.Services.*` dependencies.
- Primary constraint: preserve all current API contracts, business workflows, calculations, validations, persistence behavior, authentication flows, and integration behavior.

## Planned Azure Architecture

- Compute: Azure Kubernetes Service with zone-aware node pools.
- Database: Azure SQL, preserving current schema compatibility.
- Cache: Azure Cache for Redis with explicit TTL and invalidation strategy.
- Messaging: Azure Service Bus with outbox/idempotency strategy.
- Secrets: Azure Key Vault and managed identity.
- Observability: Application Insights, Azure Monitor, OpenTelemetry, structured logs, metrics, and traces.
- Ingress/API Management: APIM in front of AKS ingress for throttling and policy enforcement.

## Generated Artifacts

- Architecture and risk analysis: `Microservices/BillingService/docs/enterprise-modernization.md`
- Additive SQL optimization script: `Microservices/BillingService/scripts/sql/001_billing_performance_indexes.sql`
- SQL rollback script: `Microservices/BillingService/scripts/sql/001_billing_performance_indexes_rollback.sql`
- Dockerfile: `Microservices/BillingService/docker/BillingService.Dockerfile`
- Kubernetes manifests: `Microservices/BillingService/k8s/billingservice.yaml`
- Helm chart: `Microservices/BillingService/helm/billingservice`
- Load test: `Microservices/BillingService/tests/LoadTests/billing-dashboard.k6.js`
- CI workflow: `.github/workflows/billingservice-ci.yml`

## Analysis Tasks

- Inventory API contracts. Complete.
- Inventory database dependencies. Complete.
- Reverse engineer business workflows and validation rules. Complete at service/map level; detailed calculation golden masters remain next-step work.
- Identify external integrations and authentication flows. Complete.
- Identify concurrency and performance bottlenecks. Complete.
- Define regression protection strategy before code-level modernization. Complete.

## Execution Tasks

- Add compatibility-focused host modernization scaffold. Complete.
- Add regression and contract test foundations. Complete for new middleware/options; broader contract tests remain roadmap items.
- Add cloud-native configuration and deployment artifacts. Complete.
- Add SQL optimization and rollback scripts. Complete.
- Add load-test scripts and operational runbooks. Complete.

## Validation Plan

- Build the existing solution.
- Run relevant unit and web tests where dependencies allow.
- Validate generated Kubernetes and Helm YAML structure.
- Preserve existing controllers and routes unchanged.

## Validation Status

The local cloud image does not currently have the .NET SDK installed (`dotnet: command not found`), so build and test execution must run in CI or in an agent image with .NET 8 SDK and the configured private NuGet feeds. Source changes were intentionally kept additive and controller-compatible.

