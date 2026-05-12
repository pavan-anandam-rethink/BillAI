# ClearingHouse Modernized Microservice Platform

## Enterprise Architecture Overview

This platform is a fully modernized, cloud-native, event-driven microservice ecosystem for healthcare EDI transaction processing. It replaces the legacy monolithic ClearingHouse Service with independently deployable, horizontally scalable services running on Azure Kubernetes Service (AKS).

## Architecture Principles

- **Clean Architecture**: Domain → Application → Infrastructure → API layers
- **Domain-Driven Design**: Rich domain models with aggregate roots and domain events
- **Event-Driven**: Azure Service Bus for asynchronous orchestration
- **CQRS + MediatR**: Command/Query separation with pipeline behaviors
- **Plugin-Based**: Each clearinghouse is an independent, deployable plugin
- **Stream Processing**: Zero in-memory file loading for 100GB+ EDI files
- **Resilience**: Polly retry policies, circuit breakers, dead-letter queues
- **Observability**: OpenTelemetry distributed tracing, Azure Monitor integration

## Service Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Azure API Management                          │
└──────────────────────────────────┬──────────────────────────────────┘
                                   │
┌──────────────────────────────────┼──────────────────────────────────┐
│                     AKS Cluster (clearinghouse namespace)            │
│                                  │                                   │
│  ┌───────────────┐  ┌───────────┴───────────┐  ┌────────────────┐  │
│  │ SFTP Ingestion│  │  File Tracking API     │  │ Notification   │  │
│  │    Service    │  │                        │  │   Service      │  │
│  └───────┬───────┘  └───────────────────────┘  └────────────────┘  │
│          │                                                           │
│          ▼                                                           │
│  ┌───────────────┐        Azure Service Bus                         │
│  │ Blob File Mgmt│  ┌──────────────────────────────┐               │
│  │   Service     │  │  Topics: file-ingested,       │               │
│  └───────────────┘  │  edi-processed, batch-done,   │               │
│                      │  reconciliation, alerts       │               │
│          ┌───────────┴──────────────────────────────┘               │
│          │                                                           │
│          ▼                                                           │
│  ┌───────────────┐  ┌───────────────┐  ┌────────────────────────┐  │
│  │    Batch      │  │ EDI Processing│  │  Clearinghouse Plugins  │  │
│  │ Orchestration │──│    Workers    │  │  ┌─────┐ ┌─────────┐   │  │
│  │   Service     │  │   (scalable)  │  │  │Stedi│ │Availity │   │  │
│  └───────────────┘  └───────┬───────┘  │  └─────┘ └─────────┘   │  │
│                              │          │  ┌───────┐ ┌───────┐   │  │
│                              ▼          │  │TriZet │ │Sandata│   │  │
│                     ┌───────────────┐   │  └───────┘ └───────┘   │  │
│                     │Reconciliation │   │  ┌───────┐             │  │
│                     │   Service     │   │  │Waystar│             │  │
│                     └───────────────┘   │  └───────┘             │  │
│                                         └────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
                                   │
