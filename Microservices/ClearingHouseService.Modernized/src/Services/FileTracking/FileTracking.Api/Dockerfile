FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/FileTracking/FileTracking.Api/FileTracking.Api.csproj", "Services/FileTracking/FileTracking.Api/"]
COPY ["src/Services/FileTracking/FileTracking.Domain/FileTracking.Domain.csproj", "Services/FileTracking/FileTracking.Domain/"]
COPY ["src/SharedKernel/ClearingHouse.SharedKernel/ClearingHouse.SharedKernel.csproj", "SharedKernel/ClearingHouse.SharedKernel/"]
RUN dotnet restore "Services/FileTracking/FileTracking.Api/FileTracking.Api.csproj"
COPY . .
WORKDIR "/src/Services/FileTracking/FileTracking.Api"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
HEALTHCHECK --interval=30s --timeout=10s --retries=3 CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "FileTracking.Api.dll"]
