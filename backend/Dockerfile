# syntax=docker/dockerfile:1
#
# Same location and runtime conventions as the original scaffold Dockerfile
# (non-root uid 5678, port 8080, /api/health check, `dotnet VetRIS.Api.dll`
# entrypoint) — only the build stage changes, because there are now four
# projects with real ProjectReferences between them instead of one flat
# project. Build context is `backend/` (unchanged — docker-compose.yml and
# backend-deploy.yml both already build from here).

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first (nothing else) so Docker's layer cache holds
# across source-only changes.
COPY VetRIS.Domain/VetRIS.Domain.csproj VetRIS.Domain/
COPY VetRIS.Application/VetRIS.Application.csproj VetRIS.Application/
COPY VetRIS.Infrastructure/VetRIS.Infrastructure.csproj VetRIS.Infrastructure/
COPY VetRIS.Api/VetRIS.Api.csproj VetRIS.Api/
RUN dotnet restore VetRIS.Api/VetRIS.Api.csproj

COPY VetRIS.Domain/ VetRIS.Domain/
COPY VetRIS.Application/ VetRIS.Application/
COPY VetRIS.Infrastructure/ VetRIS.Infrastructure/
COPY VetRIS.Api/ VetRIS.Api/

WORKDIR /src/VetRIS.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN useradd --uid 5678 --user-group appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=15s \
  CMD wget -qO- http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "VetRIS.Api.dll"]
