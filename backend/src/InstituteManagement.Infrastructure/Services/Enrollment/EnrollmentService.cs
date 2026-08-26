using System.Text.Json;
using InstituteManagement.Application.Abstractions;
using InstituteManagement.Application.DTOs.Enrollment;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Domain.Timetables;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Enrollment;

public sealed class EnrollmentService(InstituteDbContext db, InstituteCache cache) : IEnrollmentService
{
    public async Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(string resource, string? search, Guid? departmentId, int? year, CancellationToken ct)
    {
        var period = await CurrentPeriodAsync(ct);
        return resource.ToLowerInvariant() switch
        {
            "students" => await StudentsAsync(search, departmentId, year, period, ct),
            "teachers" => await TeachersAsync(search, departmentId, year, period, ct),
            "courses" => await CoursesAsync(search, departmentId, year, period, ct),
            "classrooms" => await ClassroomsAsync(search, departmentId, year, period, ct),
            "timetable" => await TimetableAsync(search, departmentId, year, period, ct),
            "departments" => await DepartmentsAsync(search, departmentId, year, period, ct),
            _ => throw new KeyNotFoundException($"Enrollment resource '{resource}' is not supported.")
        };
    }

    public async Task<EnrollmentItemDto> UpdateAsync(string resource, Guid resourceId, Dictionary<string, string> values, CancellationToken ct)
    {
        var period = await CurrentPeriodAsync(ct);
        var result = resource.ToLowerInvariant() switch
        {
            "students" => await UpdateStudentAsync(resourceId, values, period, ct),
            "teachers" => await UpdateTeacherAsync(resourceId, values, period, ct),
            "courses" => await UpdateCourseAsync(resourceId, values, period, ct),
            "classrooms" => await UpdateClassroomAsync(resourceId, values, period, ct),
            "timetable" => await UpdateTimetableAsync(resourceId, values, period, ct),
            _ => throw new KeyNotFoundException($"Enrollment resource '{resource}' is not supported.")
        };
        await db.SaveChangesAsync(ct);
        await cache.InvalidateDashboardAsync(ct);
        return result;
    }

    public async Task<bool> RemoveAsync(string resource, Guid resourceId, CancellationToken ct)
    {
        var period = await CurrentPeriodAsync(ct);
        var removed = resource.ToLowerInvariant() switch
        {
            "students" => await RemoveStudentAsync(resourceId, period, ct),
            "teachers" => await RemoveTeacherAsync(resourceId, period, ct),
            "courses" => await RemoveCourseAsync(resourceId, period, ct),
            "classrooms" => await RemoveClassroomAsync(resourceId, period, ct),
            "timetable" => await RemoveTimetableAsync(resourceId, ct),
            _ => throw new KeyNotFoundException($"Enrollment resource '{resource}' is not supported.")
        };
        if (!removed) return false;
        await db.SaveChangesAsync(ct);
        await cache.InvalidateDashboardAsync(ct);
        return true;
    }

    private async Task<bool> RemoveStudentAsync(Guid id, Period period, CancellationToken ct)
    {
        var enrollment = await db.StudentEnrollments.FirstOrDefaultAsync(x => x.StudentId == id && x.AcademicYear == period.AcademicYear && x.Semester == period.Semester, ct);
        if (enrollment is null || enrollment.Status == "Removed") return false;
        var student = await db.Students.FindAsync([id], ct) ?? throw new KeyNotFoundException("Student not found.");
        var values = AssignmentValues(("departmentId", enrollment.DepartmentId.ToString()), ("year", enrollment.YearLevel.ToString()), ("shift", enrollment.Shift), ("status", enrollment.Status), ("academicYear", enrollment.AcademicYear), ("semester", enrollment.Semester));
        enrollment.Status = "Removed"; enrollment.UpdatedAtUtc = DateTime.UtcNow;
        student.DepartmentId = null; student.YearLevel = 0; student.Shift = ""; student.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(Audit(id, "Student", student.StudentCode, "Enrollment removed", values));
        return true;
    }

    private async Task<bool> RemoveTeacherAsync(Guid id, Period period, CancellationToken ct)
    {
        var assignment = await db.TeacherAssignments.FirstOrDefaultAsync(x => x.TeacherId == id && x.AcademicYear == period.AcademicYear && x.Semester == period.Semester, ct);
        if (assignment is null || assignment.Status == "Removed") return false;
        if (await db.CourseAssignments.AnyAsync(x => x.TeacherId == id && x.AcademicYear == period.AcademicYear && x.Semester == period.Semester && x.Status != "Removed", ct)
            || await db.ScheduleEntries.AnyAsync(x => x.TeacherId == id && x.Status != "Cancelled", ct)
            || await db.Departments.AnyAsync(x => x.HeadTeacherId == id, ct))
            throw new InvalidOperationException("Remove this teacher's active course, timetable, and department-head relationships first.");
        var teacher = await db.Teachers.FindAsync([id], ct) ?? throw new KeyNotFoundException("Teacher not found.");
        var values = AssignmentValues(("departmentId", assignment.DepartmentId?.ToString() ?? ""), ("status", assignment.Status), ("academicYear", assignment.AcademicYear), ("semester", assignment.Semester));
        assignment.Status = "Removed"; assignment.UpdatedAtUtc = DateTime.UtcNow;
        teacher.DepartmentId = null; teacher.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(Audit(id, "Teacher", teacher.TeacherCode, "Assignment removed", values));
        return true;
    }

