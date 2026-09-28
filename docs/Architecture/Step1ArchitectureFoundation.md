# Step 1 — Architecture Foundation

Status: established on 2026-09-28.

This document records the current architecture, module ownership, dependency direction, state ownership, structural audit, and findability rules for InstituteManagement. It is intentionally limited to architecture foundation work. It does not introduce database optimization, caching changes, frontend performance work, authentication, authorization, or new product behavior.

The repository remains a modular monolith. The target is a feature-oriented Clean Architecture that preserves existing HTTP contracts, persistence behavior, lifecycle rules, and Administrator/Teacher/Student boundaries.

## 1. Project inventory

### Backend

| Project | Responsibility |
| --- | --- |
| `backend/src/InstituteManagement.API` | HTTP contracts, controllers, middleware, routes, composition root |
| `backend/src/InstituteManagement.Application` | Use cases, MediatR commands/queries/handlers, DTOs, validation, boundary interfaces |
| `backend/src/InstituteManagement.Domain` | Entities, value objects, and framework-independent business policies |
| `backend/src/InstituteManagement.Infrastructure` | EF Core, SQL Server, Redis adapter, SignalR, Bakong, persistence-backed boundary implementations |
| `backend/tests/InstituteManagement.Application.Tests` | Application behavior and validation checks |
| `backend/tests/InstituteManagement.Architecture.Tests` | Dependency, folder, and persistence ownership checks |
| `backend/tests/InstituteManagement.Infrastructure.Tests` | Persistence-backed workflow checks |

### Frontend

| Project | Responsibility |
| --- | --- |
| `frontend/web` | Next.js Administrator portal, organized under `features/<feature>` |
| `frontend/mobile` | Expo Teacher and Student portals with role-specific screens |

The Administrator web currently owns feature folders for administration, assessment, attendance, dashboard, enrollment, finance, history, management, notifications, operations, record, results, search, shell, and timetable. Route files in `app/` remain composition points.

## 2. Dependency direction

```text
API ------------> Application ------------> Domain
 |                     ^                       ^
 |                     |                       |
 +------------> Infrastructure ---------------+
                 technical implementations
```

Concrete project references are:

- Domain references no production project.
- Application references Domain.
- Infrastructure references Application and Domain.
- API references Application and Infrastructure so `Program.cs` can compose the executable.

The API-to-Infrastructure reference is a composition-root dependency only. Controllers must not use `InstituteDbContext`, EF Core, Redis, SQL, or infrastructure services directly. They call MediatR requests or Application-owned interfaces.

The following are hard rules:

- Domain must not depend on ASP.NET Core, EF Core, Redis, HTTP, SignalR, or Bakong.
- Application must not depend on API or Infrastructure.
- Infrastructure implements Application boundaries and owns technical integration details.
- Queries read state; commands change state. A query must not hide a write.
- Domain rules should move into entities, value objects, or policies when doing so does not change established behavior.
- An interface is introduced only for a real layer or module boundary.

## 3. Conceptual modules and current code mapping

The conceptual product boundaries are stable even where existing folder names are more specific.

| Conceptual module | Current backend areas | Current web areas |
| --- | --- | --- |
| Management | `Features/Management`, `Entities/Management`, `Services/Management` | `features/management` |
| Enrollment | `Features/Enrollment`, `Entities/Enrollment`, `Services/Enrollment` | `features/enrollment` |
| Operation | `Features/Operations`, `Features/Attendance`, `Features/Timetable`, matching infrastructure services | `features/operations`, `features/attendance`, `features/timetable` |
| Assessment | `Features/Grades`, `Features/Results`, `Entities/Grades`, matching infrastructure services | `features/assessment`, `features/results` |
| Finance | `Features/Finance`, `Entities/Finance`, `Services/Finance` | `features/finance` |
| Record | `Features/Record`, `Entities/Records`, `Services/Record` | `features/record` |
| History | `Features/History`, `Entities/History`, `Services/History` | `features/history` |
| Administration | `Features/Administration`, administration services/settings | `features/administration` |
| Notifications | `Features/Notifications`, notification entities/services | `features/notifications` |

