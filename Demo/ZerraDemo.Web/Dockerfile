#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Demo/ZerraDemo.Web/ZerraDemo.Web.csproj", "Demo/ZerraDemo.Web/"]
COPY ["Framework/Zerra.Web/Zerra.Web.csproj", "Framework/Zerra.Web/"]
COPY ["Framework/Zerra/Zerra.csproj", "Framework/Zerra/"]
COPY ["Demo/ZerraDemo.Common/ZerraDemo.Common.csproj", "Demo/ZerraDemo.Common/"]
COPY ["Framework/Zerra.CQRS.AzureEventHub/Zerra.CQRS.AzureEventHub.csproj", "Framework/Zerra.CQRS.AzureEventHub/"]
COPY ["Framework/Zerra.CQRS.AzureServiceBus/Zerra.CQRS.AzureServiceBus.csproj", "Framework/Zerra.CQRS.AzureServiceBus/"]
COPY ["Framework/Zerra.CQRS.Kafka/Zerra.CQRS.Kafka.csproj", "Framework/Zerra.CQRS.Kafka/"]
COPY ["Framework/Zerra.CQRS.RabbitMQ/Zerra.CQRS.RabbitMQ.csproj", "Framework/Zerra.CQRS.RabbitMQ/"]
COPY ["Framework/Zerra.Logger/Zerra.Logger.csproj", "Framework/Zerra.Logger/"]
COPY ["Demo/ZerraDemo.Domain/ZerraDemo.Domain.csproj", "Demo/ZerraDemo.Domain/"]
COPY ["Framework/Zerra.Repository/Zerra.Repository.csproj", "Framework/Zerra.Repository/"]
RUN dotnet restore "Demo/ZerraDemo.Web/ZerraDemo.Web.csproj"
COPY . .
WORKDIR "/src/Demo/ZerraDemo.Web"
RUN dotnet build "ZerraDemo.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ZerraDemo.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ZerraDemo.Web.dll"]