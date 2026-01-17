FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/TransactionsService.Api/TransactionsService.Api.csproj", "src/TransactionsService.Api/"]
COPY ["src/TransactionsService.Application/TransactionsService.Application.csproj", "src/TransactionsService.Application/"]
COPY ["src/TransactionsService.Domain/TransactionsService.Domain.csproj", "src/TransactionsService.Domain/"]
COPY ["src/TransactionsService.Infrastructure/TransactionsService.Infrastructure.csproj", "src/TransactionsService.Infrastructure/"]

RUN dotnet restore "src/TransactionsService.Api/TransactionsService.Api.csproj"

COPY . .
WORKDIR "/src/src/TransactionsService.Api"
RUN dotnet build "TransactionsService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TransactionsService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TransactionsService.Api.dll"]