These mappings are deliberate. Step 1 does not rename every existing `Attendance`, `Grades`, or `Results` folder merely to reproduce a conceptual diagram.

## 4. Ownership map

| Feature or data | Owner | Ownership rule |
| --- | --- | --- |
| Student identity and permanent `StudentCode` | Management | Permanent identity; never semester-specific |
| Teacher identity and permanent `TeacherCode` | Management | Permanent identity and profile |
| Department, course, classroom, schedule definition | Management | Master/reference data |
| Student academic journey and current period | Enrollment | One continuous journey represented by period enrollment ledgers |
| Teacher, course, classroom, and timetable assignments | Enrollment | Period-specific relationships to Management records |
| Current class activity and attendance | Operation | Current academic period only |
| Grade submission and confirmation workflow | Assessment | Current academic assessment workflow |
| Semester Result publication | Assessment | Publication/locking decision for the completed result |
| Financial account, declaration, balance, and payment rows | Finance | Finance is the single owner of payment state |
| Eligibility to create the next enrollment | Enrollment workflow | Finance and Assessment supply gates; Enrollment creates enrollment |
| Completed class-session evidence and semester records | Record | Completed-period snapshots; not current-operation state |
| Read-only archive and audit presentation | History | Presentation/query boundary, not a duplicate owner of every entity |
| Institute settings and academic calendar policy | Administration | Institute-wide configuration and rollover orchestration |
| Announcements and notification delivery/history | Notifications | Messaging state and delivery history |
| Dashboard and global search | Read models | They project owner data and do not own it |

If two modules appear to own the same mutable state, the table above decides the owner. Other modules reference the state or request a workflow; they do not silently maintain a second copy.

## 5. Lifecycle boundaries

### Student academic lifecycle

```text
Management identity
  -> Enrollment for the active academic period
  -> Operation and Finance for current activity
  -> Assessment confirmation/publication
  -> semester-end gate
  -> Record snapshot
  -> History presentation
  -> Enrollment creates the next period when eligible
```

Management identity is permanent. Enrollment is the journey. Operation is current only. Record and History never treat the current semester as completed history.

### Finance state

```text
Undeclared/Pending -> Pending -> Partial -> Paid -> ClosedAtUtc/read-only
                              \-> Cancelled

Payment row: Completed -> Cancelled or Refunded
```

Only a fully paid, zero-balance account can close. Closing Finance does not itself own next-period enrollment. Finance supplies its completed state to the Enrollment progression workflow. The academic-calendar rollover still controls when the next period becomes current and archived records become historical.

### Assessment and result state

Grade submission/confirmation remains Assessment-owned. `SemesterResultPublication` marks a result published and read-only. A published result plus a closed paid account can satisfy progression gates, but Enrollment owns creation of the next enrollment.

### Record and History state

Record is completed academic evidence. History is a read-only presentation/archive boundary. Neither module edits current Operation rows.

## 6. Feature-slice rules

A backend use case belongs under its owner and resource:

```text
Application/Features/<Owner>/<Resource>/<UseCase>/
  <UseCase>Command.cs or <UseCase>Query.cs
  <UseCase>Handler.cs
  <UseCase>Validator.cs       when validation is non-trivial
  <UseCase>Dto.cs             when the read/write contract is use-case specific
```

Command and handler may remain together only when the file still has one clear use-case responsibility. Do not create repository, manager, provider, processor, or factory interfaces merely for symmetry.

Frontend route files compose feature UI. Business UI, feature API adapters, and feature types stay under `features/<owner>`. `components/` contains only reusable presentation primitives; `lib/` contains cross-feature transport and utilities.

## 7. Findability index

