# BillingService Phase 1 Current System Analysis

## Executive Summary

The requested `BillingService.App` is implemented in this repository as `Microservices/BillingService/BillingService.Web`, backed by `BillingService.Domain`, `Rethink.Services.Common`, `Authentication`, `Rethink.Services.Domain`, and Azure Functions in the wider `Microservices.sln`. The safe modernization strategy is incremental: preserve every existing controller route and domain service behavior, add Clean Architecture seams around the legacy implementation, and migrate workflows behind feature flags after regression coverage exists.

## Dependency Graph

```text
BillingService.Web
  -> Authentication
  -> BillingService.Domain
      -> Rethink.Services.Common
      -> Rethink.Services.Domain
      -> SummationService.Domain
      -> EdiFabric.Templates
      -> Billing.FolderStructure.Core
  -> Billing.FolderStructure.Core

New additive modernization slice
  -> BillingService.Application
      -> BillingService.Contracts
      -> BillingService.SharedKernel
  -> BillingService.LegacyAdapters
      -> BillingService.Domain
  -> BillingService.Infrastructure
      -> BillingService.Application
  -> BillingService.Persistence
      -> BillingService.Application
      -> Rethink.Services.Common
  -> BillingService.Workers
      -> BillingService.Application
```

## API Contract Inventory

The current API is MVC-controller based. Most controllers use `[Route("[controller]/[action]")]`, so existing clients depend on action names. The modernization must keep these contracts stable until a versioned API is introduced.

Compatibility-critical controllers:

- `ClaimController`
- `ClaimPostingController`
- `ClaimUpdateController`
- `ClaimNoteController`
- `ClaimAttachmentController`
- `ClearingHouseController`
- `EdiFileController`
- `PaymentPostingController`
- `BulkPaymentPostingController`
- `PaymentNoteController`
- `PaymentAttachmentController`
- `ChargePaymentController`
- `ServiceLineAdjustmentController`
- `WriteOffController`
- `ChargeEntryController`
- `AppointmentController`
- `AppointmentReportsController`
- `PatientInvoiceController`
- `NotifyClaimStatusController`
- `BillingSettingsController`
- `FunderSettingController`
- `ClientChargeHistoryController`
- `PusherAuthController` at `pusher/auth`
- Health check endpoint `/api/health`

## Authentication Flow

- Requests with `XApiKey` use `ApiKeyMiddleware`.
- Requests without `XApiKey` use `JwtMiddleware` then `BillingMasterDataRequestMiddleware`.
- JWT secrets are resolved from Key Vault.
- `UseAuthentication` and `UseAuthorization` are present, but the effective behavior is custom middleware, not ASP.NET Core JWT bearer policies.

Modernization rule: keep these flows unchanged while introducing standard JWT/RBAC in a versioned or feature-gated path.

## Database Dependency Map

- `BillingDbContext`: claims, payments, service lines, write-offs, patient invoices, EDI, billing settings, scheduling.
- `ReportingDbContext`: reporting projections and reporting queries.
- Repository pattern: `IRepository<,>` from `Rethink.Services.Common`.
- Connection strings are assembled from config and Key Vault secrets in `IoCContainer`.

Risk: any schema or query rewrite can break reports and billing calculations. SQL changes must be additive first: nonclustered indexes, covering indexes, read-model tables, and rollback scripts.

## Business Workflow Map

1. Claims
   - create, update, validate, approve, flag, void, rebill, submit, sync, history, attachments, notes.
2. Payments
   - payment creation, manual posting, bulk posting, unallocated payments, service-line adjustments, notes, attachments.
3. Patient invoicing
   - invoice creation, charge aggregation, PDF generation, payment grouping.
4. Appointment billing
   - appointment-to-claim workflow, authorization validation, report generation.
5. Clearinghouse / EDI
   - 837/835/999/277/270/271 file flows, blob storage layout, Stedi and Availity-oriented processing.
6. Settings and lookup data
   - billing settings, funder settings, providers, locations, claim codes, feature flags.
7. Realtime / notifications
   - Pusher auth and notifications.

## Validation and Calculation Hotspots

- `ClaimValidationService`: high-risk validation engine; must be protected by focused regression tests before extraction.
- `PaymentClaimService`: payment, adjustment, deductible, copay, coinsurance, write-off calculations.
- `PatientInvoiceService`: invoice aggregation and PDF payload shape.
- `AppointmentService` and `AppointmentReportService`: billing workflow and dashboard/report data.

## External Integrations

- Azure Key Vault
- Azure SQL
- Azure Blob Storage
- Azure Service Bus
- Azure Cache for Redis
- Application Insights
- Accounts, Curriculum, Demographics, Health Plans, Health Insurance, Medical Records, Practice Operations, Appointment APIs
- Pusher
- Rethink Print and Rethink Mail
- Stedi / clearinghouse APIs
- EdiFabric

## Concurrency and Performance Risks

- Synchronous secret calls (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`) during startup and in services.
- Large controllers and service classes increase change risk.
- EF `Include` chains and materialization-heavy workflows can cause N+1 queries and high memory pressure.
- Cache behavior is mixed between memory and Redis without a documented TTL/invalidation policy.
- Legacy `WindowsAzure.Storage` and `Microsoft.Azure.ServiceBus` packages should be migrated behind adapters.
- PDF generation requires container-safe Chromium/Puppeteer dependencies.

## Regression Protection Strategy

1. Preserve all existing routes and response payloads.
2. Keep current domain services as the behavioral source of truth.
3. Add compatibility facades before extracting use cases.
4. Add contract tests that snapshot route metadata and critical payload shapes.
5. Add regression tests around claim validation, payment posting, invoice aggregation, and EDI payload generation before refactoring each workflow.
6. Add database regression tests comparing query results before/after optimized queries.
7. Keep cache, outbox, and new read-model paths behind feature flags.
8. Roll out per workflow with canary deployments and feature toggles.

## Risk Assessment

| Risk | Impact | Mitigation |
| --- | --- | --- |
| API route drift | Frontend breakage | Route inventory and contract tests |
| Calculation drift | Billing errors | Golden-master regression tests |
| Cache stale data | Incorrect dashboards/invoices | Short TTLs, explicit invalidation, feature flags |
| Async event inconsistency | Lost notifications/reporting | Outbox pattern, idempotency keys, DLQ |
| Query/index regression | DB contention | Additive indexes with rollback scripts |
| Auth behavior change | Login/API failures | Keep custom middleware until versioned migration |
| Container PDF failure | Invoice/report failures | Include Chromium dependencies and PDF smoke tests |
