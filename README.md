# Institude of New Khmer Management System

A full-stack institute operations platform built from `docs/Layout/SystemLayout.md` and the architecture described in `docs/Personality/AboutMe.md`.

The current milestone includes dashboard reporting, module-specific live operations, relationship-aware current-data management, immutable historical records, configurable institute rules, seeded data, and SignalR events. Authentication, authorization/security, and payments are intentionally deferred.

## Data behavior

- **Institute operations** starts with a full-height one-page dashboard for Students, Teachers, Classrooms, and Courses; each section links to its complete live workspace.
- **Record** contains completed timetable evidence only. Each Class Session is one expandable visual card with time, course, teacher, classroom, student year, totals, and the full Present/Late/Absent/Permission roster. Student Record contains completed-class attendance only; Teacher and Course Record contain only classes that reached their timetable end. Classroom detail is shown inside Course and Class Session cards rather than as a separate Record navigation item.
- The top bar owns searchable Department and All/Year 1–4 scope controls. Sidebar navigation preserves both filters, the global search selects a feature and suggests first/last-name or word-prefix matches, and large relationship dropdowns support type-to-filter selection.
- **Management** is the only place for adding, editing, deactivating, or removing current data. Every module can be scoped by department.
- Attendance management groups all dated statuses and check-in times into one row per student. Grade management groups department-course results into one row per student with total, average, and overall grade.
- **History** uses a Management-style read-only register for every current and inactive entity. Search and status dropdowns filter the register, and each row expands to its complete append-only snapshot history.
- Students and teachers require a stored 4×6 portrait.
- Departments require a real teacher as head of department.
- Courses, schedules, classrooms, attendance, and grades validate their department relationships before saving.
- Timetable management uses backend-defined periods and Year 1–4 cohorts: Monday–Friday has morning, afternoon, and evening sessions; Saturday–Sunday has morning and afternoon sessions. The one-page Operations matrix shows 13 concurrent learning spaces by room and time. Classrooms and meeting rooms are both schedulable and automatically show `In Study` while occupied.
- Administration is institute-wide configuration: saved identity, academic calendar, department/course/classroom policies, attendance workflow, A–F grading, notification routing, language/time zone, and refresh timing are consumed by the shell and validated by backend workflows.
- Semester dates drive automatic lifecycle changes. Semester 1 expiry activates Semester 2; Semester 2 expiry advances the academic year and promotes active Year 1–3 students while preserving Year 4. Grade and attendance rows are tagged by academic year/semester, remain in read-only Records history, and start with a fresh current Management ledger.
- Active dependencies must be reassigned or cancelled before a linked department, teacher, classroom, or course can be deactivated.
- **Settings** has a distinct view and validation for each section; grade boundaries and relationship rules are used by backend workflows.

## Stack

- Next.js 16, React 19, TypeScript, Tailwind CSS
- ASP.NET Core 10, Clean Architecture, MediatR, REST, OpenAPI, SignalR
- Entity Framework Core with SQL Server
- Redis and Docker Compose infrastructure
- xUnit tests

## Database migrations

EF Core migrations are stored in `backend/src/InstituteManagement.Infrastructure/Persistence/Migrations`. Startup applies pending migrations automatically. Existing databases created before migrations are upgraded by the compatibility bridge and baseline-marked without deleting their data.

Create a future migration from the repository root with:

```powershell
dotnet ef migrations add MigrationName --project backend/src/InstituteManagement.Infrastructure --startup-project backend/src/InstituteManagement.API --output-dir Persistence/Migrations -- --environment Production
```

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
backend/tests/InstituteManagement.Application.Tests     Application behavior and validation tests
backend/tests/InstituteManagement.Infrastructure.Tests  EF Core mapping and workflow tests
docker-compose.yml                    Docker Compose environment
docs/                                 Original product and architecture sources
```

See [`docs/architecture.md`](docs/architecture.md) for dependency direction, request flow, validation/error behavior, persistence conventions, and test boundaries.

## Verification

```powershell
dotnet test backend/InstituteManagement.slnx
npm run build --prefix frontend/web
```

## CI/CD

`.github/workflows/ci-cd.yml` runs backend restore/build/tests, frontend install/lint/build, Docker Compose validation, production container builds, and a full-stack smoke test of the health endpoint, Overview API, Institude of New Khmer web app, and logo. Successful pushes to `main` or `v*` tags publish `ink-management-api` and `ink-management-web` images to GitHub Container Registry using the repository `GITHUB_TOKEN`.