| Question | Primary location |
| --- | --- |
| Where is Student management? | `Application/Features/Management/Students`, `Services/Management/Students`, `features/management/students` |
| Where is Enrollment? | `Application/Features/Enrollment`, `Services/Enrollment`, `features/enrollment` |
| Where is current attendance? | `Application/Features/Attendance`, `Services/Attendance`, `features/operations/attendance` |
| Where is payment? | `Application/Features/Finance/Payments`, `Services/Finance/Payments`, `features/finance` |
| Where are financial accounts? | `Application/Features/Finance/Accounts`, `Services/Finance/Accounts`, `features/finance/accounts` |
| Where is result submission/publication? | `Application/Features/Grades`, `Application/Features/Results`, `features/assessment`, `features/results` |
| Where is completed-period evidence? | `Application/Features/Record`, `Services/Record`, `features/record` |
| Where is historical Student data? | `Application/Features/History`, `Services/History/Students`, `features/history` |
| Where is Student progression? | `Application/Features/Enrollment/Students/Progression`, `Services/Enrollment/Students` |

## 8. Step 1 structural audit

| Check | Result | Evidence or action |
| --- | --- | --- |
| API/Application/Domain/Infrastructure projects exist | Pass | Four production projects in `backend/src` |
| Project references point inward | Pass | Existing architecture checks enforce exact references |
| Domain is framework-independent | Pass | No ASP.NET Core, EF Core, Redis, HTTP, or Infrastructure dependency |
| Application avoids concrete Infrastructure | Pass | Application references Domain and MediatR only |
| API avoids persistence/cache logic | Pass | Controllers contain transport mapping and application dispatch only |
| Backend features are resource-oriented | Pass | Management and Enrollment are already split by resource/use case |
| EF configuration belongs to Infrastructure | Pass | Dedicated configurations live under `Persistence/Configurations` |
| Frontend business UI is feature-local | Pass | Feature UI lives under `frontend/web/features` |
| Giant Finance implementation mixed responsibilities | Fixed | Split into account operations, payment operations, and shared persistence helpers |
| Finance owned next-enrollment creation | Fixed | Progression contract and implementation moved to Enrollment/Students |
| Finance DTOs were flat and mixed | Fixed | Grouped into Accounts and Payments slices without changing namespaces/contracts |
| Finance workspace mixed page and modal responsibilities | Fixed | Account modal moved to `features/finance/accounts`; shared formatting centralized |
| Generated migrations are large | Accepted | Generated persistence artifacts are not hand-split |

Existing direct-service controllers are accepted only where they depend on an Application-owned boundary and remain transport-only. New multi-step workflows should prefer MediatR use-case handlers. Existing direct-service endpoints should migrate incrementally when their behavior is already protected; Step 1 does not manufacture handlers that merely forward one call.

## 9. Deliberately unchanged

- HTTP routes and request/response shapes.
- Database schema, migrations, indexes, and SQL queries.
- Redis behavior and cache strategy.
- Bakong and mock-payment behavior.
- Authentication and authorization.
- Management/Enrollment/Operation/Assessment/Finance/Record/History lifecycle behavior.
- Teacher and Student mobile role boundaries.
- Existing grading, attendance, and finance rules.

## 10. Verification gate

Step 1 is complete only when the final tree satisfies all of the following:

- Backend Release build succeeds with no errors.
- Existing backend tests pass sequentially.
- Frontend lint and production build succeed.
- Docker Compose configuration and image builds succeed.
- The application starts and representative health/API/web requests succeed.
- `git diff --check` succeeds.

Final verification for this checkpoint:

- Backend Release build: succeeded with 0 warnings and 0 errors.
- Architecture checks: 31 passed.
- Application checks: 63 passed.
- Infrastructure checks: 46 passed.
- Frontend lint: passed.
- Frontend production build: passed, including TypeScript and all 19 generated routes.
- Docker Compose configuration: valid.
- API and web images: built successfully.
- Full stack: started successfully with healthy API, Redis, and SQL Server services.
- Runtime smoke checks: HTTP 200 from `/health`, `/api/finance?status=All`, `/`, `/finance`, and `/enrollment/students`.

This document records architecture ownership. Future changes should update it only when an ownership or dependency decision genuinely changes.
