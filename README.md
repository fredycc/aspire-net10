# Clean Architecture Template

What's included in the template?

- SharedKernel project with common Domain-Driven Design abstractions.
- Domain layer with sample entities.
- Application layer with abstractions for:
  - CQRS
  - Example use cases
  - Cross-cutting concerns (logging, validation)
- Infrastructure layer with:
  - Authentication
  - Permission authorization
  - EF Core, PostgreSQL
  - Serilog
- Grafana Loki for production-grade log persistence (open-source alternative to Seq)
  - Loki receives OTLP logs directly — no adapter needed
  - Grafana UI available at http://localhost:3000 in production mode
  - In development, only Aspire Dashboard runs (zero overhead)
- Testing projects
  - Architecture testing

## Prerequisites

| Requirement | Why | Install |
|-------------|-----|---------|
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Runtime and build tooling | [Download](https://dotnet.microsoft.com/download) |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | **Required** — PostgreSQL runs as container; Loki+Grafana for production telemetry | [Download](https://www.docker.com/products/docker-desktop/) · Enable WSL 2 backend during install |
| [Aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling) | Aspire orchestration SDK | `dotnet workload install aspire` |

> **Docker is mandatory**, not optional. The AppHost starts PostgreSQL as a container — without Docker it never launches and `web-api` hangs waiting for it. For production telemetry, Loki and Grafana also run as containers.

## Getting Started

Follow these steps **in order**. Skipping steps is the #1 cause of "it doesn't work".

### 1. Clone the repository

```bash
git clone <repo-url>
cd aspire-net10
```

### 2. Start Docker Desktop

Open Docker Desktop and wait until it shows **"Engine running"** in the bottom-left corner. Don't proceed until it's ready — Aspire will fail silently if Docker isn't up.

### 3. Trust the HTTPS development certificate

The Aspire dashboard uses HTTPS with gRPC internally. If the dev certificate isn't trusted, you'll get `UntrustedRoot` SSL errors and the dashboard will break.

```bash
dotnet dev-certs https --trust
```

Accept the prompt to install the certificate in your system's trust store. You only need to do this once per machine.

### 4. Restore dependencies

```bash
dotnet restore CleanArchitecture.sln
```

### 5. Generate JWT secret

The project uses `dotnet user-jwts` for local development JWT secrets:

```bash
dotnet user-jwts create --project src/Web.Api
```

This stores the secret in your local user-secrets store — never committed to source control.

### 6. Start the application

The AppHost uses **launch profiles** to switch between modes. Both modes set `DOTNET_ENVIRONMENT` and `ASPNETCORE_ENVIRONMENT` automatically — no manual env vars needed.

#### Development mode (default)

```bash
cd src/Aspire.AppHost
dotnet run
```

Or explicitly:

```bash
dotnet run --launch-profile dev_https --project src/Aspire.AppHost
```

Starts **PostgreSQL + Web.Api + Aspire Dashboard** only. No Loki, no Grafana — zero overhead. Logs go to the Aspire Dashboard.

| Service | URL |
|---------|-----|
| Web.Api | http://localhost:8080 |
| Aspire Dashboard | https://localhost:17168 (URL shown in console) |

> The dashboard URL changes on each run — use the one printed in the console.

#### Production mode

Starts **PostgreSQL + Web.Api + Loki + Grafana**. The Web.Api sends OTLP logs to Loki automatically. The Aspire Dashboard also sends telemetry to Loki.

```bash
dotnet run --launch-profile prod_https --project src/Aspire.AppHost
```

| Service | URL | Credentials |
|---------|-----|-------------|
| Web.Api | http://localhost:8080 | — |
| Aspire Dashboard | https://localhost:17168 | — |
| Grafana | http://localhost:3000 | admin / admin |
| Loki | http://localhost:3100 | — |

In Grafana → **Explore** → select **Loki** datasource → query your logs.

#### Available launch profiles

| Profile | Environment | Transport | Stack | Use case |
|---------|-------------|-----------|-------|----------|
| `dev_https` | Development | HTTPS | PostgreSQL + Web.Api + Aspire Dashboard | Default for daily development |
| `dev_http` | Development | HTTP | PostgreSQL + Web.Api + Aspire Dashboard | When dev certificate isn't trusted or HTTPS causes issues |
| `prod_https` | Production | HTTPS | PostgreSQL + Web.Api + Loki + Grafana + Aspire Dashboard | Full production-like environment with observability |
| `prod_http` | Production | HTTP | Same as `prod_https` | When HTTPS isn't needed (local testing, CI) |

##### `dev_https` (Development)

```bash
dotnet run --launch-profile dev_https --project src/Aspire.AppHost
```

- **Environment**: `ASPNETCORE_ENVIRONMENT=Development`, `DOTNET_ENVIRONMENT=Development`
- **Dashboard OTLP**: `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL=https://localhost:21034` (gRPC)
- **Resource service**: `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL=https://localhost:22017`
- **Web.Api**: Uses `appsettings.Development.json`, developer exception pages, Swagger UI
- **No Loki/Grafana** — logs go to Aspire Dashboard only

This is the default profile. Use it unless you have a reason not to.

##### `dev_http` (Development)

```bash
dotnet run --launch-profile dev_http --project src/Aspire.AppHost
```

- **Environment**: same as `dev_https`
- **Dashboard OTLP**: `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL=http://localhost:19291` (HTTP, no TLS)
- **Resource service**: `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL=http://localhost:20183`
- **No HTTPS** — dashboard and resource service use plain HTTP

Use when:
- You haven't trusted the dev certificate (`dotnet dev-certs https --trust`)
- Working in an environment where HTTPS causes certificate errors
- Debugging TLS-related issues

##### `prod_https` (Production)

```bash
dotnet run --launch-profile prod_https --project src/Aspire.AppHost
```

- **Environment**: `ASPNETCORE_ENVIRONMENT=Production`, `DOTNET_ENVIRONMENT=Production`
- **Dashboard OTLP**: `ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL=http://localhost:3100` → Loki
- **Resource service**: `ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL=https://localhost:22017`
- **Transport flag**: `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true` (required because Loki OTLP is HTTP)
- **Web.Api**: Uses `appsettings.json` (no dev overrides), no Swagger UI, no developer exception pages
- **Loki** receives OTLP logs from both Web.Api and the Aspire Dashboard
- **Grafana** at `http://localhost:3000` (admin/admin) — query Loki datasource for logs

Use when testing the production pipeline locally: Loki ingestion, Grafana dashboards, OTLP export.

##### `prod_http` (Production)

```bash
dotnet run --launch-profile prod_http --project src/Aspire.AppHost
```

- Same as `prod_https` but without HTTPS on the application URL
- **Transport flag**: `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true`
- Useful for CI pipelines or environments where HTTPS isn't needed

##### Key differences at a glance

| Feature | `dev_https` | `dev_http` | `prod_https` | `prod_http` |
|---------|---------|--------|--------------|------------------|
| Environment | Development | Development | Production | Production |
| HTTPS | ✅ | ❌ | ✅ | ❌ |
| Loki + Grafana | ❌ | ❌ | ✅ | ✅ |
| Aspire Dashboard | ✅ | ✅ | ✅ (→ Loki) | ✅ (→ Loki) |
| Swagger UI | ✅ | ✅ | ❌ | ❌ |
| Developer exceptions | ✅ | ✅ | ❌ | ❌ |
| `appsettings` file | `.Development.json` | `.Development.json` | `.json` | `.json` |

#### Switching modes

1. **Stop** the running AppHost (`Ctrl+C`)
2. **Restart** with a different profile:

```bash
# Development
dotnet run --launch-profile dev_https --project src/Aspire.AppHost

# Production
dotnet run --launch-profile prod_https --project src/Aspire.AppHost
```

#### Alternative: Docker Compose (Loki + Grafana only)

If you want to run Loki + Grafana separately and point Web.Api manually:

```powershell
docker compose -f .containers/loki-grafana/docker-compose.yml up -d
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet run --project src/Web.Api
```

> **Note**: Datasource config lives in two places — `Program.cs` (env var for AppHost) and `provisioning/datasources/loki.yaml` (for Compose). If you change one, update the other.

### 7. Verify health

```bash
curl http://localhost:8080/health
```

Should return `200 OK`.

## Project Structure

```
src/
├── Domain/                  # Entities, Value Objects, Domain Events
├── Application/             # Use Cases, Commands, Queries, Interfaces
├── Infrastructure/          # EF Core, Auth, External Services
├── Web.Api/                 # Controllers, Endpoints, Middleware
├── Aspire.AppHost/          # Aspire orchestration (entry point)
└── Aspire.ServiceDefaults/  # OpenTelemetry, resilience patterns
tests/
└── ArchitectureTests/       # Layer validation tests
```

## Running Tests

```bash
dotnet test CleanArchitecture.sln
```

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| `Container runtime 'docker' could not be found` | Docker Desktop not installed or not running | Install Docker Desktop, start it, wait for "Engine running" |
| `UntrustedRoot` SSL errors in console | Dev certificate not trusted | `dotnet dev-certs https --trust` |
| Dashboard loads but shows gRPC / circuit errors | Same as above — SSL cert issue | `dotnet dev-certs https --trust` |
| `web-api` never starts / hangs at `WaitFor` | Docker containers failed to start | Check Docker is running, check port 5432 is free |
| Port 5432 in use | Local PostgreSQL already running | Stop it or change port in `AppHost/Program.cs` |
| Loki not receiving logs in prod | Loki container not running or not healthy | Run with `--launch-profile prod_https`. Check `docker compose logs loki` if using Compose |
| Grafana shows no datasources | Provisioning volume not mounted (Compose) or env var missing (AppHost) | Compose: ensure `.containers/loki-grafana/provisioning/` exists. AppHost: the datasource is configured via `GF_DATASOURCES_LDAP_SECRET_JSON` env var — no volume needed |
| `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL not set` error | Running with wrong or missing launch profile | Use `--launch-profile prod_https` or `--launch-profile dev_https`. The profile sets all required OTLP env vars automatically |
| `applicationUrl must be https` error | Using HTTP-only profile without `ASPIRE_ALLOW_UNSECURED_TRANSPORT` | Use `--launch-profile prod_https` (HTTPS) or `--launch-profile prod_http` (HTTP with transport flag) |
| JWT validation fails | Secret not generated or expired | `dotnet user-jwts create --project src/Web.Api` |
| Build fails with missing Aspire types | Aspire workload not installed | `dotnet workload install aspire` |
| `fail` log about `__EFMigrationsHistory` on first run | EF Core tries to read migrations table before it exists — expected behavior | No action needed. Table is created automatically and migrations apply correctly. Only happens on first run with a clean database. |

### Verifying Docker is working

```bash
docker info
```

If this fails, Docker Desktop isn't running. Start it and try again.

### Verifying the certificate is trusted

```bash
dotnet dev-certs https --check --trust
```

Should output `A trusted certificate was found`.

---