    private async Task<bool> RemoveCourseAsync(Guid id, Period period, CancellationToken ct)
    {
        var assignment = await db.CourseAssignments.FirstOrDefaultAsync(x => x.CourseId == id && x.AcademicYear == period.AcademicYear && x.Semester == period.Semester, ct);
        if (assignment is null || assignment.Status == "Removed") return false;
        if (await db.ScheduleEntries.AnyAsync(x => x.CourseId == id && x.Status != "Cancelled", ct))
            throw new InvalidOperationException("Remove this course's active timetable relationships first.");
        var course = await db.Courses.FindAsync([id], ct) ?? throw new KeyNotFoundException("Course not found.");
        var values = AssignmentValues(("departmentId", assignment.DepartmentId.ToString()), ("teacherId", assignment.TeacherId?.ToString() ?? ""), ("year", assignment.YearLevel.ToString()), ("capacity", assignment.Capacity.ToString()), ("status", assignment.Status), ("academicYear", assignment.AcademicYear), ("semester", assignment.Semester));
        assignment.Status = "Removed"; assignment.UpdatedAtUtc = DateTime.UtcNow;
        course.DepartmentId = null; course.TeacherId = null; course.Capacity = 0; course.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(Audit(id, "Course", course.CourseCode, "Assignment removed", values));
        return true;
    }

    private async Task<bool> RemoveClassroomAsync(Guid id, Period period, CancellationToken ct)
    {
        var assignment = await db.ClassroomAssignments.FirstOrDefaultAsync(x => x.ClassroomId == id && x.AcademicYear == period.AcademicYear && x.Semester == period.Semester, ct);
        if (assignment is null || assignment.Status == "Removed") return false;
        if (await db.ScheduleEntries.AnyAsync(x => x.ClassroomId == id && x.Status != "Cancelled", ct))
            throw new InvalidOperationException("Remove this learning space's active timetable relationships first.");
        var room = await db.Classrooms.FindAsync([id], ct) ?? throw new KeyNotFoundException("Classroom not found.");
        var values = AssignmentValues(("departmentId", assignment.DepartmentId?.ToString() ?? ""), ("capacity", assignment.Capacity.ToString()), ("access", assignment.Access), ("status", assignment.Status), ("academicYear", assignment.AcademicYear), ("semester", assignment.Semester));
        assignment.Status = "Removed"; assignment.UpdatedAtUtc = DateTime.UtcNow;
        room.DepartmentId = null; room.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(Audit(id, "Classroom", room.ClassroomCode, "Assignment removed", values));
        return true;
    }

    private async Task<bool> RemoveTimetableAsync(Guid id, CancellationToken ct)
    {
        var entry = await db.ScheduleEntries.FindAsync([id], ct);
        if (entry is null || entry.Status == "Cancelled") return false;
        var values = AssignmentValues(("timetableCode", entry.TimetableCode), ("courseId", entry.CourseId.ToString()), ("teacherId", entry.TeacherId.ToString()), ("classroomId", entry.ClassroomId.ToString()), ("yearLevel", entry.YearLevel.ToString()), ("dayOfWeek", entry.DayOfWeek.ToString()), ("startsAt", entry.StartsAt.ToString("HH:mm")), ("endsAt", entry.EndsAt.ToString("HH:mm")), ("status", entry.Status));
        entry.Status = "Cancelled"; entry.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(Audit(id, "Timetable", entry.TimetableCode, "Assignment removed", values));
        return true;
    }

    private async Task<IReadOnlyList<EnrollmentItemDto>> StudentsAsync(string? search, Guid? departmentId, int? year, Period period, CancellationToken ct)
    {
        var rows = await db.Students.AsNoTracking()
            .Where(student => student.Status != "Inactive")
            .GroupJoin(db.StudentEnrollments.AsNoTracking().Where(x => x.AcademicYear == period.AcademicYear && x.Semester == period.Semester && x.Status != "Removed"), student => student.Id, enrollment => enrollment.StudentId, (student, enrollments) => new { student, enrollment = enrollments.FirstOrDefault() })
            .Select(x => new { x.student, x.enrollment, department = x.enrollment == null ? null : x.enrollment.Department })
            .ToListAsync(ct);
        return rows.Where(x => (!departmentId.HasValue || x.enrollment?.DepartmentId == departmentId) && (!year.HasValue || x.enrollment?.YearLevel == year) && Matches(search, x.student.StudentCode, x.student.FullName, x.department?.Name))
            .Select(x => Item(x.student.Id, ("studentCode", x.student.StudentCode), ("name", x.student.FullName), ("email", x.student.Email), ("photoDataUrl", x.student.PhotoDataUrl), ("departmentId", x.enrollment?.DepartmentId.ToString() ?? ""), ("department", x.department?.Name ?? "Unassigned"), ("year", x.enrollment?.YearLevel.ToString() ?? ""), ("shift", x.enrollment?.Shift ?? ""), ("status", x.enrollment?.Status ?? "Unassigned"), ("academicYear", period.AcademicYear), ("semester", period.Semester)))
            .ToList();
    }

