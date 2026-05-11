FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Shared/ClearingHouse.SharedKernel/ClearingHouse.SharedKernel.csproj", "src/Shared/ClearingHouse.SharedKernel/"]
COPY ["src/Shared/ClearingHouse.Contracts/ClearingHouse.Contracts.csproj", "src/Shared/ClearingHouse.Contracts/"]
COPY ["src/Services/EdiProcessing/EdiProcessing.Domain/EdiProcessing.Domain.csproj", "src/Services/EdiProcessing/EdiProcessing.Domain/"]
COPY ["src/Services/EdiProcessing/EdiProcessing.Application/EdiProcessing.Application.csproj", "src/Services/EdiProcessing/EdiProcessing.Application/"]
COPY ["src/Services/EdiProcessing/EdiProcessing.Infrastructure/EdiProcessing.Infrastructure.csproj", "src/Services/EdiProcessing/EdiProcessing.Infrastructure/"]
COPY ["src/Services/EdiProcessing/EdiProcessing.Worker/EdiProcessing.Worker.csproj", "src/Services/EdiProcessing/EdiProcessing.Worker/"]
RUN dotnet restore "src/Services/EdiProcessing/EdiProcessing.Worker/EdiProcessing.Worker.csproj"
COPY . .
WORKDIR "/src/src/Services/EdiProcessing/EdiProcessing.Worker"
RUN dotnet build "EdiProcessing.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EdiProcessing.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EdiProcessing.Worker.dll"]
