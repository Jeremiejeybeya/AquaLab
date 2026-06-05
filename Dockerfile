# ── Build ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY AquaLab/AquaLab.csproj ./AquaLab/
RUN dotnet restore AquaLab/AquaLab.csproj

COPY AquaLab/ ./AquaLab/
RUN dotnet publish AquaLab/AquaLab.csproj -c Release -o /app/publish

# ── Runtime ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Dossier pour la base SQLite
RUN mkdir -p /data

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "AquaLab.dll"]
