# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY VetRIS.Api/VetRIS.Api.csproj VetRIS.Api/
RUN dotnet restore VetRIS.Api/VetRIS.Api.csproj

COPY VetRIS.Api/ VetRIS.Api/
WORKDIR /src/VetRIS.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Run as non-root
RUN useradd --uid 5678 --user-group appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=15s \
  CMD wget -qO- http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "VetRIS.Api.dll"]
