FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Shared/ClearingHouse.SharedKernel/ClearingHouse.SharedKernel.csproj", "src/Shared/ClearingHouse.SharedKernel/"]
COPY ["src/Shared/ClearingHouse.Contracts/ClearingHouse.Contracts.csproj", "src/Shared/ClearingHouse.Contracts/"]
COPY ["src/Services/Notification/Notification.Domain/Notification/Notification.Domain.csproj", "src/Services/Notification/Notification.Domain/"]
COPY ["src/Services/Notification/Notification.Application/Notification/Notification.Application.csproj", "src/Services/Notification/Notification.Application/"]
COPY ["src/Services/Notification/Notification.Infrastructure/Notification/Notification.Infrastructure.csproj", "src/Services/Notification/Notification.Infrastructure/"]
COPY ["src/Services/Notification/Notification.Api/Notification.Api.csproj", "src/Services/Notification/Notification.Api/"]
RUN dotnet restore "src/Services/Notification/Notification.Api/Notification.Api.csproj"
COPY . .
WORKDIR "/src/src/Services/Notification/Notification.Api"
RUN dotnet build "Notification.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Notification.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Notification.Api.dll"]
