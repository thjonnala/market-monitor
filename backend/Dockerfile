# Multi-stage build for the Market Monitor .NET 10 Web API.
# Open-source toolchain: official .NET SDK/runtime images, no Azure dependency.
# Build context is the backend/ folder (see render.yaml: dockerContext: ./backend).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (copy only project files for better layer caching).
COPY src/MarketMonitor.Domain/*.csproj         src/MarketMonitor.Domain/
COPY src/MarketMonitor.Application/*.csproj     src/MarketMonitor.Application/
COPY src/MarketMonitor.Infrastructure/*.csproj  src/MarketMonitor.Infrastructure/
COPY src/MarketMonitor.Api/*.csproj             src/MarketMonitor.Api/
RUN dotnet restore src/MarketMonitor.Api/MarketMonitor.Api.csproj

# Copy sources and publish (framework-dependent; runtime image supplies the runtime).
COPY src/ src/
RUN dotnet publish src/MarketMonitor.Api/MarketMonitor.Api.csproj \
    -c Release -o /app -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Render (and most PaaS) inject $PORT; bind to it, fall back to 8080 locally.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet MarketMonitor.Api.dll"]