    private async Task<IReadOnlyList<EnrollmentItemDto>> TeachersAsync(string? search, Guid? departmentId, int? year, Period period, CancellationToken ct)
    {
        var rows = await db.Teachers.AsNoTracking()
            .Where(teacher => teacher.Status != "Inactive")
            .GroupJoin(db.TeacherAssignments.AsNoTracking().Where(x => x.AcademicYear == period.AcademicYear && x.Semester == period.Semester && x.Status != "Removed"), teacher => teacher.Id, assignment => assignment.TeacherId, (teacher, assignments) => new { teacher, assignment = assignments.FirstOrDefault() })
            .Select(x => new { x.teacher, x.assignment, department = x.assignment == null ? null : x.assignment.Department })
            .ToListAsync(ct);
        var schedules = await db.ScheduleEntries.AsNoTracking().Include(x => x.Course).Where(x => x.Status != "Cancelled").ToListAsync(ct);
        return rows.Where(x => (!departmentId.HasValue || x.assignment?.DepartmentId == departmentId) && (!year.HasValue || schedules.Any(s => s.TeacherId == x.teacher.Id && s.YearLevel == year)) && Matches(search, x.teacher.TeacherCode, x.teacher.FullName, x.department?.Name))
            .Select(x =>
            {
                var teacherSchedule = schedules.Where(s => s.TeacherId == x.teacher.Id).ToList();
                return Item(x.teacher.Id, ("teacherCode", x.teacher.TeacherCode), ("name", x.teacher.FullName), ("email", x.teacher.Email), ("photoDataUrl", x.teacher.PhotoDataUrl), ("departmentId", x.assignment?.DepartmentId.ToString() ?? ""), ("department", x.department?.Name ?? "Unassigned"), ("status", x.assignment?.Status ?? "Unassigned"), ("courseCount", teacherSchedule.Select(s => s.CourseId).Distinct().Count().ToString()), ("courses", string.Join(", ", teacherSchedule.Select(s => s.Course?.Name).Where(name => name is not null).Distinct())), ("yearLevels", string.Join(", ", teacherSchedule.Select(s => s.YearLevel).Distinct().Order().Select(value => $"Year {value}"))), ("weeklyClasses", teacherSchedule.Count.ToString()), ("learningSpaces", string.Join(", ", teacherSchedule.Select(s => s.ClassroomId).Distinct().Count())), ("academicYear", period.AcademicYear), ("semester", period.Semester));
            }).ToList();
    }

    private async Task<IReadOnlyList<EnrollmentItemDto>> CoursesAsync(string? search, Guid? departmentId, int? year, Period period, CancellationToken ct)
    {
        var rows = await db.Courses.AsNoTracking().Where(course => course.IsActive)
            .GroupJoin(db.CourseAssignments.AsNoTracking().Where(x => x.AcademicYear == period.AcademicYear && x.Semester == period.Semester && x.Status != "Removed"), course => course.Id, assignment => assignment.CourseId, (course, assignments) => new { course, assignment = assignments.FirstOrDefault() })
            .Select(x => new { x.course, x.assignment, department = x.assignment == null ? null : x.assignment.Department, teacher = x.assignment == null ? null : x.assignment.Teacher })
            .ToListAsync(ct);
        return rows.Where(x => x.assignment is not null && (!departmentId.HasValue || x.assignment.DepartmentId == departmentId) && (!year.HasValue || x.assignment.YearLevel == year) && Matches(search, x.course.CourseCode, x.course.Name, x.department?.Name, x.teacher?.FullName))
            .Select(x => Item(x.course.Id, ("courseCode", x.course.CourseCode), ("name", x.course.Name), ("departmentId", x.assignment?.DepartmentId.ToString() ?? ""), ("department", x.department?.Name ?? "Unassigned"), ("teacherId", x.assignment?.TeacherId.ToString() ?? ""), ("teacher", x.teacher?.FullName ?? "Unassigned"), ("year", x.assignment?.YearLevel.ToString() ?? ""), ("capacity", x.assignment?.Capacity.ToString() ?? ""), ("status", x.assignment?.Status ?? "Unassigned"), ("academicYear", period.AcademicYear), ("semester", period.Semester)))
            .ToList();
    }

