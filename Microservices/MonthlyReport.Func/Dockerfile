#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG ENVIRONMENT
WORKDIR /src
COPY ["Microservices/MonthlyReport.Func/MonthlyReport.Func.csproj", "Microservices/MonthlyReport.Func/"]
RUN dotnet restore "./Microservices/MonthlyReport.Func/MonthlyReport.Func.csproj"
COPY . .
COPY "Microservices/MonthlyReport.Func/local.settings.${ENVIRONMENT}.json" "Microservices/MonthlyReport.Func/local.settings.json"
WORKDIR "/src/Microservices/MonthlyReport.Func"
RUN dotnet build "./MonthlyReport.Func.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MonthlyReport.Func.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .

# Setup your variables before running.
ARG ENVIRONMENT
ENV ASPNETCORE_ENVIRONMENT=$ENVIRONMENT \
    AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true