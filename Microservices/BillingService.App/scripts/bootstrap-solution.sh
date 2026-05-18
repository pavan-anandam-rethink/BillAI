#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

dotnet new sln -n BillingService.App

dotnet sln BillingService.App.sln add src/BillingService.API/BillingService.API.csproj
dotnet sln BillingService.App.sln add src/BillingService.Application/BillingService.Application.csproj
dotnet sln BillingService.App.sln add src/BillingService.Domain/BillingService.Domain.csproj
dotnet sln BillingService.App.sln add src/BillingService.Infrastructure/BillingService.Infrastructure.csproj
dotnet sln BillingService.App.sln add src/BillingService.Persistence/BillingService.Persistence.csproj
dotnet sln BillingService.App.sln add src/BillingService.Contracts/BillingService.Contracts.csproj
dotnet sln BillingService.App.sln add src/BillingService.SharedKernel/BillingService.SharedKernel.csproj
dotnet sln BillingService.App.sln add src/BillingService.Workers/BillingService.Workers.csproj
dotnet sln BillingService.App.sln add src/BillingService.LegacyAdapters/BillingService.LegacyAdapters.csproj

dotnet sln BillingService.App.sln add tests/UnitTests/UnitTests.csproj
dotnet sln BillingService.App.sln add tests/IntegrationTests/IntegrationTests.csproj
dotnet sln BillingService.App.sln add tests/ContractTests/ContractTests.csproj
dotnet sln BillingService.App.sln add tests/RegressionTests/RegressionTests.csproj

echo "BillingService.App.sln generated."

