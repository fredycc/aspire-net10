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
- Seq for searching and analyzing structured logs
  - Seq is available at http://localhost:5341 by default
- Testing projects
  - Architecture testing

## Prerequisites

| Requirement | Why | Install |
|-------------|-----|---------|
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Runtime and build tooling | [Download](https://dotnet.microsoft.com/download) |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | **Required** — PostgreSQL and Seq run as containers | [Download](https://www.docker.com/products/docker-desktop/) · Enable WSL 2 backend during install |
| [Aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling) | Aspire orchestration SDK | `dotnet workload install aspire` |

> **Docker is mandatory**, not optional. The AppHost starts PostgreSQL and Seq as containers — without Docker they never launch and `web-api` hangs waiting for them.

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

### 6. Start the application via Aspire AppHost

```bash
cd src/Aspire.AppHost
dotnet run
```

This will automatically:
- Start PostgreSQL container with data persisted in `.containers/db/`
- Start Seq container for structured log aggregation
- Launch the Web.Api project
- Configure OpenTelemetry and health checks

### 7. Access the services

| Service | URL |
|---------|-----|
| Web.Api | http://localhost:8080 |
| Seq (logs) | http://localhost:5341 |
| Aspire Dashboard | https://localhost:17168 (URL shown in console output) |

> The dashboard URL changes on each run — use the one printed in the console after `dotnet run`.

### 8. Verify health

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
| `web-api` never starts / hangs at `WaitFor` | Docker containers failed to start | Check Docker is running, check ports 5432 and 5341 are free |
| Port 5432 in use | Local PostgreSQL already running | Stop it or change port in `AppHost/Program.cs` |
| Port 5341 in use | Local Seq already running | Stop it or change port in `AppHost/Program.cs` |
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