┌──────────────────────────────────┼──────────────────────────────────┐
│                          Azure Resources                             │
│  ┌─────────┐  ┌──────────┐  ┌─────────┐  ┌──────────┐            │
│  │Blob Store│  │Azure SQL │  │Key Vault│  │App Insight│            │
│  └─────────┘  └──────────┘  └─────────┘  └──────────┘            │
└─────────────────────────────────────────────────────────────────────┘
```

## Microservices

| Service | Purpose | Scales To |
|---------|---------|-----------|
| **SFTP Ingestion** | Poll clearinghouse SFTP endpoints, download files, upload to Blob | 10 replicas |
| **Blob File Management** | Blob storage abstraction, lifecycle, archive, retention | 8 replicas |
| **File Metadata** | File lifecycle tracking, audit, correlation | 8 replicas |
| **Batch Orchestration** | Batch grouping, parallel orchestration, queue balancing | 8 replicas |
| **EDI Processing Worker** | Stream-based EDI parsing (835/837/999/277/270/271) | 50 replicas |
| **Reconciliation** | Claim matching, payment reconciliation, status tracking | 8 replicas |
| **File Tracking API** | REST API for file lifecycle, search, dashboards | 15 replicas |
| **Notification** | Alerts, DLQ monitoring, operational events | 4 replicas |
| **Clearinghouse Plugins** | Ability, Availity, TriZetto, Sandata, Waystar | Independent |

## EDI File Types Supported

| Type | Description | Processing |
|------|-------------|------------|
| 837 | Healthcare Claim Submission | Outbound |
| 835 | Electronic Remittance Advice (ERA) | Inbound - Payment parsing |
| 999 | Functional Acknowledgment | Inbound - Submission status |
| 277 | Claim Acknowledgment | Inbound - Claim status |
| 270 | Eligibility Inquiry | Outbound |
| 271 | Eligibility Response | Inbound |

## Large File Processing Strategy

Files up to **100GB+** are supported via:
- **Chunked stream processing** (4MB chunks by default)
- **Async/await with backpressure** - never blocks worker threads
- **No in-memory file loading** - all processing is stream-based
- **Parallel chunk processing** - distribute chunks to multiple workers
- **Resumable processing** - checkpoint-based recovery on failure
- **Dead-letter queues** - failed messages auto-retry then DLQ after 5 attempts

## Technology Stack

- .NET 8.0 / ASP.NET Core 8.0
- Azure Kubernetes Service (AKS)
- Azure Service Bus (Topics + Queues)
- Azure Blob Storage
- Azure SQL Database
- Azure Key Vault
- Azure Application Insights
- OpenTelemetry
- MediatR (CQRS)
- FluentValidation
- Polly (Resilience)
- SSH.NET (SFTP)
- Entity Framework Core 8.0
- Helm 3 (Deployment)
- Docker (Containerization)

## Project Structure

```
ClearingHouseService.Modernized/
├── src/
│   ├── SharedKernel/
│   │   └── ClearingHouse.SharedKernel/     # Domain primitives, contracts
│   └── Services/
│       ├── SftpIngestion/                   # SFTP polling & file download
│       │   ├── SftpIngestion.Domain/
│       │   ├── SftpIngestion.Application/
│       │   ├── SftpIngestion.Infrastructure/
│       │   └── SftpIngestion.Api/
│       ├── EdiProcessing/                   # EDI file parsing & processing
│       │   ├── EdiProcessing.Domain/
│       │   ├── EdiProcessing.Application/
│       │   └── EdiProcessing.Infrastructure/
│       ├── BatchOrchestration/              # Batch grouping & orchestration
│       │   └── BatchOrchestration.Domain/
│       ├── BlobManagement/                  # Blob lifecycle management
│       │   └── BlobManagement.Domain/
│       ├── Reconciliation/                  # Claim & payment reconciliation
│       │   └── Reconciliation.Domain/
│       ├── FileTracking/                    # File lifecycle API
│       │   ├── FileTracking.Domain/
│       │   └── FileTracking.Api/
│       ├── Notification/                    # Alerts & operational events
│       │   └── Notification.Domain/
│       └── ClearinghousePlugins/            # Plugin-based clearinghouses
│           ├── ClearingHouse.Plugins.Contracts/
│           ├── ClearingHouse.Plugins.Runtime/
│           ├── ClearingHouse.Plugins.Ability/
│           ├── ClearingHouse.Plugins.Availity/
│           ├── ClearingHouse.Plugins.Stedi/
│           ├── ClearingHouse.Plugins.TriZetto/
│           ├── ClearingHouse.Plugins.Sandata/
│           └── ClearingHouse.Plugins.Waystar/
├── database/
│   └── migrations/                          # SQL schema with indexes & partitioning
├── deploy/
│   ├── helm/                                # Helm charts for AKS
│   │   └── clearinghouse-platform/
│   └── pipelines/                           # CI/CD Azure DevOps pipelines
└── README.md
```

## Deployment

### Prerequisites
- Azure Subscription with AKS cluster
- Azure Container Registry
- Azure Service Bus namespace
- Azure SQL Database
- Azure Key Vault
- Azure Application Insights

### Deploy with Helm
```bash
helm upgrade --install clearinghouse-platform ./deploy/helm/clearinghouse-platform \
  --namespace clearinghouse \
  --create-namespace \
  --set global.environment=production \
  --set keyVault.vaultUri=https://kv-clearinghouse.vault.azure.net/ \
  --set database.connectionString="<connection-string>" \
  --set serviceBus.connectionString="<connection-string>"
```

## Security

- **Managed Identity**: Azure Workload Identity for all service-to-Azure authentication
- **Key Vault**: All secrets, connection strings, and SFTP credentials
- **Network Policies**: Pod-to-pod traffic restricted to namespace
- **RBAC**: Kubernetes RBAC + Azure AD integration
- **HIPAA**: Design follows HIPAA-aware principles for PHI handling
- **Zero Trust**: No implicit trust between services

## Observability

- **Distributed Tracing**: Full end-to-end trace from SFTP → Blob → Queue → Worker → Reconciliation → Archive
- **Correlation IDs**: Propagated across all service boundaries
- **Structured Logging**: JSON-formatted logs with correlation context
- **Metrics**: Custom counters, histograms, gauges for all operations
- **Dashboards**: Azure Monitor workbooks for operational visibility
- **Alerting**: Severity-based alerts for DLQ depth, failure rates, latency

## Resilience

- **Retry Policies**: Exponential backoff with jitter (Polly)
- **Dead-Letter Queues**: Auto-DLQ after 5 failed processing attempts
- **Circuit Breakers**: Per-clearinghouse circuit breakers
- **Connection Pooling**: SFTP connection pools per clearinghouse
- **Idempotent Operations**: Content-hash deduplication
- **Backpressure**: Queue-based throttling with prefetch limits