    private async Task<IReadOnlyList<EnrollmentItemDto>> ClassroomsAsync(string? search, Guid? departmentId, int? year, Period period, CancellationToken ct)
    {
        var rows = await db.Classrooms.AsNoTracking().Where(room => room.Status != "Inactive")
            .GroupJoin(db.ClassroomAssignments.AsNoTracking().Where(x => x.AcademicYear == period.AcademicYear && x.Semester == period.Semester && x.Status != "Removed"), room => room.Id, assignment => assignment.ClassroomId, (room, assignments) => new { room, assignment = assignments.FirstOrDefault() })
            .Select(x => new { x.room, x.assignment, department = x.assignment == null ? null : x.assignment.Department })
            .ToListAsync(ct);
        var schedules = await db.ScheduleEntries.AsNoTracking().Include(x => x.Course).Include(x => x.Teacher).Where(x => x.Status != "Cancelled").ToListAsync(ct);
        return rows.Where(x => (!departmentId.HasValue || x.assignment?.DepartmentId == departmentId || x.assignment?.DepartmentId == null) && (!year.HasValue || schedules.Any(s => s.ClassroomId == x.room.Id && s.YearLevel == year)) && Matches(search, x.room.ClassroomCode, x.room.Building, x.department?.Name))
            .Select(x =>
            {
                var roomSchedule = schedules.Where(s => s.ClassroomId == x.room.Id && (!year.HasValue || s.YearLevel == year)).ToList();
                return Item(x.room.Id, ("classroomCode", x.room.ClassroomCode), ("building", x.room.Building), ("roomType", x.room.RoomType), ("departmentId", x.assignment?.DepartmentId.ToString() ?? ""), ("department", x.department?.Name ?? "Shared institute"), ("capacity", x.assignment?.Capacity.ToString() ?? x.room.Capacity.ToString()), ("access", x.assignment?.Access ?? "Shared institute"), ("status", x.assignment?.Status ?? "Unassigned"), ("courses", string.Join(", ", roomSchedule.Select(s => s.Course?.Name).Where(name => name is not null).Distinct())), ("teachers", string.Join(", ", roomSchedule.Select(s => s.Teacher?.FullName).Where(name => name is not null).Distinct())), ("yearLevels", string.Join(", ", roomSchedule.Select(s => s.YearLevel).Distinct().Order().Select(value => $"Year {value}"))), ("weeklyClasses", roomSchedule.Count.ToString()), ("academicYear", period.AcademicYear), ("semester", period.Semester));
            }).ToList();
    }

    private async Task<IReadOnlyList<EnrollmentItemDto>> TimetableAsync(string? search, Guid? departmentId, int? year, Period period, CancellationToken ct)
    {
        var assignments = await db.CourseAssignments.AsNoTracking().Include(x => x.Department)
            .Where(x => x.AcademicYear == period.AcademicYear && x.Semester == period.Semester && x.Status == "Active")
            .ToDictionaryAsync(x => x.CourseId, ct);
        var entries = await db.ScheduleEntries.AsNoTracking().Include(x => x.Course).Include(x => x.Teacher).Include(x => x.Classroom)
            .Where(x => x.Status != "Cancelled").ToListAsync(ct);
        return entries.Where(entry => assignments.TryGetValue(entry.CourseId, out var assignment)
                && (!departmentId.HasValue || assignment.DepartmentId == departmentId)
                && (!year.HasValue || entry.YearLevel == year)
                && Matches(search, entry.TimetableCode, entry.Course?.Name, entry.Teacher?.FullName, entry.Classroom?.ClassroomCode, assignment.Department?.Name))
            .Select(entry =>
            {
                var assignment = assignments[entry.CourseId];
                return Item(entry.Id, ("timetableCode", entry.TimetableCode), ("courseId", entry.CourseId.ToString()), ("course", entry.Course?.Name ?? "Unassigned"), ("teacherId", entry.TeacherId.ToString()), ("teacher", entry.Teacher?.FullName ?? "Unassigned"), ("classroomId", entry.ClassroomId.ToString()), ("classroom", entry.Classroom?.ClassroomCode ?? "Unassigned"), ("classroomType", entry.Classroom?.RoomType ?? "Classroom"), ("departmentId", assignment.DepartmentId.ToString()), ("department", assignment.Department?.Name ?? "Unassigned"), ("yearLevel", entry.YearLevel.ToString()), ("dayOfWeek", entry.DayOfWeek.ToString()), ("startsAt", entry.StartsAt.ToString("HH:mm")), ("endsAt", entry.EndsAt.ToString("HH:mm")), ("status", entry.Status), ("createAt", entry.CreateAt.ToString("yyyy-MM-dd")));
            }).ToList();
    }

