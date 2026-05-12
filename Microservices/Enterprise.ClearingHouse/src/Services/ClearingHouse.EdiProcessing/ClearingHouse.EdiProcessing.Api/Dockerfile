FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["Services/ClearingHouse.EdiProcessing/ClearingHouse.EdiProcessing.Api/ClearingHouse.EdiProcessing.Api.csproj", "Services/ClearingHouse.EdiProcessing/ClearingHouse.EdiProcessing.Api/"]
COPY ["Services/ClearingHouse.EdiProcessing/ClearingHouse.EdiProcessing.Application/ClearingHouse.EdiProcessing.Application.csproj", "Services/ClearingHouse.EdiProcessing/ClearingHouse.EdiProcessing.Application/"]
COPY ["Services/ClearingHouse.EdiProcessing/ClearingHouse.EdiProcessing.Domain/ClearingHouse.EdiProcessing.Domain.csproj", "Services/ClearingHouse.EdiProcessing/ClearingHouse.EdiProcessing.Domain/"]
COPY ["Services/ClearingHouse.EdiProcessing/ClearingHouse.EdiProcessing.Infrastructure/ClearingHouse.EdiProcessing.Infrastructure.csproj", "Services/ClearingHouse.EdiProcessing/ClearingHouse.EdiProcessing.Infrastructure/"]
COPY ["Shared/ClearingHouse.SharedKernel/ClearingHouse.SharedKernel.csproj", "Shared/ClearingHouse.SharedKernel/"]

RUN dotnet restore "Services/ClearingHouse.EdiProcessing/ClearingHouse.EdiProcessing.Api/ClearingHouse.EdiProcessing.Api.csproj"

# Copy all source files
COPY . .

WORKDIR "/src/Services/ClearingHouse.EdiProcessing/ClearingHouse.EdiProcessing.Api"
RUN dotnet build "ClearingHouse.EdiProcessing.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ClearingHouse.EdiProcessing.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=publish /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "ClearingHouse.EdiProcessing.Api.dll"]
