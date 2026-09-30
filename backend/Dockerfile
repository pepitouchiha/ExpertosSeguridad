# Build stage: restores and publishes the API with the .NET 8 SDK.
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /source

# Project files are copied first so the restore layer is cached while only sources change.
COPY global.json ./
COPY ExpertosSeguridad.sln ./
COPY src/ExpertosSeguridad.Domain/ExpertosSeguridad.Domain.csproj src/ExpertosSeguridad.Domain/
COPY src/ExpertosSeguridad.Application/ExpertosSeguridad.Application.csproj src/ExpertosSeguridad.Application/
COPY src/ExpertosSeguridad.Infrastructure/ExpertosSeguridad.Infrastructure.csproj src/ExpertosSeguridad.Infrastructure/
COPY src/ExpertosSeguridad.Api/ExpertosSeguridad.Api.csproj src/ExpertosSeguridad.Api/
RUN dotnet restore src/ExpertosSeguridad.Api/ExpertosSeguridad.Api.csproj

COPY src/ src/
RUN dotnet publish src/ExpertosSeguridad.Api/ExpertosSeguridad.Api.csproj -c Release -o /app --no-restore

# Runtime stage: only the published output and the ASP.NET runtime.
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Runs as a non-root user, so a compromised process has no privileges inside the container.
RUN adduser --disabled-password --no-create-home --uid 10001 appuser
USER appuser

COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ExpertosSeguridad.Api.dll"]
