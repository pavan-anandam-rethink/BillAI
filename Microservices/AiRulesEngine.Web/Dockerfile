FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Microservices/AiRulesEngine.Web/AiRulesEngine.Web.csproj", "Microservices/AiRulesEngine.Web/"]
RUN dotnet restore "Microservices/AiRulesEngine.Web/AiRulesEngine.Web.csproj"
COPY . .
RUN dotnet build "Microservices/AiRulesEngine.Web/AiRulesEngine.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Microservices/AiRulesEngine.Web/AiRulesEngine.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ARG ENVIRONMENT
ENV ASPNETCORE_ENVIRONMENT $ENVIRONMENT

ENTRYPOINT ["dotnet", "AiRulesEngine.Web.dll"]
