FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Shared/ClearingHouse.SharedKernel/ClearingHouse.SharedKernel.csproj", "src/Shared/ClearingHouse.SharedKernel/"]
COPY ["src/Shared/ClearingHouse.Contracts/ClearingHouse.Contracts.csproj", "src/Shared/ClearingHouse.Contracts/"]
COPY ["src/Services/SftpIngestion/SftpIngestion.Domain/SftpIngestion/SftpIngestion.Domain.csproj", "src/Services/SftpIngestion/SftpIngestion.Domain/"]
COPY ["src/Services/SftpIngestion/SftpIngestion.Application/SftpIngestion/SftpIngestion.Application.csproj", "src/Services/SftpIngestion/SftpIngestion.Application/"]
COPY ["src/Services/SftpIngestion/SftpIngestion.Infrastructure/SftpIngestion/SftpIngestion.Infrastructure.csproj", "src/Services/SftpIngestion/SftpIngestion.Infrastructure/"]
COPY ["src/Services/SftpIngestion/SftpIngestion.Api/SftpIngestion.Api.csproj", "src/Services/SftpIngestion/SftpIngestion.Api/"]
RUN dotnet restore "src/Services/SftpIngestion/SftpIngestion.Api/SftpIngestion.Api.csproj"
COPY . .
WORKDIR "/src/src/Services/SftpIngestion/SftpIngestion.Api"
RUN dotnet build "SftpIngestion.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SftpIngestion.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "SftpIngestion.Api.dll"]