    private async Task<IReadOnlyList<EnrollmentItemDto>> DepartmentsAsync(string? search, Guid? departmentId, int? year, Period period, CancellationToken ct)
    {
        var departments = await db.Departments.AsNoTracking().Where(x => x.IsActive && (!departmentId.HasValue || x.Id == departmentId)).ToListAsync(ct);
        var students = await db.StudentEnrollments.AsNoTracking().Where(x => x.AcademicYear == period.AcademicYear && x.Semester == period.Semester && x.Status == "Active" && (!year.HasValue || x.YearLevel == year)).ToListAsync(ct);
        var teachers = await db.TeacherAssignments.AsNoTracking().Where(x => x.AcademicYear == period.AcademicYear && x.Semester == period.Semester && x.Status == "Assigned").ToListAsync(ct);
        var courses = await db.CourseAssignments.AsNoTracking().Where(x => x.AcademicYear == period.AcademicYear && x.Semester == period.Semester && x.Status == "Active" && (!year.HasValue || x.YearLevel == year)).ToListAsync(ct);
        var courseIds = courses.Select(x => x.CourseId).ToHashSet();
        var classes = await db.ScheduleEntries.AsNoTracking().Where(x => x.Status != "Cancelled" && courseIds.Contains(x.CourseId) && (!year.HasValue || x.YearLevel == year)).ToListAsync(ct);
        return departments.Where(department => Matches(search, department.DepartmentCode, department.Name))
            .Select(department =>
            {
                var departmentCourseIds = courses.Where(course => course.DepartmentId == department.Id).Select(course => course.CourseId).ToHashSet();
                var departmentClasses = classes.Where(entry => departmentCourseIds.Contains(entry.CourseId)).ToList();
                return Item(department.Id, ("departmentCode", department.DepartmentCode), ("name", department.Name), ("year", year?.ToString() ?? "All"), ("students", students.Count(x => x.DepartmentId == department.Id).ToString()), ("teachers", teachers.Count(teacher => teacher.DepartmentId == department.Id && departmentClasses.Any(entry => entry.TeacherId == teacher.TeacherId)).ToString()), ("courses", departmentCourseIds.Count.ToString()), ("classrooms", departmentClasses.Select(entry => entry.ClassroomId).Distinct().Count().ToString()), ("weeklyClasses", departmentClasses.Count.ToString()), ("status", "Active"), ("academicYear", period.AcademicYear), ("semester", period.Semester));
            }).ToList();
    }

    private async Task<EnrollmentItemDto> UpdateStudentAsync(Guid id, Dictionary<string, string> values, Period period, CancellationToken ct)
    {
        var student = await db.Students.FindAsync([id], ct) ?? throw new KeyNotFoundException("Student not found.");
        var departmentId = await RequiredDepartmentAsync(values, ct);
        var year = Integer(values, "year", 1, 4);
        var shift = Choice(values, "shift", AcademicTimetablePolicy.ShiftNames);
        var enrollment = await db.StudentEnrollments.FirstOrDefaultAsync(x => x.StudentId == id && x.AcademicYear == period.AcademicYear && x.Semester == period.Semester, ct);
        if (enrollment is null) { enrollment = new StudentEnrollment { StudentId = id, AcademicYear = period.AcademicYear, Semester = period.Semester }; db.StudentEnrollments.Add(enrollment); }
        if (enrollment.DepartmentId != departmentId || enrollment.YearLevel != year || enrollment.Shift != shift) await ReassignStudentRecordsAsync(student, departmentId, year, shift, period, ct);
        enrollment.DepartmentId = departmentId; enrollment.YearLevel = year; enrollment.Shift = shift; enrollment.Status = Choice(values, "status", ["Active", "Paused", "Completed"], "Active"); enrollment.UpdatedAtUtc = DateTime.UtcNow;
        student.DepartmentId = departmentId; student.YearLevel = year; student.Shift = shift;
        db.AuditLogs.Add(Audit(id, "Student", student.StudentCode, "Enrollment updated", values));
        return Item(id, ("studentCode", student.StudentCode), ("name", student.FullName), ("departmentId", departmentId.ToString()), ("year", year.ToString()), ("shift", shift), ("status", enrollment.Status));
    }

