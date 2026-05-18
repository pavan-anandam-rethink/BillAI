# Security and Resiliency Controls

## Authentication and authorization

- Legacy-compatible auth behavior preserved via forwarded headers:
  - `Authorization: Bearer <token>`
  - `XApiKey: <key>`
- Endpoint contracts continue to rely on legacy service authorization for non-migrated logic.

## Rate limiting

- API-level sliding-window limiter configured in `Program.cs`.
- Default policy protects from burst overload and abusive clients.

## Resilience

- Legacy adapter uses `AddStandardResilienceHandler` for HTTP retries/timeouts/circuit behavior.
- SQL uses retry-on-failure.
- Outbox worker supports retry counts and preserves failed events for replay.

## Secrets

- Kubernetes `Secret` placeholders and Bicep secure parameters included.
- Production recommendation: Key Vault references + managed identity.

