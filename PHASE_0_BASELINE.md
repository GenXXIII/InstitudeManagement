# Phase 0 - Project Baseline

Captured: 2026-09-08 (Asia/Bangkok)

Status: Complete

This phase records the current project state before optimization. It does not include architecture, database, EF Core, API, pagination, or UI optimization work.

## Source snapshot

- Repository: `D:\Coding\Project\InstitudeManagement`
- Branch: `main` tracking `origin/main`
- Commit: `58269958654e716fcf21d93ff819c56fe7e7e42e`
- Commit subject: `Optimize Previous Behavious`
- Existing worktree state before Phase 0: 9 modified files, 78 insertions, 18 deletions
- The nine pre-existing modifications were preserved. Phase 0 added only this baseline report.

Pre-existing modified files:

- `backend/src/InstituteManagement.API/Program.cs`
- `frontend/web/app/settings/[section]/page.tsx`
- `frontend/web/app/settings/page.tsx`
- `frontend/web/features/administration/components/maintenance-mode-card.tsx`
- `frontend/web/features/administration/components/settings-section.tsx`
- `frontend/web/features/shell/nav-group.tsx`
- `frontend/web/features/shell/navigation-config.ts`
- `frontend/web/features/shell/topbar-search-model.ts`
- `frontend/web/features/shell/use-record-entry-navigation.ts`

## System topology

- Backend: .NET 10, ASP.NET Core controllers, MediatR, EF Core 10, SQL Server, Redis
- Backend projects: API, Application, Domain, Infrastructure, Application tests, Architecture tests
- Project direction:
  - API -> Application and Infrastructure
  - Application -> Domain
  - Infrastructure -> Application and Domain
  - Domain -> no project dependency
- Frontend: Next.js 16.3.1 App Router, React 19.2.8, TypeScript 5.9.3, Tailwind CSS 4.3.3, SignalR 9.0.19
- Compose services: `redis`, `sqlserver`, `sql-init`, `api`, `web`
- Published ports: web `3000`, API `5080`, SQL Server `1433`

## Code and test inventory

| Measure | Baseline |
| --- | ---: |
| C# files / lines | 451 / 30,316 |
| TypeScript files / lines | 200 / 8,076 |
| CSS files / lines | 2 / 917 |
| API controllers | 27 |
| Attributed API endpoints | 73 |
| EF Core migrations | 14 migrations plus the model snapshot; database history has 14 rows |
| Next.js `page.tsx` files | 21 |
| Backend tests | 21 passing |

Line counts include Git-tracked files and are intended only for before/after comparison.

## Toolchain baseline

| Tool | Version |
| --- | --- |
| Local .NET SDK | 10.0.300 |
| Local Node.js | 26.5.0 |
| Local npm | 12.0.1 |
| Docker Engine client | 29.4.3 |
| Docker Compose | 5.1.3 |
| Frontend container Node.js | 22 (`node:22-alpine`) |
| CI Node.js | 24 |

## Validation results

| Check | Result | Wall time |
| --- | --- | ---: |
| `dotnet build backend/InstituteManagement.slnx --configuration Release` | Passed; 0 warnings, 0 errors | 40.9 s |
| `dotnet test backend/InstituteManagement.slnx --configuration Release --no-build` | Passed; 21/21 | 5.7 s |
| `npm.cmd run lint` in `frontend/web` | Passed | 29.5 s |
| `npm.cmd run build` in `frontend/web` | Passed; 21 App Router pages listed | 48.2 s |
| `docker compose up -d --build` | Passed; current source rebuilt and started | Passed |
| Compose health | SQL Server, Redis, API, and Web healthy; SQL Init exited 0 | Passed |
| Visual smoke at 1440x1000 | Operation Overview rendered without a visible error state | Passed |

These observed wall times are workstation snapshots, not optimization targets. Use a scripted, isolated benchmark in the relevant later phase.

## Local HTTP response baseline

Method: one first request followed by ten sequential warm requests from the host to the rebuilt Compose stack. Warm P50/P95 are descriptive localhost measurements, not a load or stress test.

| Route | Status | First request | Warm P50 | Warm P95 | Response bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| `GET /health` (API) | 200 | 289.61 ms | 10.51 ms | 18.74 ms | 7 |
| `GET /api/dashboard` | 200 | 1,270.05 ms | 13.72 ms | 37.44 ms | 3,561 |
| `GET /api/settings` | 200 | 138.35 ms | 17.93 ms | 89.84 ms | 8,625 |
| `GET /` (web) | 200 | 11.45 ms | 7.73 ms | 10.79 ms | 27,973 |
| `GET /operation/overview` | 200 | 498.45 ms | 43.23 ms | 132.32 ms | 28,616 |
| `GET /management/students` | 200 | 74.95 ms | 28.45 ms | 41.25 ms | 28,859 |
| `GET /settings` | 200 | 31.90 ms | 8.34 ms | 9.76 ms | 27,549 |

The root HTML title is `Institude of New Khmer`.

## Database baseline

- Database: `INK_Manangement`
- Allocated data/log files: 144.00 MiB
- User tables: 20
- Non-heap indexes: 131
- Current row counts:
  - Students: 800
  - StudentEnrollments: 800
  - AttendanceRecords: 800
  - GradeRecords: 800
  - AuditLogs: 2,975
  - ClassSessionRecords: 280
  - ScheduleEntries: 180
  - TimetableEnrollments: 160
  - SystemSettings: 249
  - Notifications: 29
  - NotificationHistory: 65
  - Departments: 5
  - Teachers: 40
  - Courses: 40
  - Classrooms: 13

## Runtime and artifact size snapshot

Idle-like container snapshot after the HTTP sample:

| Container | CPU | Memory |
| --- | ---: | ---: |
| SQL Server | 1.60% | 1.06 GiB |
| Redis | 0.63% | 13.55 MiB |
| API | 0.11% | 198.4 MiB |
| Web | 5.75% | 142.0 MiB |

Image sizes:

| Image | Size |
| --- | ---: |
| `ink-api:latest` | 120.5 MiB |
| `ink-web:latest` | 219.9 MiB |
| `ink-sql-server:latest` | 596.0 MiB |
| `ink-redis:latest` | 15.5 MiB |

Frontend production output:

- `generated/`: 165.48 MiB on disk
- `generated/static/`: 2.52 MiB across 33 files
- Largest uncompressed JavaScript chunk: 223.56 KiB

## Baseline findings to carry forward

1. The validation gate is green: backend build/tests, frontend lint/build, rebuilt containers, API routes, web routes, and visual smoke all pass.
2. The local, CI, and container Node.js major versions are different (26, 24, and 22). A later phase should intentionally standardize or document this matrix.
3. `README.md` points to `docs/architecture.md` and `InstituteManagement.Infrastructure.Tests`, but neither path exists in this checkout. Documentation/test topology is already drifting.
4. There are only 21 backend tests for a system exposing 73 attributed endpoints. This is an inventory observation, not a coverage percentage; Phase 16 should establish actual line/branch and integration coverage.
5. The response measurements are sequential localhost samples. Phase 17 must define concurrent scenarios, datasets, service-level targets, and repeatable tooling before drawing capacity conclusions.
6. SQL Server's memory footprint, cold dashboard response, Operation Overview response, frontend static assets, and container image sizes are recorded as comparison points. They are not diagnosed or changed in Phase 0.

## Phase gate

Phase 0 is complete. Preserve this file as the before-state and update comparisons in later phases without rewriting the original measurements.
