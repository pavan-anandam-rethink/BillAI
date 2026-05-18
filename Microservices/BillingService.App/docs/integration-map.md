# External Integration Map

## Platform integrations

- Azure SQL (billing + reporting)
- Azure Redis Cache
- Azure Service Bus (topics/queues)
- Azure Blob Storage
- Application Insights / OpenTelemetry collector

## Service integrations (HTTP)

- Accounts API
- Curriculum API
- Demographics API
- Health Plans API
- Health Insurance API
- Medical Records API
- Practice Operations API
- Appointment API

## Real-time/notification integration

- Pusher auth + notification paths

## Compatibility adapter pattern

- `BillingService.LegacyAdapters` forwards requests to legacy BillingService APIs.
- `YARP` catch-all route preserves non-migrated endpoint behavior.
- Per-endpoint CQRS migration is gated behind feature toggles.

