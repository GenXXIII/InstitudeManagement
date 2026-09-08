# Phase 2 - Database Optimization

Captured: 2026-09-08 (Asia/Bangkok)

Status: Complete

Phase 2 audits the deployed SQL Server schema and adds database constraints and indexes supported by current EF Core query shapes. It does not rewrite LINQ, change API contracts, paginate responses, or modify UI behavior.

## Data ownership clarified

- Management tables are permanent master data: one current row for each student, teacher, course, department, and classroom.
- Enrollment tables are academic ledgers: the same master entity can have one row in each academic year and semester as it progresses from Semester 1 to Semester 2 and then to the next year level.
- Permanent Management codes remain globally unique.
- Enrollment codes follow the linked master identity, so their uniqueness is scoped to `EnrollmentCode + AcademicYear + Semester`.
- Each enrollment ledger also keeps the existing unique master-period key, such as `StudentId + AcademicYear + Semester`. This prevents a student, teacher, course, classroom, or timetable entry from being enrolled twice in one period.

## Live SQL Server audit

Database: `INK_Manangement`, compatibility level 160, Query Store enabled.

| Check | Result |
| --- | --- |
| Domain tables | 19 |
| Primary keys | 19/19 domain tables; clustered `uniqueidentifier Id` |
| Foreign keys | 27/27 enabled and trusted |
| FK leading-index coverage | 27/27 |
| Constraint integrity | `DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS` passed |
| Duplicate period-scoped enrollment codes | 0 |
| Pre-migration indexes | 69 total B-tree indexes, including PKs |
| Post-migration indexes | 88 total B-tree indexes, including PKs |

The Phase 0 value of 131 was not the count returned by `sys.indexes`; this direct Phase 2 audit supersedes that label. The schema had 69 indexes before this migration.

`Payment` is intentionally deferred and has no table to audit yet.

## Relationship map

| Required relationship | Deployed relationship |
| --- | --- |
| Student -> Department | `Students.DepartmentId -> Departments.Id` |
| Course -> Department | `Courses.DepartmentId -> Departments.Id` |
| Enrollment -> Student | `Enrollment.StudentEnrollments.StudentId -> Students.Id` |
| Enrollment -> Course | `Enrollment.CourseAssignments.CourseId -> Courses.Id` |
| Enrollment -> Teacher | `Enrollment.TeacherAssignments.TeacherId -> Teachers.Id`; course assignments also reference their assigned teacher |
| Enrollment -> Classroom | `Enrollment.ClassroomAssignments.ClassroomId -> Classrooms.Id` |
| Timetable enrollment | `Enrollment.TimetableEnrollments.ScheduleEntryId -> ScheduleEntries.Id` |
| Timetable -> Course | `ScheduleEntries.CourseId -> Courses.Id` |
| Timetable -> Teacher | `ScheduleEntries.TeacherId -> Teachers.Id` |
| Timetable -> Classroom | `ScheduleEntries.ClassroomId -> Classrooms.Id` |
| GradeRecord -> Student/Course | `GradeRecords.StudentId -> Students.Id`; `CourseId -> Courses.Id` |
| History | `AuditLogs` and `NotificationHistory` use their own PKs and protected public codes; audit resource IDs are historical references rather than cascading FKs |

All configured delete behavior remains restrictive so master data cannot silently cascade-delete academic records.

## Database constraints

Global unique indexes already protect:

- `StudentCode`, `TeacherCode`, `CourseCode`, `DepartmentCode`, and `ClassroomCode`
- timetable, attendance, grade, class-session, audit, notification, announcement, notification-history, and system-setting public codes
- `SystemSettings(Section, Key)`
- `AttendanceRecords(StudentId, Date)`
- `GradeRecords(StudentId, CourseId, AcademicYear, Term)`
- `ClassSessionRecords(ScheduleEntryId, SessionDate)`

This phase adds period-scoped unique indexes for all five Enrollment tables:

```text
EnrollmentCode + AcademicYear + Semester
```

The existing master-period unique indexes remain in place:

```text
StudentId/ScheduleEntryId/CourseId/TeacherId/ClassroomId + AcademicYear + Semester
```

