FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Shared/ClearingHouse.SharedKernel/ClearingHouse.SharedKernel.csproj", "src/Shared/ClearingHouse.SharedKernel/"]
COPY ["src/Shared/ClearingHouse.Contracts/ClearingHouse.Contracts.csproj", "src/Shared/ClearingHouse.Contracts/"]
COPY ["src/Services/FileMetadata/FileMetadata.Domain/FileMetadata/FileMetadata.Domain.csproj", "src/Services/FileMetadata/FileMetadata.Domain/"]
COPY ["src/Services/FileMetadata/FileMetadata.Application/FileMetadata/FileMetadata.Application.csproj", "src/Services/FileMetadata/FileMetadata.Application/"]
COPY ["src/Services/FileMetadata/FileMetadata.Infrastructure/FileMetadata/FileMetadata.Infrastructure.csproj", "src/Services/FileMetadata/FileMetadata.Infrastructure/"]
COPY ["src/Services/FileMetadata/FileMetadata.Api/FileMetadata.Api.csproj", "src/Services/FileMetadata/FileMetadata.Api/"]
RUN dotnet restore "src/Services/FileMetadata/FileMetadata.Api/FileMetadata.Api.csproj"
COPY . .
WORKDIR "/src/src/Services/FileMetadata/FileMetadata.Api"
RUN dotnet build "FileMetadata.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FileMetadata.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "FileMetadata.Api.dll"]
