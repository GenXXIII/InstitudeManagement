# Architecture

Institute Management follows a pragmatic Clean Architecture layout. The existing product behavior and HTTP contracts remain the source of truth; dependencies point inward and abstractions are added only at boundaries that have more than one meaningful implementation concern.

## Dependency direction

```text
API ───────────────► Application ◄────────────── Infrastructure
                           │                          │
                           └────────► Domain ◄────────┘
```

- `InstituteManagement.Domain` contains one business entity per file and has no framework or persistence dependencies.
- `InstituteManagement.Application` contains feature-oriented MediatR commands, queries, handlers, DTOs, boundary interfaces, and request validation behavior.
- `InstituteManagement.Infrastructure` implements application interfaces with EF Core, SQL Server, Redis, and resource-specific query or management services.
- `InstituteManagement.API` owns HTTP contracts, thin controllers, SignalR delivery, composition, and RFC 7807 exception responses.

Management resources are not represented by a mixed catalog DTO. Students, teachers, classrooms, courses, timetable entries, attendance, departments, and grades each own a response DTO and value contract. The common management boundary exposes only the `Id` and serialized `Values` members required to keep the established HTTP shape stable.

## Request flow

```text
HTTP request
  -> API contract/controller
  -> MediatR request validation
  -> feature handler
  -> application boundary interface
  -> infrastructure implementation
  -> EF Core / Redis / SignalR
```

Controllers do not contain business or database logic. Expected invalid input returns HTTP 400, missing records return 404, relationship and uniqueness conflicts return 409, and unexpected failures return a sanitized 500 response. Validation details use the standard `ValidationProblemDetails.errors` shape.

## Persistence

Each entity has an independent EF Core configuration when it needs indexes, constraints, lengths, precision, or relationships. Foreign-key deletion is restrictive by default because institute history and linked academic records must not disappear through cascades. Unique business identifiers are checked in application workflows and represented as database indexes.

`DatabaseSchemaUpdater` is a compatibility bridge for databases created by earlier project versions. New schema design belongs in `Persistence/Configurations`; the updater should only contain idempotent compatibility steps needed to preserve existing local data. The timetable compatibility step converts only the six exact hourly patterns produced by the former demo seed into the new weekday periods; other custom records are left untouched and shown as needing rescheduling.

## Frontend

Next.js route files are composition points. Business-facing UI, types, API adapters, and feature-specific components live together under `features/<feature>`. Shared presentation primitives stay in `components`, cross-feature transport stays in `lib`, and shell navigation stays in `features/shell`.

Each management resource owns its frontend item/value type, API adapter, editor field configuration, and resource-specific view. `management-client.ts`, the API registry, editor shell, and module router contain only shared transport or composition mechanics.

Timetable period rules are backend-owned domain policy. Monday through Friday expose morning, afternoon, and evening periods; Saturday and Sunday expose morning and afternoon periods only. Every entry owns a Year 1–4 cohort. The timetable management editor loads periods from `GET /api/timetable/periods`. Management presents all 13 learning spaces as rows and teaching periods as columns, with local day/year filters and related course, teacher, cohort-student, and attendance context in each editable class cell. Operations receives the same periods, learning spaces, and weekly schedule for its live room matrix. Arbitrary times and Year values outside 1–4 are rejected by the backend.

Administration settings are persisted configuration, not display-only preferences. A shared frontend provider applies institute identity, academic year, current term, language metadata, time zone, live refresh timing, and new-record defaults immediately after save. Backend management features enforce department-head, cross-department teaching, course-teacher, attendance correction, classroom-device, and shared-room rules. Grade rules define A through E thresholds with F below E; saving them recalculates existing grade letters and invalidates the dashboard distribution. Attendance and grade notification switches control creation of operational alerts, and every settings save creates an audit record.

The academic calendar is an active lifecycle policy. A startup check and hourly hosted check compare the institute-local date with both configured semester windows. Crossing the Semester 1 boundary activates Semester 2. Crossing Semester 2 advances every calendar date by one year and promotes active Year 1–3 students exactly once; Year 4 is deliberately retained because graduation/removal was not authorized. Attendance and grade entities carry `AcademicYear` and `Term`. Management queries only the current pair and blocks completed-period edits/removals, while Records continues to project every period.

Completed timetable delivery has its own persistent `ClassSessionRecord`. A startup check and minute-level recorder create one unique row per schedule/date after the configured end time, capturing course, teacher, classroom, cohort, academic period, totals, and a JSON snapshot of every matching student's daily attendance state. Student, Teacher, and Course operational record readers join these immutable snapshots into their expandable timelines. The current attendance model is daily per student, so the session recorder intentionally freezes that day's Present, Late, Absent, or Excused state rather than inventing course-specific check-ins.

A room's explicit type is either `Classroom` or `Meeting Room`; both are valid teaching spaces and remain real managed records rather than hardcoded presentation elements. Operational status and live study status are separate: management stores availability, starting, offline, or inactive state, while current timetable occupancy derives `In Study` for both room types. The booking rule prevents only teacher and learning-space collisions, so different rooms may run concurrently.

Attendance and grade storage remains record-oriented so each correction, course result, and audit relationship has its own identity. Their Management views are student-oriented projections: one attendance row contains all of the student's dated status/time records, and one grade row contains all department course results with total, average, and overall grade. Editing still targets the individual underlying record through its resource-specific API.

The single department selector below Dashboard remains the global scope for Operation, Record, and Management routes. The pale ice-blue and white visual system is shared through `app/globals.css`.

## Testing

- `InstituteManagement.Application.Tests` tests command/query behavior, request validation, DTO boundaries, and timetable-period delivery by feature.
- `InstituteManagement.Infrastructure.Tests` tests persistence mappings, EF-backed business workflows, and timetable domain policy.
- API contract and browser-level end-to-end coverage should be added when authentication and role policies enter scope.
