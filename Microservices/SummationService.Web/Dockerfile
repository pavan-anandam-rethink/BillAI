#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

WORKDIR /app
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Microservices/SummationService.Web/SummationService.Web.csproj", "Microservices/SummationService.Web/"]

ARG ACCESS_TOKEN

RUN dotnet nuget add source https://rethinkfirst.pkgs.visualstudio.com/RethinkFirst/_packaging/RethinkBHFeed/nuget/v3/index.json -u any -p $ACCESS_TOKEN --store-password-in-clear-text
RUN dotnet nuget add source https://rethinkfirst.pkgs.visualstudio.com/RethinkFirst/_packaging/Rethink-Common/nuget/v3/index.json -u any -p $ACCESS_TOKEN --store-password-in-clear-text
RUN dotnet restore "Microservices/SummationService.Web/SummationService.Web.csproj"
COPY . .

RUN dotnet build "Microservices/SummationService.Web/SummationService.Web.csproj" -c Release -o /app/build

#COPY ["Microservices/Test/BillingServiceTests/BillingService.Test/BillingService.Test.csproj", "Microservices/Test/BillingServiceTests/BillingService.Test/"]
#COPY ["Microservices/Test/BillingServiceTests/BillingService.XUnit.Integration.Tests/BillingService.XUnit.Integration.Tests.csproj", "Microservices/Test/BillingServiceTests/BillingService.XUnit.Integration.Tests/"]
#COPY ["Microservices/Test/BillingServiceTests/BillingService.XUnit.Tests/BillingService.XUnit.Tests/BillingService.XUnit.Tests.csproj", "Microservices/Test/BillingServiceTests/BillingService.XUnit.Tests/BillingService.XUnit.Tests/"]
#COPY ["Microservices/Test/BillingServiceTests/BillingService.XUnit.Tests.Common/BillingService.XUnit.Tests.Common/BillingService.XUnit.Tests.Common.csproj", "Microservices/Test/BillingServiceTests/BillingService.XUnit.Tests.Common/BillingService.XUnit.Tests.Common/"]

FROM build AS publish
RUN dotnet publish "Microservices/SummationService.Web/SummationService.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# --- Datadog .NET Tracer Installation ---
COPY Microservices/dd-trace/datadog-dotnet-apm-3.32.0.tar.gz /tmp/datadog.tar.gz

RUN apt-get update && apt-get install -y tar \
    && mkdir -p /opt/datadog \
    && tar -xzf /tmp/datadog.tar.gz -C /opt/datadog \
    && rm /tmp/datadog.tar.gz
# ----------------------------------------

# Setup your variables before running.
ARG ENVIRONMENT
ENV ASPNETCORE_ENVIRONMENT $ENVIRONMENT

# Make tracer home known to startup scripts
ENV DD_DOTNET_TRACER_HOME=/opt/datadog
ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER="{846F5F1C-F9AE-4B07-969E-05C26BC060D8}"
ENV CORECLR_PROFILER_PATH="/opt/datadog/Datadog.Trace.ClrProfiler.Native.so"

# Disable HostingStartup (fixes your error log)
#ENV DOTNET_STARTUP_HOOKS=""
#ENV ASPNETCORE_HOSTINGSTARTUPASSEMBLIES=""

ENTRYPOINT ["dotnet", "SummationService.Web.dll"]