    private async Task<EnrollmentItemDto> UpdateTeacherAsync(Guid id, Dictionary<string, string> values, Period period, CancellationToken ct)
    {
        var teacher = await db.Teachers.FindAsync([id], ct) ?? throw new KeyNotFoundException("Teacher not found.");
        var departmentId = await OptionalDepartmentAsync(values, ct);
        var assignment = await db.TeacherAssignments.FirstOrDefaultAsync(x => x.TeacherId == id && x.AcademicYear == period.AcademicYear && x.Semester == period.Semester, ct);
        if (assignment is null) { assignment = new TeacherAssignment { TeacherId = id, AcademicYear = period.AcademicYear, Semester = period.Semester }; db.TeacherAssignments.Add(assignment); }
        if (assignment.DepartmentId != departmentId)
        {
            var conflict = await db.CourseAssignments.AnyAsync(x => x.TeacherId == id && x.AcademicYear == period.AcademicYear && x.Semester == period.Semester && x.Status == "Active" && (!departmentId.HasValue || x.DepartmentId != departmentId), ct);
            var headsOther = await db.Departments.AnyAsync(x => x.HeadTeacherId == id && (!departmentId.HasValue || x.Id != departmentId), ct);
            if (conflict || headsOther) throw new InvalidOperationException("Reassign this teacher's active course and department-head assignments first.");
        }
        assignment.DepartmentId = departmentId; assignment.Status = Choice(values, "status", ["Assigned", "On leave", "Unassigned"], departmentId.HasValue ? "Assigned" : "Unassigned"); assignment.UpdatedAtUtc = DateTime.UtcNow;
        teacher.DepartmentId = departmentId;
        db.AuditLogs.Add(Audit(id, "Teacher", teacher.TeacherCode, "Assignment updated", values));
        return Item(id, ("teacherCode", teacher.TeacherCode), ("name", teacher.FullName), ("departmentId", departmentId?.ToString() ?? ""), ("status", assignment.Status));
    }

    private async Task<EnrollmentItemDto> UpdateCourseAsync(Guid id, Dictionary<string, string> values, Period period, CancellationToken ct)
    {
        var course = await db.Courses.FindAsync([id], ct) ?? throw new KeyNotFoundException("Course not found.");
        var departmentId = await RequiredDepartmentAsync(values, ct);
        var teacherId = GuidValue(values, "teacherId", true)!.Value;
        var teacherAssignment = await db.TeacherAssignments.AsNoTracking().FirstOrDefaultAsync(x => x.TeacherId == teacherId && x.AcademicYear == period.AcademicYear && x.Semester == period.Semester && x.Status == "Assigned", ct) ?? throw new InvalidOperationException("Assign this teacher in Teacher enrollment first.");
        if (teacherAssignment.DepartmentId.HasValue && teacherAssignment.DepartmentId != departmentId) throw new InvalidOperationException("Course and teacher must belong to the same enrollment department.");
        var year = Integer(values, "year", 1, 4); var capacity = Integer(values, "capacity", 1, 10000);
        var assignment = await db.CourseAssignments.FirstOrDefaultAsync(x => x.CourseId == id && x.AcademicYear == period.AcademicYear && x.Semester == period.Semester, ct);
        if (assignment is null) { assignment = new CourseAssignment { CourseId = id, AcademicYear = period.AcademicYear, Semester = period.Semester }; db.CourseAssignments.Add(assignment); }
        assignment.DepartmentId = departmentId; assignment.TeacherId = teacherId; assignment.YearLevel = year; assignment.Capacity = capacity; assignment.Status = Choice(values, "status", ["Active", "Paused"], "Active"); assignment.UpdatedAtUtc = DateTime.UtcNow;
        course.DepartmentId = departmentId; course.TeacherId = teacherId; course.Capacity = capacity;
        db.AuditLogs.Add(Audit(id, "Course", course.CourseCode, "Assignment updated", values));
        return Item(id, ("courseCode", course.CourseCode), ("name", course.Name), ("departmentId", departmentId.ToString()), ("teacherId", teacherId.ToString()), ("year", year.ToString()), ("capacity", capacity.ToString()), ("status", assignment.Status));
    }

    private async Task<EnrollmentItemDto> UpdateClassroomAsync(Guid id, Dictionary<string, string> values, Period period, CancellationToken ct)
    {
        var room = await db.Classrooms.FindAsync([id], ct) ?? throw new KeyNotFoundException("Classroom not found.");
        var departmentId = await OptionalDepartmentAsync(values, ct); var capacity = Integer(values, "capacity", 1, 10000);
        var assignment = await db.ClassroomAssignments.FirstOrDefaultAsync(x => x.ClassroomId == id && x.AcademicYear == period.AcademicYear && x.Semester == period.Semester, ct);
        if (assignment is null) { assignment = new ClassroomAssignment { ClassroomId = id, AcademicYear = period.AcademicYear, Semester = period.Semester }; db.ClassroomAssignments.Add(assignment); }
        assignment.DepartmentId = departmentId; assignment.Capacity = capacity; assignment.Access = Choice(values, "access", ["Shared institute", "Department only"], "Shared institute"); assignment.Status = Choice(values, "status", ["Available", "Reserved", "Unavailable"], "Available"); assignment.UpdatedAtUtc = DateTime.UtcNow;
        room.DepartmentId = departmentId; room.Capacity = capacity;
        db.AuditLogs.Add(Audit(id, "Classroom", room.ClassroomCode, "Assignment updated", values));
        return Item(id, ("classroomCode", room.ClassroomCode), ("building", room.Building), ("departmentId", departmentId?.ToString() ?? ""), ("capacity", capacity.ToString()), ("access", assignment.Access), ("status", assignment.Status));
    }

