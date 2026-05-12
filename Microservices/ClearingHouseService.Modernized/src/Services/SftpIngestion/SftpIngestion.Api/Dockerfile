FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/SftpIngestion/SftpIngestion.Api/SftpIngestion.Api.csproj", "Services/SftpIngestion/SftpIngestion.Api/"]
COPY ["src/Services/SftpIngestion/SftpIngestion.Infrastructure/SftpIngestion.Infrastructure.csproj", "Services/SftpIngestion/SftpIngestion.Infrastructure/"]
COPY ["src/Services/SftpIngestion/SftpIngestion.Application/SftpIngestion.Application.csproj", "Services/SftpIngestion/SftpIngestion.Application/"]
COPY ["src/Services/SftpIngestion/SftpIngestion.Domain/SftpIngestion.Domain.csproj", "Services/SftpIngestion/SftpIngestion.Domain/"]
COPY ["src/SharedKernel/ClearingHouse.SharedKernel/ClearingHouse.SharedKernel.csproj", "SharedKernel/ClearingHouse.SharedKernel/"]
RUN dotnet restore "Services/SftpIngestion/SftpIngestion.Api/SftpIngestion.Api.csproj"
COPY . .
WORKDIR "/src/Services/SftpIngestion/SftpIngestion.Api"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "SftpIngestion.Api.dll"]
