FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Shared/ClearingHouse.SharedKernel/ClearingHouse.SharedKernel.csproj", "src/Shared/ClearingHouse.SharedKernel/"]
COPY ["src/Shared/ClearingHouse.Contracts/ClearingHouse.Contracts.csproj", "src/Shared/ClearingHouse.Contracts/"]
COPY ["src/Services/Reconciliation/Reconciliation.Domain/Reconciliation/Reconciliation.Domain.csproj", "src/Services/Reconciliation/Reconciliation.Domain/"]
COPY ["src/Services/Reconciliation/Reconciliation.Application/Reconciliation/Reconciliation.Application.csproj", "src/Services/Reconciliation/Reconciliation.Application/"]
COPY ["src/Services/Reconciliation/Reconciliation.Infrastructure/Reconciliation/Reconciliation.Infrastructure.csproj", "src/Services/Reconciliation/Reconciliation.Infrastructure/"]
COPY ["src/Services/Reconciliation/Reconciliation.Api/Reconciliation.Api.csproj", "src/Services/Reconciliation/Reconciliation.Api/"]
RUN dotnet restore "src/Services/Reconciliation/Reconciliation.Api/Reconciliation.Api.csproj"
COPY . .
WORKDIR "/src/src/Services/Reconciliation/Reconciliation.Api"
RUN dotnet build "Reconciliation.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Reconciliation.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Reconciliation.Api.dll"]
