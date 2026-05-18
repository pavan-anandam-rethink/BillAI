FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
ARG ACCESS_TOKEN
RUN if [ -n "$ACCESS_TOKEN" ]; then \
      dotnet nuget add source https://rethinkfirst.pkgs.visualstudio.com/RethinkFirst/_packaging/RethinkBHFeed/nuget/v3/index.json -n RethinkBHFeed -u any -p "$ACCESS_TOKEN" --store-password-in-clear-text && \
      dotnet nuget add source https://rethinkfirst.pkgs.visualstudio.com/RethinkFirst/_packaging/Rethink-Common/nuget/v3/index.json -n RethinkCommon -u any -p "$ACCESS_TOKEN" --store-password-in-clear-text; \
    fi
COPY ["Microservices/Microservices.sln", "Microservices/"]
COPY ["Microservices/BillingService/BillingService.Web/BillingService.Web.csproj", "Microservices/BillingService/BillingService.Web/"]
COPY ["Microservices/BillingService/BillingService.Domain/BillingService.Domain.csproj", "Microservices/BillingService/BillingService.Domain/"]
COPY ["Microservices/BillingService/Rethink.Billing.FolderStructure.Core/Billing.FolderStructure.Core.csproj", "Microservices/BillingService/Rethink.Billing.FolderStructure.Core/"]
COPY ["Microservices/Authentication/Authentication.csproj", "Microservices/Authentication/"]
COPY ["Microservices/Rethink.Services.Common/Rethink.Services.Common.csproj", "Microservices/Rethink.Services.Common/"]
COPY ["Microservices/Rethink.Services.Domain/Rethink.Services.Domain.csproj", "Microservices/Rethink.Services.Domain/"]
COPY ["Microservices/SummationService.Domain/SummationService.Domain.csproj", "Microservices/SummationService.Domain/"]
COPY . .
RUN dotnet restore "Microservices/BillingService/BillingService.Web/BillingService.Web.csproj"
RUN dotnet publish "Microservices/BillingService/BillingService.Web/BillingService.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "BillingService.Web.dll"]
