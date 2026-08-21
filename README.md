# Institude of New Khmer Management System

A full-stack institute operations platform built from `docs/Layout/SystemLayout.md` and the architecture described in `docs/Personality/AboutMe.md`.

The current milestone includes dashboard reporting, module-specific live operations, relationship-aware current-data management, immutable historical records, configurable institute rules, seeded data, and SignalR events. Authentication, authorization/security, and payments are intentionally deferred.

## Data behavior

- **Institute operations** starts with a one-page dashboard for Students, Teachers, Classrooms, and Courses; each card links to its complete live workspace.
- **Record** is a read-only operational log for students, teachers, classrooms, and courses. Each row expands to show saved timetable, attendance, course, and assessment activity over time; it has no add, edit, deactivate, or remove controls.
- **Management** is the only place for adding, editing, deactivating, or removing current data. Every module can be scoped by department.
- **History** uses a Management-style read-only register for every current and inactive entity. Search and status dropdowns filter the register, and each row expands to its complete append-only snapshot history.
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

## SQL Server login

Use these values for SQL Server Management Studio or Azure Data Studio when the Docker stack is running:

```text
Server address: 127.0.0.1,1433
SQL Server name: INK-SQL-SERVER
Database: INK_Manangement
Authentication: SQL Server Authentication
Login: sa
Password: NorthstarLocal!2026
Encryption: Optional (or Trust server certificate: Yes)
```

The API container uses `sqlserver,1433` as its Docker network address. SQL Server reports its logical server name as `INK-SQL-SERVER`. The local password is defined by `SQL_PASSWORD` in the ignored root `.env`; `.env.example` contains the development default.

## Repository structure

```text
frontend/web/                         Next.js application
backend/src/InstituteManagement.API   HTTP, SignalR, OpenAPI
backend/src/InstituteManagement.Application  MediatR commands, queries, handlers, DTOs, and interfaces
backend/src/InstituteManagement.Domain       One business entity per file
backend/src/InstituteManagement.Infrastructure EF Core configurations and resource-specific services
backend/tests/                        Unit tests
docker-compose.yml                    Docker Compose environment
docs/                                 Original product and architecture sources
```

## Verification

```powershell
dotnet test backend/InstituteManagement.slnx
npm run build --prefix frontend/web
```

## CI/CD

`.github/workflows/ci-cd.yml` runs backend restore/build/tests, frontend install/lint/build, Docker Compose validation, production container builds, and a full-stack smoke test of the health endpoint, Overview API, INK-branded web app, and logo. Successful pushes to `main` or `v*` tags publish `ink-management-api` and `ink-management-web` images to GitHub Container Registry using the repository `GITHUB_TOKEN`.
