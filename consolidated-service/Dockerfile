FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/ConsolidatedService.Api/ConsolidatedService.Api.csproj", "src/ConsolidatedService.Api/"]
COPY ["src/ConsolidatedService.Application/ConsolidatedService.Application.csproj", "src/ConsolidatedService.Application/"]
COPY ["src/ConsolidatedService.Domain/ConsolidatedService.Domain.csproj", "src/ConsolidatedService.Domain/"]
COPY ["src/ConsolidatedService.Infrastructure/ConsolidatedService.Infrastructure.csproj", "src/ConsolidatedService.Infrastructure/"]

RUN dotnet restore "src/ConsolidatedService.Api/ConsolidatedService.Api.csproj"

COPY . .
WORKDIR "/src/src/ConsolidatedService.Api"
RUN dotnet build "ConsolidatedService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConsolidatedService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsolidatedService.Api.dll"]
