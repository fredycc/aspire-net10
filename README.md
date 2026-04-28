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

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) with Compose v2
- [Aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling) (install via `dotnet workload install aspire`)
- [Seq](https://datalust.co/seq) (optional — runs as container via Aspire)

## Getting Started

Follow these steps **in order** to run the project successfully:

### 1. Clone the repository

```bash
git clone <repo-url>
cd aspire-net10
```

### 2. Restore dependencies

```bash
dotnet restore CleanArchitecture.sln
```

### 3. Generate JWT secret

The project uses `dotnet user-jwts` for local development JWT secrets:

```bash
dotnet user-jwts create --project src/Web.Api
```

This stores the secret in your local user-secrets store — never committed to source control.

### 4. Start the application via Aspire AppHost

```bash
cd src/Aspire.AppHost
dotnet run
```

This will automatically:
- Start PostgreSQL container with data persisted in `.containers/db/`
- Start Seq container for structured log aggregation
- Launch the Web.Api project
- Configure OpenTelemetry and health checks

### 5. Access the services

| Service | URL |
|---------|-----|
| Web.Api | http://localhost:8080 |
| Seq (logs) | http://localhost:5341 |
| Aspire Dashboard | http://localhost:15000 |

### 6. Verify health

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

| Issue | Solution |
|-------|----------|
| Docker not running | Start Docker Desktop before running AppHost |
| Port 5432 in use | Stop local PostgreSQL or change port in AppHost |
| Port 5341 in use | Stop local Seq or change port in AppHost |
| JWT validation fails | Run `dotnet user-jwts create --project src/Web.Api` |
| Build fails | Run `dotnet workload install aspire` to install Aspire |

---