Rollback-only verification proved that SQL Server rejects a duplicate permanent code, a duplicate enrollment code in the same period, and an invalid Student-to-Department foreign key. No verification data was committed.

## Query-shaped index changes

| Query shape observed in source and Query Store | Index action |
| ---| ---|
| Enrollment readers filter by academic year, semester, and status | Added period/status indexes to all five Enrollment tables |
| Student operations additionally filter shift, department, and year level | Student period index continues with `Shift, DepartmentId, YearLevel` and includes identity/code |
| Course enrollment/dashboard reads group or filter by department/year | Course period index continues with `DepartmentId, YearLevel` and includes course/teacher/code |
| Attendance catalog filters period and orders newest first | Added `AcademicYear, Term, CreatedAtUtc DESC` |
| Dashboard and attendance operations filter attendance date | Added `Date`, including student/status/check-in fields |
| Grade catalog filters academic period | Added `AcademicYear, Term` |
| Dashboard selects scores by update range | Added covering `UpdatedAtUtc INCLUDE (Score)` |
| Dashboard and results filter class-session date, period, department, and year | Added `SessionDate`, `AcademicYear + Term`, and `DepartmentId + YearLevel` |
| Dashboard/history repeatedly read newest audit entries | Added `CreatedAtUtc DESC`, including action/subject/type |
| Results/records find graduated Student audit references | Added filtered `Type + Action + ResourceId` |
| Timetable conflict checks match teacher/classroom plus day and time overlap | Replaced two single-column FK indexes with two composite indexes that retain the same FK-leading columns |

Low-selectivity standalone status indexes were not added. Existing core-entity department indexes were retained, and no duplicate FK index was added.

## SQL logical-read comparison

The same SQL predicates and projections were executed immediately before and after migration against the live local database.

| Representative query | Before | After | Change |
| --- | ---: | ---: | ---: |
| Active morning Student enrollment for current period | 35 | 7 | -80.0% |
| Active Course assignments for current period | 3 | 3 | Already minimal |
| Seven newest audit activities in date range | 361 | 2 | -99.4% |
| Dashboard score projection by update range | 24 | 6 | -75.0% |
| Class-session count by date range | 5 | 2 | -60.0% |
| Distinct graduated Student audit references | 361 | 37 | -89.8% |

Logical reads are a stable local indicator for this schema, not an end-user latency guarantee. The data currently covers mostly one active academic period, so the period indexes become more selective as semester/year history grows.

## Migration

Created and deployed:

```text
20260908114006_OptimizeDatabaseIndexes
```

Files:

- `backend/src/InstituteManagement.Infrastructure/Persistence/Migrations/20260908114006_OptimizeDatabaseIndexes.cs`
- `backend/src/InstituteManagement.Infrastructure/Persistence/Migrations/20260908114006_OptimizeDatabaseIndexes.Designer.cs`
- `backend/src/InstituteManagement.Infrastructure/Persistence/Migrations/InstituteDbContextModelSnapshot.cs`

API startup applied the migration through the normal production path. The migration history reports EF Core `10.0.11`.

## Verification

| Check | Result |
| --- | --- |
| Release backend build | Passed; 0 warnings, 0 errors |
| Application/domain tests | 26 passed |
| Architecture/database-configuration tests | 31 passed |
| Total backend tests | 57 passed |
| Migration deployed | Passed |
| SQL constraint checks | Passed |
| Compose services | SQL Server, Redis, API, and Web healthy; SQL Init exited 0 |
| API smoke routes | 13/13 returned HTTP 200 |

## Phase 3 handoff

The database now supplies the correct constraints and access paths, but Query Store and the live smoke run show EF query-shape work still remains:

- large `GroupJoin`/`FirstOrDefault` enrollment queries
- large entity graphs and photo/JSON columns loaded when only summaries are required
- a 200-parameter timetable `IN` query observed around 0.9 seconds
- unpaginated payloads such as Student Enrollment (about 677 KB), Student Records (about 927 KB), Attendance (about 336 KB), and Grades (about 335 KB)

Those are intentionally left for Phase 3 EF Core optimization and later API/pagination phases.
