FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Shared/ClearingHouse.SharedKernel/ClearingHouse.SharedKernel.csproj", "src/Shared/ClearingHouse.SharedKernel/"]
COPY ["src/Shared/ClearingHouse.Contracts/ClearingHouse.Contracts.csproj", "src/Shared/ClearingHouse.Contracts/"]
COPY ["src/Services/StediIntegration/StediIntegration.Domain/StediIntegration/StediIntegration.Domain.csproj", "src/Services/StediIntegration/StediIntegration.Domain/"]
COPY ["src/Services/StediIntegration/StediIntegration.Application/StediIntegration/StediIntegration.Application.csproj", "src/Services/StediIntegration/StediIntegration.Application/"]
COPY ["src/Services/StediIntegration/StediIntegration.Infrastructure/StediIntegration/StediIntegration.Infrastructure.csproj", "src/Services/StediIntegration/StediIntegration.Infrastructure/"]
COPY ["src/Services/StediIntegration/StediIntegration.Api/StediIntegration.Api.csproj", "src/Services/StediIntegration/StediIntegration.Api/"]
RUN dotnet restore "src/Services/StediIntegration/StediIntegration.Api/StediIntegration.Api.csproj"
COPY . .
WORKDIR "/src/src/Services/StediIntegration/StediIntegration.Api"
RUN dotnet build "StediIntegration.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "StediIntegration.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "StediIntegration.Api.dll"]
