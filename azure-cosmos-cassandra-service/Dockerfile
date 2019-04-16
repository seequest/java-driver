FROM mcr.microsoft.com/dotnet/core/aspnet:2.1-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:2.1-stretch AS build
WORKDIR /src
COPY ["azure-cosmosdb-cassandra-service/azure-cosmosdb-cassandra-service.csproj", "azure-cosmosdb-cassandra-service/"]
RUN dotnet restore "azure-cosmosdb-cassandra-service/azure-cosmosdb-cassandra-service.csproj"
COPY . .
WORKDIR "/src/azure-cosmosdb-cassandra-service"
RUN dotnet build "azure-cosmosdb-cassandra-service.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "azure-cosmosdb-cassandra-service.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "azure-cosmosdb-cassandra-service.dll"]