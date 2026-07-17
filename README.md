# BillAI

An enterprise-grade, cloud-native Healthcare Billing platform built with .NET 8. The repository contains two major components:

| Component | Description |
|---|---|
| **BillingService** | Core billing microservice (ASP.NET Core, Azure SQL, Redis, Service Bus) |
| **RulesEngine** | Business Rules Validation & Workflow Orchestration platform |

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Repository Structure](#repository-structure)
3. [Local Development Setup](#local-development-setup)
   - [BillingService](#billingservice-local)
   - [RulesEngine](#rulesengine-local)
4. [Configuration Reference](#configuration-reference)
5. [Building Docker Images](#building-docker-images)
6. [Deploy to AKS](#deploy-to-aks)
   - [1 – Azure Prerequisites](#1--azure-prerequisites)
   - [2 – Push Image to ACR](#2--push-image-to-acr)
   - [3 – Configure Kubernetes Secrets](#3--configure-kubernetes-secrets)
   - [4 – Apply Raw Manifests](#4--apply-raw-manifests)
   - [5 – Deploy with Helm](#5--deploy-with-helm)
   - [6 – Verify the Deployment](#6--verify-the-deployment)
   - [7 – Autoscaling](#7--autoscaling)
7. [CI/CD Pipeline](#cicd-pipeline)
8. [Running Tests](#running-tests)
9. [Scale Profiles](#scale-profiles)

---

## Prerequisites

| Tool | Minimum Version | Purpose |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0 | Build and run services locally |
| [Docker](https://docs.docker.com/get-docker/) | 24+ | Build container images |
| [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) | 2.60+ | Manage Azure resources |
| [kubectl](https://kubernetes.io/docs/tasks/tools/) | 1.29+ | Interact with AKS cluster |
| [Helm](https://helm.sh/docs/intro/install/) | 3.14+ | Deploy Helm charts |
| Azure Subscription | — | AKS, ACR, Key Vault, SQL, Redis, Service Bus |

---

## Repository Structure

```
BillAI/
├── Microservices/               # Source for all microservices
│   ├── BillingService/          # Core billing service
│   │   ├── BillingService.Web/  # ASP.NET Core host
│   │   ├── BillingService.Domain/
│   │   ├── BillingService.Infrastructure/
│   │   ├── BillingService.Persistence/
│   │   └── BillingService.Workers/
│   ├── Authentication/
│   ├── LoginService.Web/
│   └── ...
├── RulesEngine/                 # Rules & Workflow Orchestration platform
│   ├── src/
│   └── tests/
├── docker/
│   └── BillingService.Web.Dockerfile
├── helm/
│   └── billingservice/          # Helm chart for BillingService
├── k8s/
│   └── billingservice/          # Raw Kubernetes manifests
├── scripts/
│   └── sql/                     # SQL migration and rollback scripts
├── tests/
│   └── LoadTests/               # k6 load/spike/soak tests
└── .github/
    └── workflows/               # GitHub Actions CI pipelines
```

---

## Local Development Setup

### BillingService (local)

1. **Restore and build**

   ```bash
   cd Microservices
   dotnet restore BillingService/BillingService.Web/BillingService.Web.csproj
   dotnet build   BillingService/BillingService.Web/BillingService.Web.csproj
   ```

2. **Configure secrets** — create or update `Microservices/BillingService/BillingService.Web/appsettings.Development.json`:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=BillingDb;Trusted_Connection=True;",
       "Redis": "localhost:6379"
     },
     "Jwt": {
       "Key": "<your-dev-jwt-key>",
       "Issuer": "BillingService",
       "Audience": "BillingServiceClients"
     },
     "KeyVaultUri": ""
   }
   ```

3. **Run**

   ```bash
   dotnet run --project BillingService/BillingService.Web/BillingService.Web.csproj
   ```

### RulesEngine (local)

1. **Restore and run**

   ```bash
   cd RulesEngine
   dotnet run --project src/webapi/BillAI.RulesEngine.WebApi
   ```

2. Open `https://localhost:5001/swagger` for interactive API docs.

3. **Configuration** — `RulesEngine/src/webapi/BillAI.RulesEngine.WebApi/appsettings.json`:

   ```json
   {
     "ConnectionStrings": {
       "AzureStorage": "<optional — leave empty to use local file storage>"
     },
     "Storage": {
       "LocalRootPath": "/data/rules-engine"
     },
     "Jwt": {
       "Key": "<your-jwt-signing-key>"
     }
   }
   ```

   - When `AzureStorage` is empty the service automatically uses `LocalFileStorageProvider` (suitable for dev/test).
   - Set `AzureStorage` to an Azure Blob Storage connection string to enable `AzureBlobStorageProvider` in production.

4. **Multi-tenant** — pass the `X-Tenant-Id` header with every request:

   ```
   X-Tenant-Id: your-tenant-id
   ```

---

## Configuration Reference

| Key | Description | Required |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Azure SQL connection string | ✅ Production |
| `ConnectionStrings:Redis` | Azure Cache for Redis connection string | ✅ Production |
| `ConnectionStrings:AzureStorage` | Azure Blob Storage connection string (RulesEngine) | ✅ Production |
| `Jwt:Key` | JWT signing key | ✅ |
| `KeyVaultUri` | Azure Key Vault URI for secret retrieval | ✅ AKS |
| `BillingService__Modernization__EnableDistributedCacheDecorators` | Feature toggle for Redis-backed cache decorators | ❌ |
| `BillingService__Modernization__EnableOutboxPublisher` | Feature toggle for Service Bus outbox publisher | ❌ |
| `BillingService__Modernization__EnableReadModelQueries` | Feature toggle for CQRS read models | ❌ |
| `BillingService__OpenTelemetry__ServiceName` | OpenTelemetry service name tag | ❌ |

Production secrets (connection strings, keys) should never be stored in source control. Use Azure Key Vault with Workload Identity (see [Deploy to AKS](#deploy-to-aks)).

---

## Building Docker Images

```bash
# From the repository root
docker build \
  -f docker/BillingService.Web.Dockerfile \
  -t myacr.azurecr.io/rtabilling/billingserviceweb:latest \
  .
```

The Dockerfile uses a multi-stage build:
- **Stage `build`** — .NET 8 SDK, restores and publishes in Release configuration.
- **Stage `final`** — ASP.NET 8 runtime image, runs as non-root user on port `8080`.

---

## Deploy to AKS

### 1 – Azure Prerequisites

```bash
# Variables — update these for your environment
RESOURCE_GROUP="rg-billingservice"
LOCATION="eastus"
ACR_NAME="myacr"
AKS_CLUSTER="aks-billing"
KEYVAULT_NAME="billing-keyvault"
MANAGED_IDENTITY_NAME="billingservice-identity"

# Login
az login
az account set --subscription "<your-subscription-id>"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create Azure Container Registry
az acr create --name $ACR_NAME --resource-group $RESOURCE_GROUP --sku Standard

# Create AKS cluster with Workload Identity and OIDC issuer enabled
az aks create \
  --resource-group $RESOURCE_GROUP \
  --name $AKS_CLUSTER \
  --node-count 3 \
  --enable-oidc-issuer \
  --enable-workload-identity \
  --attach-acr $ACR_NAME \
  --generate-ssh-keys

# Get credentials
az aks get-credentials --resource-group $RESOURCE_GROUP --name $AKS_CLUSTER

# Create Azure Key Vault
az keyvault create --name $KEYVAULT_NAME --resource-group $RESOURCE_GROUP --location $LOCATION

# Create Managed Identity for Workload Identity
az identity create --name $MANAGED_IDENTITY_NAME --resource-group $RESOURCE_GROUP

# Grant identity access to Key Vault secrets
IDENTITY_CLIENT_ID=$(az identity show --name $MANAGED_IDENTITY_NAME --resource-group $RESOURCE_GROUP --query clientId -o tsv)
az keyvault set-policy --name $KEYVAULT_NAME --spn $IDENTITY_CLIENT_ID --secret-permissions get list

# Store your secrets in Key Vault
az keyvault secret set --vault-name $KEYVAULT_NAME --name "DefaultConnection" --value "<azure-sql-connection-string>"
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Redis"             --value "<redis-connection-string>"
az keyvault secret set --vault-name $KEYVAULT_NAME --name "JwtKey"            --value "<jwt-signing-key>"
```

### 2 – Push Image to ACR

```bash
# Log in to ACR
az acr login --name $ACR_NAME

# Build and push
docker build \
  -f docker/BillingService.Web.Dockerfile \
  -t $ACR_NAME.azurecr.io/rtabilling/billingserviceweb:latest \
  .

docker push $ACR_NAME.azurecr.io/rtabilling/billingserviceweb:latest
```

### 3 – Configure Kubernetes Secrets

Edit `k8s/billingservice/secret-template.yaml` and replace the placeholder:

```yaml
stringData:
  key-vault-uri: "https://<your-keyvault-name>.vault.azure.net/"
```

Then apply:

```bash
kubectl apply -f k8s/billingservice/secret-template.yaml
```

> **Security note:** For production workloads, use the [Azure Key Vault Provider for Secrets Store CSI Driver](https://learn.microsoft.com/azure/aks/csi-secrets-store-driver) instead of storing URIs in plain Kubernetes secrets.

### 4 – Apply Raw Manifests

```bash
# Apply in order
kubectl apply -f k8s/billingservice/namespace.yaml
kubectl apply -f k8s/billingservice/serviceaccount.yaml
kubectl apply -f k8s/billingservice/configmap.yaml
kubectl apply -f k8s/billingservice/secret-template.yaml
kubectl apply -f k8s/billingservice/deployment.yaml
kubectl apply -f k8s/billingservice/service.yaml
kubectl apply -f k8s/billingservice/hpa.yaml
kubectl apply -f k8s/billingservice/pdb.yaml
```

Or apply the entire directory at once:

```bash
kubectl apply -f k8s/billingservice/
```

### 5 – Deploy with Helm

The Helm chart in `helm/billingservice` is the recommended approach for managing multiple environments.

```bash
# Lint the chart first
helm lint helm/billingservice

# Install (or upgrade) — initial scale profile
helm upgrade --install billingservice helm/billingservice \
  --namespace billing --create-namespace \
  --values helm/billingservice/values.yaml \
  --set image.repository=$ACR_NAME.azurecr.io/rtabilling/billingserviceweb \
  --set image.tag=latest \
  --set keyVaultUri="https://$KEYVAULT_NAME.vault.azure.net/" \
  --set workloadIdentityClientId="$IDENTITY_CLIENT_ID"
```

**Environment-specific values files:**

| Profile | Values file | Min replicas | Max replicas |
|---|---|---|---|
| Initial | `values-initial.yaml` | 3 | 8 |
| Mid | `values-mid.yaml` | 5 | 15 |
| Default | `values.yaml` | 3 | 20 |
| Enterprise | `values-enterprise.yaml` | 8 | 40 |

```bash
# Example: deploy with enterprise profile
helm upgrade --install billingservice helm/billingservice \
  --namespace billing --create-namespace \
  --values helm/billingservice/values.yaml \
  --values helm/billingservice/values-enterprise.yaml \
  --set image.repository=$ACR_NAME.azurecr.io/rtabilling/billingserviceweb \
  --set image.tag=latest \
  --set keyVaultUri="https://$KEYVAULT_NAME.vault.azure.net/" \
  --set workloadIdentityClientId="$IDENTITY_CLIENT_ID"
```

### 6 – Verify the Deployment

```bash
# Check pod status
kubectl get pods -n billing

# Check service
kubectl get svc -n billing

# View logs
kubectl logs -n billing -l app.kubernetes.io/name=billingservice -f

# Check readiness / liveness probes
kubectl describe pod -n billing -l app.kubernetes.io/name=billingservice
```

Health endpoints exposed by the service:

| Path | Purpose |
|---|---|
| `/api/health` | Readiness probe |
| `/health/live` | Liveness probe |

### 7 – Autoscaling

The HPA is applied automatically by the Helm chart or raw manifests. It scales on:

- **CPU**: target 65% utilization (default profile)
- **Memory**: target 75% utilization (default profile)
- **Scale-up**: doubles replicas every 60 s when threshold is breached
- **Scale-down**: reduces by 25% per minute with a 5-minute stabilization window

```bash
kubectl get hpa -n billing
```

---

## CI/CD Pipeline

The GitHub Actions workflow at `.github/workflows/billingservice-modernization-ci.yml` runs on every pull request that touches BillingService, Helm chart, or Kubernetes manifest paths:

| Job | Steps |
|---|---|
| `build-and-test` | Restore → Build infrastructure / workers / persistence → Run architecture tests |
| `helm-lint` | `helm lint helm/billingservice` |

The pipeline does **not** push images or deploy to AKS automatically — wire up the deployment step with your own `az acr build` + `helm upgrade` commands and appropriate Azure credentials as GitHub secrets.

---

## Running Tests

```bash
# BillingService unit and integration tests
cd Microservices
dotnet test Microservices.sln --configuration Release

# RulesEngine tests
cd RulesEngine
dotnet test BillAI.RulesEngine.slnx --configuration Release
```

---

## Scale Profiles

Choose the Helm values file that matches your environment needs:

| Profile | Replicas | CPU request/limit | Memory request/limit | Feature toggles |
|---|---|---|---|---|
| `values-initial.yaml` | 3–8 | 500m / 2 | 1 Gi / 3 Gi | Adapters only |
| `values-mid.yaml` | 5–15 | 750m / 2 | 1.5 Gi / 4 Gi | Adapters only |
| `values.yaml` | 3–20 | 500m / 2 | 1 Gi / 3 Gi | Adapters only |
| `values-enterprise.yaml` | 8–40 | 1 / 3 | 2 Gi / 6 Gi | All features on |

Enable modernization feature flags gradually using Helm `--set` overrides:

```bash
# Enable Redis-backed cache decorators
helm upgrade billingservice helm/billingservice -n billing \
  --reuse-values \
  --set modernization.enableDistributedCacheDecorators=true

# Enable Service Bus outbox publisher
helm upgrade billingservice helm/billingservice -n billing \
  --reuse-values \
  --set modernization.enableOutboxPublisher=true
```
