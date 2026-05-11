FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Shared/ClearingHouse.SharedKernel/ClearingHouse.SharedKernel.csproj", "src/Shared/ClearingHouse.SharedKernel/"]
COPY ["src/Shared/ClearingHouse.Contracts/ClearingHouse.Contracts.csproj", "src/Shared/ClearingHouse.Contracts/"]
COPY ["src/Services/BlobManagement/BlobManagement.Domain/BlobManagement/BlobManagement.Domain.csproj", "src/Services/BlobManagement/BlobManagement.Domain/"]
COPY ["src/Services/BlobManagement/BlobManagement.Application/BlobManagement/BlobManagement.Application.csproj", "src/Services/BlobManagement/BlobManagement.Application/"]
COPY ["src/Services/BlobManagement/BlobManagement.Infrastructure/BlobManagement/BlobManagement.Infrastructure.csproj", "src/Services/BlobManagement/BlobManagement.Infrastructure/"]
COPY ["src/Services/BlobManagement/BlobManagement.Api/BlobManagement.Api.csproj", "src/Services/BlobManagement/BlobManagement.Api/"]
RUN dotnet restore "src/Services/BlobManagement/BlobManagement.Api/BlobManagement.Api.csproj"
COPY . .
WORKDIR "/src/src/Services/BlobManagement/BlobManagement.Api"
RUN dotnet build "BlobManagement.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BlobManagement.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "BlobManagement.Api.dll"]
