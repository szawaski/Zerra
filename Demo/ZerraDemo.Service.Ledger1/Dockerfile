#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Demo/ZerraDemo.Service.Ledger1/ZerraDemo.Service.Ledger1.csproj", "Demo/ZerraDemo.Service.Ledger1/"]
COPY ["Framework/Zerra.Logger/Zerra.Logger.csproj", "Framework/Zerra.Logger/"]
COPY ["Framework/Zerra/Zerra.csproj", "Framework/Zerra/"]
COPY ["Demo/ZerraDemo.Domain.Ledger1.EventStore/ZerraDemo.Domain.Ledger1.EventStore.csproj", "Demo/ZerraDemo.Domain.Ledger1.EventStore/"]
COPY ["Framework/Zerra.Repository.EventStoreDB/Zerra.Repository.EventStoreDB.csproj", "Framework/Zerra.Repository.EventStoreDB/"]
COPY ["Framework/Zerra.Repository/Zerra.Repository.csproj", "Framework/Zerra.Repository/"]
COPY ["Framework/Zerra.Repository.MsSql/Zerra.Repository.MsSql.csproj", "Framework/Zerra.Repository.MsSql/"]
COPY ["Framework/Zerra.Repository.PostgreSql/Zerra.Repository.PostgreSql.csproj", "Framework/Zerra.Repository.PostgreSql/"]
COPY ["Framework/Zerra.Repository.MySql/Zerra.Repository.MySql.csproj", "Framework/Zerra.Repository.MySql/"]
COPY ["Demo/ZerraDemo.Domain.Ledger1/ZerraDemo.Domain.Ledger1.csproj", "Demo/ZerraDemo.Domain.Ledger1/"]
COPY ["Demo/ZerraDemo.Domain/ZerraDemo.Domain.csproj", "Demo/ZerraDemo.Domain/"]
COPY ["Demo/ZerraDemo.Common/ZerraDemo.Common.csproj", "Demo/ZerraDemo.Common/"]
COPY ["Framework/Zerra.CQRS.AzureEventHub/Zerra.CQRS.AzureEventHub.csproj", "Framework/Zerra.CQRS.AzureEventHub/"]
COPY ["Framework/Zerra.CQRS.AzureServiceBus/Zerra.CQRS.AzureServiceBus.csproj", "Framework/Zerra.CQRS.AzureServiceBus/"]
COPY ["Framework/Zerra.CQRS.Kafka/Zerra.CQRS.Kafka.csproj", "Framework/Zerra.CQRS.Kafka/"]
COPY ["Framework/Zerra.CQRS.RabbitMQ/Zerra.CQRS.RabbitMQ.csproj", "Framework/Zerra.CQRS.RabbitMQ/"]
RUN dotnet restore "Demo/ZerraDemo.Service.Ledger1/ZerraDemo.Service.Ledger1.csproj"
COPY . .
WORKDIR "/src/Demo/ZerraDemo.Service.Ledger1"
RUN dotnet build "ZerraDemo.Service.Ledger1.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ZerraDemo.Service.Ledger1.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ZerraDemo.Service.Ledger1.dll"]