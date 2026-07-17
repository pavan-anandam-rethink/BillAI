# BillAI Rules Engine & Workflow Orchestration Platform

An enterprise-grade, cloud-native **Business Rules Validation and Workflow Orchestration Platform** for Healthcare Claims — built with .NET 8, Clean Architecture, DDD, CQRS, and MediatR.

## Architecture

```
RulesEngine/
├── src/
│   ├── domain/                  # Domain entities, enums, events, value objects
│   ├── application/             # CQRS commands, queries, handlers, service interfaces
│   ├── infrastructure/          # Storage providers, repositories, tenant service
│   ├── rule-engine/             # High-performance rules evaluation engine
│   ├── workflow-engine/         # State-machine workflow execution engine
│   ├── shared/                  # Common abstractions, models, guards
│   └── webapi/                  # ASP.NET Core Web API
├── tests/
│   ├── BillAI.RulesEngine.Domain.Tests/
│   ├── BillAI.RulesEngine.Application.Tests/
│   └── BillAI.RulesEngine.Engine.Tests/
├── docs/
└── deployment/
```

## Features

### Phase 1 — Solution Structure & Domain ✅
- Clean Architecture with Domain, Application, Infrastructure, and API layers
- DDD aggregate roots: `RuleDefinition`, `WorkflowDefinition`, `WorkflowInstance`, `ClaimValidationRecord`
- Rule versioning, rollback, publish/archive lifecycle
- Domain events for all state transitions
- Multi-tenant support via `X-Tenant-Id` header

### Phase 2 — Rules Runtime Engine ✅
- High-performance, stateless, thread-safe evaluation engine
- Supports: `Equals`, `NotEquals`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `Contains`, `NotContains`, `StartsWith`, `EndsWith`, `In`, `NotIn`, `IsNull`, `IsNotNull`, `Regex`, `Between`
- Composite conditions with `AND`/`OR`/`NOT` logical operators
- Dot-notation field extraction including array indexing (`items[0].name`)
- Decision trace and execution timeline

### Phase 3 — Rule Designer APIs ✅
- Full CRUD for rule definitions
- Publish, archive, rollback, enable/disable
- Paginated listing with status/category/search filters

### Healthcare Claim Validation ✅
```
POST /api/rules/validateClaim
```
Returns: AuditId, IsValid, PassedRules, FailedRules, Warnings, ExecutionTimeMs, DecisionTrace

### Storage Providers ✅
| Provider | Description |
|---|---|
| `LocalFileStorageProvider` | Local file system (dev/test) |
| `AzureBlobStorageProvider` | Azure Blob Storage (production) |

Configured automatically: Azure if `ConnectionStrings:AzureStorage` is set, otherwise local.

## Quick Start

### Prerequisites
- .NET 8 SDK
- (Optional) Azure Storage Account for production

### Run locally

```bash
cd RulesEngine
dotnet run --project src/webapi/BillAI.RulesEngine.WebApi
```

Navigate to `https://localhost:5001/swagger` for the interactive API documentation.

### Run tests

```bash
dotnet test RulesEngine/BillAI.RulesEngine.slnx
```

## Configuration

`appsettings.json` / environment variables:

```json
{
  "ConnectionStrings": {
    "AzureStorage": "<optional Azure Blob Storage connection string>"
  },
  "Storage": {
    "LocalRootPath": "/data/rules-engine"
  },
  "Jwt": {
    "Key": "<your-jwt-signing-key>"
  }
}
```

## API Reference

### Rules

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/rules` | List rules (paginated) |
| `GET` | `/api/rules/{id}` | Get rule by ID |
| `GET` | `/api/rules/{id}/versions` | Get version history |
| `POST` | `/api/rules` | Create draft rule |
| `PUT` | `/api/rules/{id}` | Update rule |
| `POST` | `/api/rules/{id}/publish` | Publish rule |
| `POST` | `/api/rules/{id}/archive` | Archive rule |
| `POST` | `/api/rules/{id}/rollback` | Rollback to version |
| `PATCH` | `/api/rules/{id}/toggle` | Enable/disable rule |
| `DELETE` | `/api/rules/{id}` | Delete rule (Admin only) |

### Healthcare Claim Validation

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/rules/validateClaim` | Validate claim against published rules |

### Workflows

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/workflows` | List workflows |
| `GET` | `/api/workflows/{id}` | Get workflow |
| `POST` | `/api/workflows` | Create workflow |
| `PUT` | `/api/workflows/{id}/definition` | Update definition |
| `POST` | `/api/workflows/{id}/publish` | Publish workflow |
| `POST` | `/api/workflows/{id}/start` | Start execution |
| `GET` | `/api/workflows/instances` | List instances |
| `GET` | `/api/workflows/instances/{id}` | Get instance |
| `POST` | `/api/workflows/instances/{id}/cancel` | Cancel instance |
| `POST` | `/api/workflows/instances/{id}/resume` | Resume instance |

## Rule Expression Format

Rules are stored as JSON using the `RuleCondition` schema:

### Simple condition
```json
{
  "field": "claim.totalAmount",
  "operator": "GreaterThan",
  "value": 0,
  "dataType": "Numeric"
}
```

### Composite AND condition
```json
{
  "logicalOperator": "And",
  "children": [
    { "field": "claim.patientId", "operator": "IsNotNull", "dataType": "String" },
    { "field": "claim.totalAmount", "operator": "GreaterThan", "value": 0, "dataType": "Numeric" }
  ]
}
```

## Multi-Tenant

Pass the `X-Tenant-Id` header with every request:
```
X-Tenant-Id: your-tenant-id
```

Each tenant has independent rules, workflows, version history, and storage.

## Roadmap

- [ ] Phase 4: Complete Workflow Designer APIs
- [ ] Phase 5: Angular 20 Rule & Workflow Designer UI  
- [ ] Phase 6: Monitoring Dashboard
- [ ] Phase 7: Authentication & Azure AD
- [ ] Phase 8: Helm Charts, Dockerfiles, CI/CD Pipelines
