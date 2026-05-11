FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Shared/ClearingHouse.SharedKernel/ClearingHouse.SharedKernel.csproj", "src/Shared/ClearingHouse.SharedKernel/"]
COPY ["src/Shared/ClearingHouse.Contracts/ClearingHouse.Contracts.csproj", "src/Shared/ClearingHouse.Contracts/"]
COPY ["src/Services/BatchOrchestration/BatchOrchestration.Domain/BatchOrchestration/BatchOrchestration.Domain.csproj", "src/Services/BatchOrchestration/BatchOrchestration.Domain/"]
COPY ["src/Services/BatchOrchestration/BatchOrchestration.Application/BatchOrchestration/BatchOrchestration.Application.csproj", "src/Services/BatchOrchestration/BatchOrchestration.Application/"]
COPY ["src/Services/BatchOrchestration/BatchOrchestration.Infrastructure/BatchOrchestration/BatchOrchestration.Infrastructure.csproj", "src/Services/BatchOrchestration/BatchOrchestration.Infrastructure/"]
COPY ["src/Services/BatchOrchestration/BatchOrchestration.Api/BatchOrchestration.Api.csproj", "src/Services/BatchOrchestration/BatchOrchestration.Api/"]
RUN dotnet restore "src/Services/BatchOrchestration/BatchOrchestration.Api/BatchOrchestration.Api.csproj"
COPY . .
WORKDIR "/src/src/Services/BatchOrchestration/BatchOrchestration.Api"
RUN dotnet build "BatchOrchestration.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BatchOrchestration.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "BatchOrchestration.Api.dll"]
