# Northstar Institute Management System

A full-stack institute operations platform built from `docs/Layout/SystemLayout.md` and the architecture described in `docs/Personality/AboutMe.md`.

The current milestone includes dashboard reporting, module-specific live operations, relationship-aware current-data management, immutable historical records, configurable institute rules, seeded data, and SignalR events. Authentication, authorization/security, and payments are intentionally deferred.

## Data behavior

- **Operation** shows live state with a distinct workspace for every sidebar module.
- **Management** is the only place for adding, editing, deactivating, or removing current data. Every module can be scoped by department.
- **Record** is append-only history. It has no add, edit, or delete route; management changes automatically create snapshot entries.
- Students and teachers require a stored 4×6 portrait.
- Departments require a real teacher as head of department.
- Courses, schedules, classrooms, attendance, and grades validate their department relationships before saving.
- Active dependencies must be reassigned or cancelled before a linked department, teacher, classroom, or course can be deactivated.
- **Settings** has a distinct view and validation for each section; grade boundaries and relationship rules are used by backend workflows.

## Stack

- Next.js 16, React 19, TypeScript, Tailwind CSS
- ASP.NET Core 10, Clean Architecture, MediatR, REST, OpenAPI, SignalR
- Entity Framework Core with SQL Server
- Redis and Docker Compose infrastructure
- xUnit tests

## Run locally with Docker

The root `.env` is configured for local development and ignored by Git. From the repository root, run only:

```powershell
docker compose up --build
```

This starts the web app, API, SQL Server, and Redis. Open `http://localhost:3000`; the API health endpoint is `http://localhost:5080/health`. SQL Server data and Redis cache data are stored in named Docker volumes and survive container restarts.

Stop the application with:

```powershell
docker compose down
```

The local data is preserved. Use `docker compose down -v` only when you intentionally want to erase the local database and cache.

## Repository structure

```text
frontend/web/                         Next.js application
backend/src/InstituteManagement.API   HTTP, SignalR, OpenAPI
backend/src/InstituteManagement.Application  MediatR use cases and contracts
backend/src/InstituteManagement.Domain       Business entities
backend/src/InstituteManagement.Infrastructure EF Core and data services
backend/tests/                        Unit tests
infrastructure/docker/                Docker Compose environment
docs/                                 Original product and architecture sources
```

## Verification

```powershell
dotnet test backend/InstituteManagement.slnx
npm run build --prefix frontend/web
```
