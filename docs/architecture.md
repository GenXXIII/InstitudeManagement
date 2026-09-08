# Backend architecture

The backend uses Clean Architecture with inward compile-time dependencies and outward runtime adapters.

## Dependency direction

The request flow and the project-reference direction are deliberately different.

Runtime request flow:

```text
HTTP client
    -> API boundary
    -> Application use case
    -> Application port
    -> Infrastructure adapter
    -> SQL Server / Redis / SignalR / file system
```

Compile-time project references:

```text
API ----------------------> Application ------> Domain
 |                              ^                 ^
 +----------------> Infrastructure --------------+
```

- Domain references no other production project or framework package.
- Application references Domain and MediatR.
- Infrastructure references Application and Domain to implement their ports.
- API references Application for use cases and Infrastructure only as the composition root that registers and exposes adapters.
- Domain and Application never reference Infrastructure or API.

This direction keeps business code independent from SQL Server and delivery technology while still allowing API startup to compose the executable application.

## Layer responsibilities

### API

Allowed concerns:

- HTTP routing, request contracts, upload-boundary checks, status codes, and Problem Details responses
- Authentication and authorization adapters when they are introduced
- Middleware and transport configuration
- Application startup composition

The API must not query `InstituteDbContext`, use EF Core or Redis directly, implement file storage, or contain SignalR publishers/hubs.

### Application

Allowed concerns:

- Commands, queries, handlers, DTOs, and use-case orchestration
- Business-workflow ports implemented by Infrastructure
- Cross-field and use-case validation
- Boundary-neutral exceptions and startup-task contracts

Application code must not use ASP.NET Core request types, EF Core, SQL clients, Redis clients, or Infrastructure types.

### Domain

Allowed concerns:

- Entities and value objects
- Academic, grade, result, attendance, timetable, and identity rules
- Framework-independent state and behavior

Domain code must not contain EF Core/data-annotation mapping, JSON serialization, ASP.NET Core, MediatR, configuration, logging, database, or cache concerns.

### Infrastructure

Allowed concerns:

- EF Core mappings, migrations, `InstituteDbContext`, SQL Server initialization, and persistence implementations
- Redis caching
- SignalR hub and live-update publisher implementation
- File-system asset storage
- Hosted/background adapters and other external-service implementations

Infrastructure may depend on Application ports and Domain types. It must not depend on the API project or define controllers.

## Boundary behavior

### Validation and responses

1. API validates transport-specific details such as uploaded-file size and content type.
2. Application validation behavior validates commands and business-facing input combinations.
3. Domain policies apply framework-independent academic rules.
4. Infrastructure translates EF Core write conflicts into `PersistenceConflictException`.
5. API maps boundary-neutral exceptions to HTTP Problem Details.

### Maintenance mode

`MaintenanceModeMiddleware` owns the HTTP 503 response. It obtains state through the Application `IMaintenanceModeReader` port; the EF Core query is implemented by Infrastructure.

### Live updates

Application publishes through `ILiveUpdatePublisher`. Infrastructure implements that port with SignalR and owns `InstituteHub`. API only maps the hub endpoint.

### Startup

Infrastructure owns database initialization. Startup business workflows implement the Application `IApplicationStartupTask` contract. API invokes only these public boundaries and does not resolve `InstituteDbContext` or concrete Infrastructure services.

## Automated enforcement

`InstituteManagement.Architecture.Tests` verifies:

- exact production project references
- layer-appropriate NuGet dependencies
- forbidden source dependencies in Domain, Application, and API
- API source folders limited to transport and composition concerns
- feature-folder organization
- EF configuration ownership, including the `CreatedAtUtc` column mapping

Run the gate with:

```powershell
dotnet test backend/InstituteManagement.slnx --configuration Release
```
