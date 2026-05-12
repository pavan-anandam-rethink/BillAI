FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["Services/ClearingHouse.SftpIngestion/ClearingHouse.SftpIngestion.Api/ClearingHouse.SftpIngestion.Api.csproj", "Services/ClearingHouse.SftpIngestion/ClearingHouse.SftpIngestion.Api/"]
COPY ["Services/ClearingHouse.SftpIngestion/ClearingHouse.SftpIngestion.Application/ClearingHouse.SftpIngestion.Application.csproj", "Services/ClearingHouse.SftpIngestion/ClearingHouse.SftpIngestion.Application/"]
COPY ["Services/ClearingHouse.SftpIngestion/ClearingHouse.SftpIngestion.Domain/ClearingHouse.SftpIngestion.Domain.csproj", "Services/ClearingHouse.SftpIngestion/ClearingHouse.SftpIngestion.Domain/"]
COPY ["Services/ClearingHouse.SftpIngestion/ClearingHouse.SftpIngestion.Infrastructure/ClearingHouse.SftpIngestion.Infrastructure.csproj", "Services/ClearingHouse.SftpIngestion/ClearingHouse.SftpIngestion.Infrastructure/"]
COPY ["Shared/ClearingHouse.SharedKernel/ClearingHouse.SharedKernel.csproj", "Shared/ClearingHouse.SharedKernel/"]

RUN dotnet restore "Services/ClearingHouse.SftpIngestion/ClearingHouse.SftpIngestion.Api/ClearingHouse.SftpIngestion.Api.csproj"

# Copy all source files
COPY . .

WORKDIR "/src/Services/ClearingHouse.SftpIngestion/ClearingHouse.SftpIngestion.Api"
RUN dotnet build -c Release -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish --no-build /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=publish /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "ClearingHouse.SftpIngestion.Api.dll"]