    private async Task<EnrollmentItemDto> UpdateTimetableAsync(Guid id, Dictionary<string, string> values, Period period, CancellationToken ct)
    {
        var entry = await db.ScheduleEntries.FindAsync([id], ct) ?? throw new KeyNotFoundException("Timetable entry not found.");
        var code = Required(values, "timetableCode");
        if (await db.ScheduleEntries.AnyAsync(x => x.Id != id && x.TimetableCode == code, ct)) throw new ArgumentException("TimetableCode already exists.");
        var courseId = GuidValue(values, "courseId", true)!.Value; var teacherId = GuidValue(values, "teacherId", true)!.Value; var classroomId = GuidValue(values, "classroomId", true)!.Value; var year = Integer(values, "yearLevel", 1, 4);
        var course = await db.CourseAssignments.AsNoTracking().Include(x => x.Course).FirstOrDefaultAsync(x => x.CourseId == courseId && x.AcademicYear == period.AcademicYear && x.Semester == period.Semester && x.Status == "Active", ct) ?? throw new InvalidOperationException("Assign this course in Course enrollment first.");
        if (course.YearLevel != year || course.TeacherId != teacherId) throw new InvalidOperationException("Timetable year and teacher must match the current course assignment.");
        var teacher = await db.TeacherAssignments.AsNoTracking().FirstOrDefaultAsync(x => x.TeacherId == teacherId && x.AcademicYear == period.AcademicYear && x.Semester == period.Semester && x.Status == "Assigned", ct) ?? throw new InvalidOperationException("Assign this teacher in Teacher enrollment first.");
        if (teacher.DepartmentId.HasValue && teacher.DepartmentId != course.DepartmentId) throw new InvalidOperationException("Teacher and course must belong to the same enrollment department.");
        var classroom = await db.ClassroomAssignments.AsNoTracking().Include(x => x.Classroom).FirstOrDefaultAsync(x => x.ClassroomId == classroomId && x.AcademicYear == period.AcademicYear && x.Semester == period.Semester && x.Status != "Unavailable" && x.Status != "Removed", ct) ?? throw new InvalidOperationException("Assign this classroom in Classroom enrollment first.");
        if (classroom.DepartmentId.HasValue && classroom.DepartmentId != course.DepartmentId) throw new InvalidOperationException("This classroom is assigned to another department.");
        if (classroom.Capacity < course.Capacity) throw new InvalidOperationException("Classroom assignment capacity must cover the course capacity.");
        if (year == 1 && classroom.Classroom?.ClassroomCode != "501") throw new InvalidOperationException("Year 1 must use Classroom 501.");
        if (year >= 2 && classroom.Classroom?.ClassroomCode == "501") throw new InvalidOperationException("Classroom 501 is reserved for Year 1.");
        var day = Enum.TryParse<DayOfWeek>(Required(values, "dayOfWeek"), true, out var parsedDay) ? parsedDay : throw new ArgumentException("dayOfWeek is invalid.");
        var startsAt = TimeOnly.TryParse(Required(values, "startsAt"), out var parsedStart) ? parsedStart : throw new ArgumentException("startsAt is invalid.");
        var endsAt = TimeOnly.TryParse(Required(values, "endsAt"), out var parsedEnd) ? parsedEnd : throw new ArgumentException("endsAt is invalid.");
        if (endsAt <= startsAt || AcademicTimetablePolicy.Find(day, startsAt, endsAt) is null) throw new ArgumentException("Select a configured teaching period.");
        if (await db.ScheduleEntries.AnyAsync(x => x.Id != id && x.Status != "Cancelled" && x.DayOfWeek == day && x.StartsAt < endsAt && startsAt < x.EndsAt && (x.TeacherId == teacherId || x.ClassroomId == classroomId), ct)) throw new InvalidOperationException("Teacher or classroom is already scheduled during this time.");
        entry.TimetableCode = code; entry.CourseId = courseId; entry.TeacherId = teacherId; entry.ClassroomId = classroomId; entry.YearLevel = year; entry.DayOfWeek = day; entry.StartsAt = startsAt; entry.EndsAt = endsAt; entry.Status = Choice(values, "status", ["Upcoming", "Running", "Completed", "Cancelled"], "Upcoming"); entry.UpdatedAtUtc = DateTime.UtcNow;
        db.AuditLogs.Add(Audit(id, "Timetable", code, "Assignment updated", values));
        return Item(id, ("timetableCode", code), ("courseId", courseId.ToString()), ("teacherId", teacherId.ToString()), ("classroomId", classroomId.ToString()), ("departmentId", course.DepartmentId.ToString()), ("yearLevel", year.ToString()), ("dayOfWeek", day.ToString()), ("startsAt", startsAt.ToString("HH:mm")), ("endsAt", endsAt.ToString("HH:mm")), ("status", entry.Status));
    }

