# Phase 1 - Architecture Cleanup

Captured: 2026-09-08 (Asia/Bangkok)

Status: Complete

Phase 1 cleans and enforces backend layer boundaries without performing database or query optimization.

## Architecture direction

Runtime flow:

```text
HTTP -> API -> Application use case -> Application port
                                      -> Infrastructure adapter
                                      -> SQL Server / Redis / SignalR / file system
```

Compile-time references:

```text
API ----------------------> Application ------> Domain
 |                              ^                 ^
 +----------------> Infrastructure --------------+
```

Infrastructure depends inward on Application ports and Domain types. Domain does not depend on Infrastructure.

## Cleanup completed

### API

- Removed the API project's direct EF Core package reference.
- Removed all direct `InstituteDbContext`, EF Core, Redis, persistence, and concrete Infrastructure-service usage.
- Replaced the maintenance middleware's EF query with `IMaintenanceModeReader`.
- Replaced direct database exception handling with the boundary-neutral `PersistenceConflictException`.
- Reduced API source folders to `Contracts`, `Controllers`, `Middleware`, `Routes`, and the `Program.cs` composition root.
- Kept transport-specific upload validation, HTTP responses, Problem Details, routes, and hub endpoint mapping at the API boundary.

### Application

- Added the `IMaintenanceModeReader` port and `MaintenanceModeState` DTO.
- Added the `ISettingsAssetStorage` port.
- Added the `IApplicationStartupTask` boundary for startup workflows.
- Added `PersistenceConflictException` so API does not depend on EF Core exception types.
- Moved notification content validation out of Infrastructure into Application.

### Domain

- Removed `System.ComponentModel.DataAnnotations.Schema` and the EF `[Column]` attribute from the base entity.
- Moved teacher-presence, grade-threshold, and semester-result rules into framework-independent Domain policies.
- Added behavior tests for grade letters, result outcomes, and teacher status normalization.

### Infrastructure

- Moved `InstituteHub` and `SignalRLiveUpdatePublisher` from API into Infrastructure.
- Moved settings asset persistence to `FileSystemSettingsAssetStorage` in Infrastructure.
- Added the EF-backed `MaintenanceModeReader` adapter.
- Hid `DatabaseInitializer` behind `InitializeInfrastructureAsync`.
- Added Infrastructure implementations for Application startup tasks.
- Translated EF `DbUpdateException` at the persistence boundary.
- Preserved the existing SQL column name `CreatedAtUtc` through EF model configuration.

### Documentation and enforcement

- Added `docs/architecture.md` with runtime flow, compile-time direction, responsibilities, validation flow, and adapter boundaries.
- Corrected the stale repository structure in `README.md`.
- Expanded architecture tests to enforce project references, allowed packages, source dependencies, API folder responsibilities, and EF mapping ownership.
- Added domain/application rule tests.

## Verification

| Check | Result |
| --- | --- |
| Release backend build | Passed; 0 warnings, 0 errors |
| Application/domain tests | 26 passed |
| Architecture tests | 18 passed |
| Total backend tests | 44 passed |
| Full `dotnet format --verify-no-changes` | Passed |
| `docker compose up -d --build` | Passed |
| SQL Server, Redis, API, Web | Healthy |
| SQL Init | Exited 0 |
| `/health`, `/api/dashboard`, `/api/settings` | HTTP 200 |
| `/operation/overview`, `/settings` | HTTP 200 |
| Missing settings response | HTTP 404, `application/problem+json` |
| SignalR negotiation | HTTP 200, `application/json` |
| API/web error-pattern log scan | No matches |
| Existing SQL `CreatedAtUtc` mapping | Preserved |

The database row counts used in Phase 0 remained unchanged during the architecture cleanup.

## Deferred observations

- Docker's API build copies the entire backend before `dotnet restore`, so source-only edits invalidate the restore layer. The final rebuild still passed, but Docker layer optimization belongs to a later production/build phase.
- Database indexes, query plans, migrations, EF tracking/query shape, caching performance, and pagination were deliberately not tuned in Phase 1.

## Phase gate

Phase 1 is complete. Phase 2 can begin from the enforced architecture documented in `docs/architecture.md`.
