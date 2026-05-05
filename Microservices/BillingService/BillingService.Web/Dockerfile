#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

WORKDIR /app
EXPOSE 8080
EXPOSE 443

# Add the necessary dependencies for PuppeteerSharp and Chromium
#RUN apt-get update && apt-get install -y \
   #wget \
   #--no-install-recommends \
   #libnss3 \
   #libx11-xcb1 \
   #libxcomposite1 \
   #libxcursor1 \
   #libxdamage1 \
   #libxi6 \
   #libxtst6 \
   #libglib2.0-0 \
   #libxrandr2 \
   #libxss1 \
   #libasound2 \
   #libatk1.0-0 \
   #libatk-bridge2.0-0 \
   #libpangocairo-1.0-0 \
   #libcairo2 \
   #libgtk-3-0 \
   #fonts-noto \   
   #fonts-liberation \
   #fonts-dejavu-core \
   #libappindicator3-1 \
   #xdg-utils \
   #libgbm1 \
   #ca-certificates \
   #--no-install-recommends \
#&& rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Microservices/BillingService/BillingService.Web/BillingService.Web.csproj", "Microservices/BillingService/BillingService.Web/"]

ARG ACCESS_TOKEN

RUN dotnet nuget add source https://rethinkfirst.pkgs.visualstudio.com/RethinkFirst/_packaging/RethinkBHFeed/nuget/v3/index.json -u any -p $ACCESS_TOKEN --store-password-in-clear-text
RUN dotnet nuget add source https://rethinkfirst.pkgs.visualstudio.com/RethinkFirst/_packaging/Rethink-Common/nuget/v3/index.json -u any -p $ACCESS_TOKEN --store-password-in-clear-text
RUN dotnet restore "Microservices/BillingService/BillingService.Web/BillingService.Web.csproj"
COPY . .

RUN if [ -d "./testresults" ]; then rm -Rf ./testresults; fi
RUN if [ -d "./coveragereport" ]; then rm -Rf ./coveragereport; fi

RUN dotnet tool install --tool-path tools dotnet-reportgenerator-globaltool --version 5.1.12
RUN export PATH="$PATH:/root/.dotnet/tools"

RUN dotnet add "Microservices/Test/BillingServiceTests/BillingService.XUnit.Tests/BillingService.XUnit.Tests/BillingService.XUnit.Tests.csproj" package coverlet.collector --version 3.2.0
RUN dotnet add "Microservices/Test/BillingServiceTests/BillingService.XUnit.Integration.Tests/BillingService.XUnit.Integration.Tests.csproj" package coverlet.collector --version 3.2.0

RUN dotnet build "Microservices/BillingService/BillingService.Web/BillingService.Web.csproj" -c Release -o /app/build

RUN dotnet test -c  Release "Microservices/Test/BillingServiceTests/BillingService.XUnit.Tests/BillingService.XUnit.Tests/BillingService.XUnit.Tests.csproj" --collect:"XPlat Code Coverage" --settings "Microservices/Test/BillingServiceTests/coverlet.runsettings" --logger "trx" --results-directory './testresults'
RUN dotnet test -c  Release "Microservices/Test/BillingServiceTests/BillingService.XUnit.Integration.Tests/BillingService.XUnit.Integration.Tests.csproj" --collect:"XPlat Code Coverage" --settings "Microservices/Test/BillingServiceTests/coverlet.runsettings" --logger "trx" --results-directory './testresults'

RUN ./tools/reportgenerator -reports:"./testresults/**/*.cobertura.xml" -targetdir:"coveragereport" -reporttypes:"Html;Cobertura"

FROM build AS publish
RUN dotnet publish "Microservices/BillingService/BillingService.Web/BillingService.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=publish /src/testresults ./tests
COPY --from=publish /src/coveragereport ./coveragereport

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
ENV DOTNET_STARTUP_HOOKS=""
ENV ASPNETCORE_HOSTINGSTARTUPASSEMBLIES=""

ENTRYPOINT ["dotnet", "BillingService.Web.dll"]