    private async Task ReassignStudentRecordsAsync(Student student, Guid departmentId, int year, string shift, Period period, CancellationToken ct)
    {
        var courseIds = await db.CourseAssignments.AsNoTracking().Where(x => x.DepartmentId == departmentId && x.YearLevel == year && x.AcademicYear == period.AcademicYear && x.Semester == period.Semester && x.Status == "Active").Select(x => x.CourseId).ToListAsync(ct);
        var courseId = await db.ScheduleEntries.AsNoTracking().Where(x => courseIds.Contains(x.CourseId) && x.Status != "Cancelled").OrderBy(x => x.TimetableCode).Select(x => (Guid?)x.CourseId).FirstOrDefaultAsync(ct)
            ?? courseIds.FirstOrDefault();
        if (courseId == Guid.Empty) throw new InvalidOperationException("Assign a course for the selected department and year before saving this student enrollment.");
        foreach (var grade in await db.GradeRecords.Where(x => x.StudentId == student.Id && x.AcademicYear == period.AcademicYear && x.Term == period.Semester).ToListAsync(ct)) { grade.CourseId = courseId; grade.Score = 0; grade.LetterGrade = "F"; grade.UpdatedAtUtc = DateTime.UtcNow; }
        var startsAt = AcademicTimetablePolicy.FindShift(shift)?.StartsAt ?? throw new ArgumentException("Shift is invalid.");
        foreach (var attendance in await db.AttendanceRecords.Where(x => x.StudentId == student.Id && x.AcademicYear == period.AcademicYear && x.Term == period.Semester).ToListAsync(ct)) { attendance.CheckedInAt = startsAt; attendance.UpdatedAtUtc = DateTime.UtcNow; }
    }

    private async Task<Period> CurrentPeriodAsync(CancellationToken ct)
    {
        var values = await db.SystemSettings.AsNoTracking().Where(x => (x.Section == "academic-year" && x.Key == "currentYear") || (x.Section == "semester" && x.Key == "currentTerm")).ToDictionaryAsync(x => $"{x.Section}:{x.Key}", x => x.Value, ct);
        return new(values.GetValueOrDefault("academic-year:currentYear", "2026–2027"), values.GetValueOrDefault("semester:currentTerm", "Semester 1"));
    }

    private async Task<Guid> RequiredDepartmentAsync(Dictionary<string, string> values, CancellationToken ct) => await OptionalDepartmentAsync(values, ct) ?? throw new ArgumentException("Department is required.");
    private async Task<Guid?> OptionalDepartmentAsync(Dictionary<string, string> values, CancellationToken ct) { var id = GuidValue(values, "departmentId", false); if (!id.HasValue) return null; if (!await db.Departments.AnyAsync(x => x.Id == id && x.IsActive, ct)) throw new ArgumentException("Department must reference an active department."); return id; }
    private static Guid? GuidValue(IReadOnlyDictionary<string, string> values, string key, bool required) { var raw = values.GetValueOrDefault(key); if (string.IsNullOrWhiteSpace(raw)) { if (required) throw new ArgumentException($"{key} is required."); return null; } return Guid.TryParse(raw, out var value) ? value : throw new ArgumentException($"{key} is invalid."); }
    private static string Required(IReadOnlyDictionary<string, string> values, string key) => !string.IsNullOrWhiteSpace(values.GetValueOrDefault(key)) ? values[key].Trim() : throw new ArgumentException($"{key} is required.");
    private static int Integer(IReadOnlyDictionary<string, string> values, string key, int minimum, int maximum) => int.TryParse(values.GetValueOrDefault(key), out var value) && value >= minimum && value <= maximum ? value : throw new ArgumentException($"{key} must be between {minimum} and {maximum}.");
    private static string Choice(IReadOnlyDictionary<string, string> values, string key, IEnumerable<string> choices, string? fallback = null) { var value = values.GetValueOrDefault(key, fallback ?? ""); return choices.Contains(value, StringComparer.OrdinalIgnoreCase) ? choices.First(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)) : throw new ArgumentException($"{key} is invalid."); }
    private static bool Matches(string? search, params string?[] values) => string.IsNullOrWhiteSpace(search) || values.Any(value => value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);
    private static Dictionary<string, string> AssignmentValues(params (string Key, string Value)[] values) => values.ToDictionary(x => x.Key, x => x.Value);
    private static EnrollmentItemDto Item(Guid id, params (string Key, string Value)[] values) => new(id, values.ToDictionary(x => x.Key, x => x.Value));
    private static AuditLog Audit(Guid id, string type, string subject, string action, IReadOnlyDictionary<string, string> values) => new() { ResourceId = id, Type = type, Subject = subject, Action = action, Details = JsonSerializer.Serialize(values) };
    private sealed record Period(string AcademicYear, string Semester);
}
