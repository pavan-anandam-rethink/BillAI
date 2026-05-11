FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Shared/ClearingHouse.SharedKernel/ClearingHouse.SharedKernel.csproj", "src/Shared/ClearingHouse.SharedKernel/"]
COPY ["src/Shared/ClearingHouse.Contracts/ClearingHouse.Contracts.csproj", "src/Shared/ClearingHouse.Contracts/"]
COPY ["src/Services/FileTracking/FileTracking.Domain/FileTracking/FileTracking.Domain.csproj", "src/Services/FileTracking/FileTracking.Domain/"]
COPY ["src/Services/FileTracking/FileTracking.Application/FileTracking/FileTracking.Application.csproj", "src/Services/FileTracking/FileTracking.Application/"]
COPY ["src/Services/FileTracking/FileTracking.Infrastructure/FileTracking/FileTracking.Infrastructure.csproj", "src/Services/FileTracking/FileTracking.Infrastructure/"]
COPY ["src/Services/FileTracking/FileTracking.Api/FileTracking.Api.csproj", "src/Services/FileTracking/FileTracking.Api/"]
RUN dotnet restore "src/Services/FileTracking/FileTracking.Api/FileTracking.Api.csproj"
COPY . .
WORKDIR "/src/src/Services/FileTracking/FileTracking.Api"
RUN dotnet build "FileTracking.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FileTracking.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "FileTracking.Api.dll"]
