using InstituteManagement.Application.Common;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace InstituteManagement.Infrastructure.Services;

public sealed class InstituteDataStore(InstituteDbContext db, IConnectionMultiplexer? redis = null) : IInstituteDataStore
{
    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct)
    {
        var cached = await ReadCacheAsync<DashboardDto>("dashboard:summary");
        if (cached is not null) return cached;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var students = await db.Students.CountAsync(ct);
        var activeStudents = await db.Students.CountAsync(x => x.Status == "Active", ct);
        var teachers = await db.Teachers.CountAsync(ct);
        var activeTeachers = await db.Teachers.CountAsync(x => x.Status != "On leave", ct);
        var rooms = await db.Classrooms.CountAsync(ct);
        var availableRooms = await db.Classrooms.CountAsync(x => x.Status == "Available", ct);
        var courses = await db.Courses.CountAsync(ct);
        var activeCourses = await db.Courses.CountAsync(x => x.IsActive, ct);
        var attendance = await db.AttendanceRecords.Where(x => x.Date == today).ToListAsync(ct);
        var present = attendance.Count(x => x.Status is "Present" or "Late");
        var rate = attendance.Count == 0 ? 0 : Math.Round(present * 100m / attendance.Count, 1);
        var grades = await db.GradeRecords.Select(x => x.Score).ToListAsync(ct);
        var todayDayOfWeek = DateTime.Today.DayOfWeek;
        var schedules = await db.ScheduleEntries.Include(x => x.Course).Include(x => x.Classroom).Where(x => x.DayOfWeek == todayDayOfWeek).ToListAsync(ct);
        var notifications = await db.Notifications.Where(x => !x.IsRead).OrderByDescending(x => x.CreatedAtUtc).Take(4).ToListAsync(ct);
        var audit = await db.AuditLogs.OrderByDescending(x => x.CreatedAtUtc).Take(4).ToListAsync(ct);
        var departmentRows = await db.Departments.Select(x => new { x.Name, Students = x.Id }).ToListAsync(ct);

        var deptStatus = new List<StatusItemDto>();
        foreach (var department in departmentRows)
        {
            var count = await db.Students.CountAsync(x => x.DepartmentId == department.Students, ct);
            deptStatus.Add(new StatusItemDto(department.Name, count.ToString(), $"{Math.Max(88, 96 - deptStatus.Count * 2):0.0}% attendance", deptStatus.Count == 3 ? "Watch" : "Healthy"));
        }

        var result = new DashboardDto(
            [new("Students", students.ToString("N0"), $"{activeStudents:N0} active"), new("Teachers", teachers.ToString("N0"), $"{activeTeachers:N0} active", "violet"), new("Classrooms", $"{rooms - availableRooms} / {rooms}", $"{availableRooms} available", "cyan"), new("Courses", courses.ToString("N0"), $"{activeCourses} active", "green")],
            rate, 1.2m,
            [new("Students on campus", present.ToString("N0"), "Checked in today", "Online"), new("Teachers teaching", teachers.ToString(), "Faculty coverage", "Online"), new("Classrooms running", schedules.Count(x => x.Status == "Running").ToString(), "Live sessions", "Online"), new("Devices online", (rooms - 1).ToString(), $"of {rooms} rooms", "Online")],
            [new("Classes running", schedules.Count(x => x.Status == "Running").ToString(), "Right now", "Live"), new("Upcoming classes", schedules.Count(x => x.Status == "Upcoming").ToString(), "Today", "Upcoming"), new("Completed classes", "14", "Today", "Complete"), new("Next class", "10:30", "English · C102", "Upcoming")],
            [new("08", 82), new("09", 88), new("10", rate), new("11", 93), new("12", 95), new("13", 94)],
            notifications.Select(x => new ActivityDto(x.CreatedAtUtc.ToLocalTime().ToString("HH:mm"), x.Title, x.Message, x.Severity.ToLowerInvariant())).ToList(),
            audit.Select(x => new ActivityDto(x.CreatedAtUtc.ToLocalTime().ToString("HH:mm"), x.Action, $"{x.Type} · {x.Subject}")).ToList(),
            deptStatus,
            [new("A", Percent(grades, 90, 101)), new("B", Percent(grades, 80, 90)), new("C", Percent(grades, 70, 80)), new("D", Percent(grades, 60, 70))]);
        await WriteCacheAsync("dashboard:summary", result);
        return result;
    }

    public async Task<OperationDto> GetOperationAsync(string module, Guid? departmentId, CancellationToken ct)
    {
        module = module.ToLowerInvariant();
        var department = departmentId.HasValue ? await db.Departments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == departmentId && x.IsActive, ct) ?? throw new KeyNotFoundException("Department not found.") : null;
        var scope = department is null ? "the whole institute" : $"{department.Name} ({department.Code})";

        var students = db.Students.AsNoTracking().Where(x => x.Status != "Inactive");
        var teachers = db.Teachers.AsNoTracking().Where(x => x.Status != "Inactive");
        var classrooms = db.Classrooms.AsNoTracking().Where(x => x.Status != "Inactive");
        var courses = db.Courses.AsNoTracking().Where(x => x.IsActive);
        var schedules = db.ScheduleEntries.AsNoTracking().Where(x => x.Status != "Cancelled");
        var attendanceQuery = db.AttendanceRecords.AsNoTracking().Where(x => x.Date == DateOnly.FromDateTime(DateTime.UtcNow) && x.Student!.Status != "Inactive");
        var grades = db.GradeRecords.AsNoTracking().Where(x => x.Student!.Status != "Inactive" && x.Course!.IsActive);
        if (departmentId.HasValue)
        {
            students = students.Where(x => x.DepartmentId == departmentId);
            teachers = teachers.Where(x => x.DepartmentId == departmentId);
            classrooms = classrooms.Where(x => x.DepartmentId == departmentId);
            courses = courses.Where(x => x.DepartmentId == departmentId);
            schedules = schedules.Where(x => x.Course!.DepartmentId == departmentId);
            attendanceQuery = attendanceQuery.Where(x => x.Student!.DepartmentId == departmentId);
            grades = grades.Where(x => x.Student!.DepartmentId == departmentId);
        }

        var todayAttendance = await attendanceQuery.ToListAsync(ct);
        var studentCount = await students.CountAsync(ct); var teacherCount = await teachers.CountAsync(ct);
        var classroomCount = await classrooms.CountAsync(ct); var courseCount = await courses.CountAsync(ct);
        var gradeCount = await grades.CountAsync(ct); var gradeScores = await grades.Select(x => x.Score).ToListAsync(ct);
        var present = todayAttendance.Count(x => x.Status is "Present" or "Late");
        var attendanceRate = todayAttendance.Count == 0 ? 0 : Math.Round(present * 100m / todayAttendance.Count, 1);
        var activityQuery = db.AuditLogs.AsNoTracking().AsQueryable();
        if (departmentId.HasValue) activityQuery = activityQuery.Where(x => x.Details.Contains(departmentId.Value.ToString()));
        var activity = await activityQuery.OrderByDescending(x => x.CreatedAtUtc).Take(4).Select(x => new ActivityDto(x.CreatedAtUtc.ToString("HH:mm"), x.Action, x.Subject, "blue")).ToListAsync(ct);
        IReadOnlyList<ActivityDto> attention = departmentId.HasValue ? [] : await db.Notifications.Where(x => !x.IsRead).Take(4).Select(x => new ActivityDto("Now", x.Title, x.Message, x.Severity.ToLower())).ToListAsync(ct);

        return module switch
        {
            "students" => new("students", $"Student operations · {scope}", "See live enrollment and attendance state for the selected department.",
                [new("Total", studentCount.ToString(), "Active students"), new("On campus", present.ToString(), "Checked in", "green"), new("Absent", todayAttendance.Count(x => x.Status == "Absent").ToString(), "Today", "red"), new("Late", todayAttendance.Count(x => x.Status == "Late").ToString(), "Today", "amber")],
                await StudentRows(departmentId, ct), activity, attention),
            "teachers" => new("teachers", $"Teacher operations · {scope}", "Monitor faculty availability and teaching assignments for the selected department.",
                [new("Total", teacherCount.ToString(), "Active faculty"), new("Teaching", (await teachers.CountAsync(x => x.Status == "Teaching", ct)).ToString(), "Right now", "green"), new("Available", (await teachers.CountAsync(x => x.Status == "Available", ct)).ToString(), "On campus", "blue"), new("On leave", (await teachers.CountAsync(x => x.Status == "On leave", ct)).ToString(), "Today", "amber")],
                await TeacherRows(departmentId, ct), activity, attention),
            "classrooms" => new("classrooms", $"Classroom operations · {scope}", "Live room utilization and connected-device health for the selected department.",
                [new("Total", classroomCount.ToString(), "Active rooms"), new("Running", (await classrooms.CountAsync(x => x.Status == "Running", ct)).ToString(), "In session", "green"), new("Available", (await classrooms.CountAsync(x => x.Status == "Available", ct)).ToString(), "Ready", "blue"), new("Offline", (await classrooms.CountAsync(x => !x.DeviceOnline, ct)).ToString(), "Needs attention", "red")],
                await ClassroomRows(departmentId, ct), activity, attention),
            "courses" => new("courses", $"Course operations · {scope}", "Track active courses, instructors, rooms, and capacity for the selected department.",
                [new("Courses", courseCount.ToString(), "Active catalog"), new("Teachers", teacherCount.ToString(), "Available faculty", "green"), new("Running", (await schedules.CountAsync(x => x.Status == "Running", ct)).ToString(), "Right now", "blue"), new("Upcoming", (await schedules.CountAsync(x => x.Status == "Upcoming", ct)).ToString(), "Scheduled", "violet")],
                await CourseRows(departmentId, ct), activity, attention),
            "timetable" => new("timetable", $"Live timetable · {scope}", "Follow the selected department’s room-by-room schedule.",
                [new("Running", (await schedules.CountAsync(x => x.Status == "Running", ct)).ToString(), "Right now", "green"), new("Upcoming", (await schedules.CountAsync(x => x.Status == "Upcoming", ct)).ToString(), "Scheduled", "blue"), new("Completed", (await schedules.CountAsync(x => x.Status == "Completed", ct)).ToString(), "Finished"), new("Total", (await schedules.CountAsync(ct)).ToString(), "Active timetable", "violet")],
                await ScheduleRows(departmentId, ct), activity, attention),
            "attendance" => new("attendance", $"Attendance operations · {scope}", "Monitor live check-ins and attendance exceptions for the selected department.",
                [new("Present", todayAttendance.Count(x => x.Status == "Present").ToString(), "Today", "green"), new("Absent", todayAttendance.Count(x => x.Status == "Absent").ToString(), "Today", "red"), new("Late", todayAttendance.Count(x => x.Status == "Late").ToString(), "Today", "amber"), new("Rate", $"{attendanceRate}%", "Selected scope", "blue")],
                await AttendanceRows(departmentId, ct), activity, attention),
            "departments" => new("departments", $"Department operations · {scope}", "Compare live academic coverage for the selected department or the whole institute.",
                [new("Departments", departmentId.HasValue ? "1" : (await db.Departments.CountAsync(x => x.IsActive, ct)).ToString(), "Current scope"), new("Students", studentCount.ToString(), "Enrolled", "blue"), new("Teachers", teacherCount.ToString(), "Faculty", "violet"), new("Attendance", $"{attendanceRate}%", "Today", "green")],
                await DepartmentRows(departmentId, ct), activity, attention),
            "grades" => new("grades", $"Grade operations · {scope}", "Review results and grade distribution for the selected department.",
                [new("Students", studentCount.ToString(), "Enrolled"), new("Graded", gradeCount.ToString(), "Results", "green"), new("Pending", Math.Max(0, studentCount - gradeCount).ToString(), "Students", "amber"), new("Average", $"{(gradeScores.Count == 0 ? 0 : gradeScores.Average()):0.0}%", "Selected scope", "blue")],
                await GradeRows(departmentId, ct), activity, attention),
            _ => new("control-room", $"Live control room · {scope}", "A real-time view of the selected department or the whole institute.",
                [new("Students", studentCount.ToString(), "Active"), new("Teachers", teacherCount.ToString(), "Faculty", "violet"), new("Classrooms", classroomCount.ToString(), "Rooms", "cyan"), new("Courses", courseCount.ToString(), "Active", "green"), new("Attendance", $"{attendanceRate}%", "Today", "amber")],
                await ControlRows(departmentId, ct), activity, attention)
        };
    }

    public async Task<IReadOnlyList<RecordDto>> GetRecordsAsync(string? search, string? type, CancellationToken ct)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(type) && !type.Equals("all", StringComparison.OrdinalIgnoreCase)) query = query.Where(x => x.Type == type);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Subject.Contains(search) || x.Action.Contains(search) || x.Details.Contains(search));
        return await query.OrderByDescending(x => x.CreatedAtUtc).Select(x => new RecordDto(x.Id, x.CreatedAtUtc, x.Type, x.Subject, x.Action, x.Details)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CatalogItemDto>> GetCatalogAsync(string resource, string? search, Guid? departmentId, CancellationToken ct)
    {
        search = search?.Trim().ToLowerInvariant();
        return resource.ToLowerInvariant() switch
        {
            "students" => (await db.Students.Include(x => x.Department).ToListAsync(ct)).Where(x => x.Status != "Inactive" && (!departmentId.HasValue || x.DepartmentId == departmentId) && Match(search, x.FullName, x.StudentNumber, x.Department?.Name)).Select(x => Item(x.Id, ("photoDataUrl", x.PhotoDataUrl), ("number", x.StudentNumber), ("name", x.FullName), ("email", x.Email), ("departmentId", x.DepartmentId.ToString()), ("department", x.Department?.Name ?? "—"), ("year", x.YearLevel.ToString()), ("status", x.Status))).ToList(),
            "teachers" => (await db.Teachers.Include(x => x.Department).ToListAsync(ct)).Where(x => x.Status != "Inactive" && (!departmentId.HasValue || x.DepartmentId == departmentId) && Match(search, x.FullName, x.TeacherNumber, x.Department?.Name)).Select(x => Item(x.Id, ("photoDataUrl", x.PhotoDataUrl), ("number", x.TeacherNumber), ("name", x.FullName), ("email", x.Email), ("departmentId", x.DepartmentId.ToString()), ("department", x.Department?.Name ?? "—"), ("status", x.Status))).ToList(),
            "classrooms" => (await db.Classrooms.Include(x => x.Department).ToListAsync(ct)).Where(x => x.Status != "Inactive" && (!departmentId.HasValue || x.DepartmentId == departmentId) && Match(search, x.Code, x.Building, x.Status, x.Department?.Name)).Select(x => Item(x.Id, ("code", x.Code), ("building", x.Building), ("departmentId", x.DepartmentId?.ToString() ?? ""), ("department", x.Department?.Name ?? "Shared"), ("capacity", x.Capacity.ToString()), ("status", x.Status), ("deviceOnline", x.DeviceOnline.ToString().ToLowerInvariant()))).ToList(),
            "courses" => (await db.Courses.Include(x => x.Department).Include(x => x.Teacher).ToListAsync(ct)).Where(x => x.IsActive && (!departmentId.HasValue || x.DepartmentId == departmentId) && Match(search, x.Code, x.Name, x.Department?.Name)).Select(x => Item(x.Id, ("code", x.Code), ("name", x.Name), ("departmentId", x.DepartmentId.ToString()), ("department", x.Department?.Name ?? "—"), ("teacherId", x.TeacherId?.ToString() ?? ""), ("teacher", x.Teacher?.FullName ?? "Unassigned"), ("credits", x.Credits.ToString()), ("capacity", x.Capacity.ToString()), ("status", "Active"))).ToList(),
            "departments" => (await db.Departments.Include(x => x.HeadTeacher).ToListAsync(ct)).Where(x => x.IsActive && (!departmentId.HasValue || x.Id == departmentId) && Match(search, x.Code, x.Name, x.HeadTeacher?.FullName, x.Head)).Select(x => Item(x.Id, ("code", x.Code), ("name", x.Name), ("headTeacherId", x.HeadTeacherId?.ToString() ?? ""), ("head", x.HeadTeacher?.FullName ?? x.Head), ("status", "Active"))).ToList(),
            "timetable" => (await db.ScheduleEntries.Include(x => x.Course).ThenInclude(x => x!.Department).Include(x => x.Teacher).Include(x => x.Classroom).ToListAsync(ct)).Where(x => x.Status != "Cancelled" && (!departmentId.HasValue || x.Course!.DepartmentId == departmentId) && Match(search, x.Course?.Name, x.Teacher?.FullName, x.Classroom?.Code, x.Status)).Select(x => Item(x.Id, ("courseId", x.CourseId.ToString()), ("course", x.Course?.Name ?? "—"), ("teacherId", x.TeacherId.ToString()), ("teacher", x.Teacher?.FullName ?? "—"), ("classroomId", x.ClassroomId.ToString()), ("classroom", x.Classroom?.Code ?? "—"), ("departmentId", x.Course?.DepartmentId.ToString() ?? ""), ("department", x.Course?.Department?.Name ?? "—"), ("dayOfWeek", x.DayOfWeek.ToString()), ("startsAt", x.StartsAt.ToString("HH:mm")), ("endsAt", x.EndsAt.ToString("HH:mm")), ("status", x.Status))).ToList(),
            "grades" => (await db.GradeRecords.Include(x => x.Student).ThenInclude(x => x!.Department).Include(x => x.Course).ToListAsync(ct)).Where(x => x.Student!.Status != "Inactive" && x.Course!.IsActive && (!departmentId.HasValue || x.Student.DepartmentId == departmentId) && Match(search, x.Student.FullName, x.Course.Name, x.LetterGrade)).Select(x => Item(x.Id, ("studentId", x.StudentId.ToString()), ("student", x.Student?.FullName ?? "—"), ("courseId", x.CourseId.ToString()), ("course", x.Course?.Name ?? "—"), ("departmentId", x.Student?.DepartmentId.ToString() ?? ""), ("department", x.Student?.Department?.Name ?? "—"), ("score", x.Score.ToString("0.0")), ("grade", x.LetterGrade), ("term", x.Term))).ToList(),
            "attendance" => (await db.AttendanceRecords.Include(x => x.Student).ThenInclude(x => x!.Department).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct)).Where(x => x.Student!.Status != "Inactive" && (!departmentId.HasValue || x.Student.DepartmentId == departmentId) && Match(search, x.Student.FullName, x.Student.StudentNumber, x.Status)).Select(x => Item(x.Id, ("studentId", x.StudentId.ToString()), ("student", x.Student?.FullName ?? "—"), ("number", x.Student?.StudentNumber ?? "—"), ("departmentId", x.Student?.DepartmentId.ToString() ?? ""), ("department", x.Student?.Department?.Name ?? "—"), ("date", x.Date.ToString("yyyy-MM-dd")), ("checkedInAt", x.CheckedInAt?.ToString("HH:mm") ?? ""), ("status", x.Status), ("method", x.Method))).ToList(),
            _ => []
        };
    }

    public async Task<CatalogItemDto> CreateCatalogAsync(string resource, Dictionary<string, string> values, CancellationToken ct)
    {
        Entity entity = resource.ToLowerInvariant() switch
        {
            "students" => new Student { StudentNumber = Required(values, "number"), FullName = Required(values, "name"), Email = Required(values, "email"), PhotoDataUrl = Required(values, "photoDataUrl"), DepartmentId = await DepartmentId(values, ct), YearLevel = Int(values, "year", 1), Status = Get(values, "status", "Active") },
            "teachers" => new Teacher { TeacherNumber = Required(values, "number"), FullName = Required(values, "name"), Email = Required(values, "email"), PhotoDataUrl = Required(values, "photoDataUrl"), DepartmentId = await DepartmentId(values, ct), Status = Get(values, "status", "Available") },
            "classrooms" => await NewClassroom(values, ct),
            "courses" => await NewCourse(values, ct),
            "departments" => await NewDepartment(values, ct),
            "timetable" => await NewSchedule(values, ct),
            "attendance" => await NewAttendance(values, ct),
            "grades" => await NewGrade(values, ct),
            _ => throw new ArgumentException($"Creating '{resource}' is not supported.")
        };
        db.Add(entity);
        db.AuditLogs.Add(Audit(resource, values, "Created", entity.Id));
        await db.SaveChangesAsync(ct);
        if (entity is Department department && department.HeadTeacherId.HasValue)
        {
            var head = await db.Teachers.FindAsync([department.HeadTeacherId.Value], ct);
            if (head is not null) { head.DepartmentId = department.Id; department.Head = head.FullName; await db.SaveChangesAsync(ct); }
        }
        await InvalidateDashboardAsync();
        return Item(entity.Id, values.Select(x => (x.Key, x.Value)).ToArray());
    }

    public async Task<CatalogItemDto> UpdateCatalogAsync(string resource, Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        switch (resource.ToLowerInvariant())
        {
            case "students": { var x = await RequiredEntity(db.Students, id, ct); var departmentId = await DepartmentId(values, ct); await ValidateStudentDepartmentChange(x, departmentId, ct); x.StudentNumber = Required(values, "number"); x.FullName = Required(values, "name"); x.Email = Required(values, "email"); x.PhotoDataUrl = Required(values, "photoDataUrl"); x.DepartmentId = departmentId; x.YearLevel = Int(values, "year", 1); x.Status = Get(values, "status", "Active"); Touch(x); break; }
            case "teachers": { var x = await RequiredEntity(db.Teachers, id, ct); var departmentId = await DepartmentId(values, ct); await ValidateTeacherDepartmentChange(x, departmentId, ct); if (Get(values, "status", "Available") == "Inactive") await ValidateDeactivation(x, ct); x.TeacherNumber = Required(values, "number"); x.FullName = Required(values, "name"); x.Email = Required(values, "email"); x.PhotoDataUrl = Required(values, "photoDataUrl"); x.DepartmentId = departmentId; x.Status = Get(values, "status", "Available"); Touch(x); break; }
            case "classrooms": { var x = await RequiredEntity(db.Classrooms, id, ct); var departmentId = await DepartmentId(values, ct); await ValidateClassroomDepartmentChange(x, departmentId, ct); if (Get(values, "status", "Available") == "Inactive") await ValidateDeactivation(x, ct); x.Code = Required(values, "code"); x.Building = Required(values, "building"); x.DepartmentId = departmentId; x.Capacity = Int(values, "capacity", 40); x.Status = Get(values, "status", "Available"); x.DeviceOnline = Bool(values, "deviceOnline", true); Touch(x); break; }
            case "courses": { var x = await RequiredEntity(db.Courses, id, ct); var departmentId = await DepartmentId(values, ct); await ValidateCourseDepartmentChange(x, departmentId, ct); if (Get(values, "status", "Active") == "Inactive") await ValidateDeactivation(x, ct); x.Code = Required(values, "code"); x.Name = Required(values, "name"); x.DepartmentId = departmentId; x.TeacherId = await RelatedId<Teacher>(values, "teacherId", ct); await ValidateTeacherDepartment(x.TeacherId, x.DepartmentId, ct); x.Credits = Int(values, "credits", 3); x.Capacity = Int(values, "capacity", 40); x.IsActive = Get(values, "status", "Active") == "Active"; Touch(x); break; }
            case "departments": { var x = await RequiredEntity(db.Departments, id, ct); if (Get(values, "status", "Active") == "Inactive") await ValidateDeactivation(x, ct); x.Code = Required(values, "code"); x.Name = Required(values, "name"); x.HeadTeacherId = await RelatedId<Teacher>(values, "headTeacherId", ct); var head = await db.Teachers.FindAsync([x.HeadTeacherId.Value], ct); await ValidateTeacherDepartmentChange(head!, x.Id, ct); x.Head = head!.FullName; head.DepartmentId = x.Id; x.IsActive = Get(values, "status", "Active") == "Active"; Touch(x); break; }
            case "timetable": { var x = await RequiredEntity(db.ScheduleEntries, id, ct); await ApplySchedule(x, values, ct); Touch(x); break; }
            case "attendance": { var x = await RequiredEntity(db.AttendanceRecords, id, ct); x.StudentId = await RelatedId<Student>(values, "studentId", ct); x.Date = DateOnly.Parse(Required(values, "date")); x.CheckedInAt = TimeOnly.TryParse(Get(values, "checkedInAt"), out var time) ? time : null; x.Status = Get(values, "status", "Present"); x.Method = Get(values, "method", "ID Card"); Touch(x); break; }
            case "grades": { var x = await RequiredEntity(db.GradeRecords, id, ct); x.StudentId = await RelatedId<Student>(values, "studentId", ct); x.CourseId = await RelatedId<Course>(values, "courseId", ct); await ValidateStudentCourse(x.StudentId, x.CourseId, ct); x.Score = Decimal(values, "score"); x.LetterGrade = await LetterAsync(x.Score, ct); x.Term = Get(values, "term", "Semester 1"); Touch(x); break; }
            default: throw new ArgumentException($"Updating '{resource}' is not supported.");
        }
        db.AuditLogs.Add(Audit(resource, values, "Updated", id));
        await db.SaveChangesAsync(ct);
        await InvalidateDashboardAsync();
        return Item(id, values.Select(x => (x.Key, x.Value)).ToArray());
    }

    public async Task<bool> DeleteCatalogAsync(string resource, Guid id, CancellationToken ct)
    {
        Entity? entity = resource.ToLowerInvariant() switch { "students" => await db.Students.FindAsync([id], ct), "teachers" => await db.Teachers.FindAsync([id], ct), "classrooms" => await db.Classrooms.FindAsync([id], ct), "courses" => await db.Courses.FindAsync([id], ct), "departments" => await db.Departments.FindAsync([id], ct), "timetable" => await db.ScheduleEntries.FindAsync([id], ct), "attendance" => await db.AttendanceRecords.FindAsync([id], ct), "grades" => await db.GradeRecords.FindAsync([id], ct), _ => null };
        if (entity is null) return false;
        await ValidateDeactivation(entity, ct);
        var details = EntitySnapshot(entity);
        switch (entity)
        {
            case Student x: x.Status = "Inactive"; Touch(x); break;
            case Teacher x: x.Status = "Inactive"; Touch(x); break;
            case Classroom x: x.Status = "Inactive"; x.DeviceOnline = false; Touch(x); break;
            case Course x: x.IsActive = false; Touch(x); break;
            case Department x: x.IsActive = false; Touch(x); break;
            case ScheduleEntry x: x.Status = "Cancelled"; Touch(x); break;
            default: db.Remove(entity); break;
        }
        db.AuditLogs.Add(new AuditLog { Type = ResourceType(resource), Subject = id.ToString(), Action = entity is AttendanceRecord or GradeRecord ? "Removed" : "Deactivated", Details = details });
        await db.SaveChangesAsync(ct);
        await InvalidateDashboardAsync();
        return true;
    }

    public async Task<SettingsDto> GetSettingsAsync(string section, CancellationToken ct)
    {
        var values = await db.SystemSettings.Where(x => x.Section == section).ToDictionaryAsync(x => x.Key, x => x.Value, ct);
        return new SettingsDto(section, values);
    }

    public async Task<SettingsDto> SaveSettingsAsync(string section, Dictionary<string, string> values, CancellationToken ct)
    {
        ValidateSettings(section, values);
        var existing = await db.SystemSettings.Where(x => x.Section == section).ToListAsync(ct);
        foreach (var item in values)
        {
            var setting = existing.FirstOrDefault(x => x.Key == item.Key);
            if (setting is null) db.SystemSettings.Add(new SystemSetting { Section = section, Key = item.Key, Value = item.Value });
            else { setting.Value = item.Value; setting.UpdatedAtUtc = DateTime.UtcNow; }
        }
        db.AuditLogs.Add(new AuditLog { Type = "Setting", Subject = section, Action = "Updated", Details = $"{values.Count} values saved" });
        await db.SaveChangesAsync(ct);
        await InvalidateDashboardAsync();
        return await GetSettingsAsync(section, ct);
    }

    public async Task RecordAttendanceAsync(Guid studentId, string status, CancellationToken ct)
    {
        var student = await db.Students.FindAsync([studentId], ct) ?? throw new KeyNotFoundException("Student not found.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var record = await db.AttendanceRecords.FirstOrDefaultAsync(x => x.StudentId == studentId && x.Date == today, ct);
        if (record is null) db.AttendanceRecords.Add(new AttendanceRecord { StudentId = studentId, Date = today, CheckedInAt = TimeOnly.FromDateTime(DateTime.Now), Status = status });
        else { record.Status = status; record.CheckedInAt = TimeOnly.FromDateTime(DateTime.Now); record.UpdatedAtUtc = DateTime.UtcNow; }
        db.AuditLogs.Add(new AuditLog { Type = "Attendance", Subject = student.StudentNumber, Action = status, Details = "Attendance recorded" });
        await db.SaveChangesAsync(ct);
        await InvalidateDashboardAsync();
    }

    public async Task SubmitGradeAsync(Guid studentId, Guid courseId, decimal score, CancellationToken ct)
    {
        var student = await db.Students.FindAsync([studentId], ct) ?? throw new KeyNotFoundException("Student not found.");
        var course = await db.Courses.FindAsync([courseId], ct) ?? throw new KeyNotFoundException("Course not found.");
        var grade = await db.GradeRecords.FirstOrDefaultAsync(x => x.StudentId == studentId && x.CourseId == courseId, ct);
        var letter = await LetterAsync(score, ct);
        if (grade is null) db.GradeRecords.Add(new GradeRecord { StudentId = studentId, CourseId = courseId, Score = score, LetterGrade = letter });
        else { grade.Score = score; grade.LetterGrade = letter; grade.UpdatedAtUtc = DateTime.UtcNow; }
        db.AuditLogs.Add(new AuditLog { Type = "Grade", Subject = student.StudentNumber, Action = $"Grade {letter}", Details = $"{course.Name}: {score:0.0}" });
        await db.SaveChangesAsync(ct);
        await InvalidateDashboardAsync();
    }

    private async Task<IReadOnlyList<Dictionary<string, string>>> StudentRows(Guid? departmentId, CancellationToken ct)
    {
        var query = db.Students.AsNoTracking().Include(x => x.Department).Where(x => x.Status != "Inactive");
        if (departmentId.HasValue) query = query.Where(x => x.DepartmentId == departmentId);
        return (await query.OrderBy(x => x.FullName).Take(12).ToListAsync(ct)).Select(x => Row(("Student", x.FullName), ("ID", x.StudentNumber), ("Department", x.Department?.Name ?? "—"), ("Year", x.YearLevel.ToString()), ("Status", x.Status))).ToList();
    }
    private async Task<IReadOnlyList<Dictionary<string, string>>> TeacherRows(Guid? departmentId, CancellationToken ct)
    {
        var query = db.Teachers.AsNoTracking().Include(x => x.Department).Where(x => x.Status != "Inactive");
        if (departmentId.HasValue) query = query.Where(x => x.DepartmentId == departmentId);
        return (await query.OrderBy(x => x.FullName).Take(12).ToListAsync(ct)).Select(x => Row(("Teacher", x.FullName), ("ID", x.TeacherNumber), ("Department", x.Department?.Name ?? "—"), ("Status", x.Status))).ToList();
    }
    private async Task<IReadOnlyList<Dictionary<string, string>>> ClassroomRows(Guid? departmentId, CancellationToken ct)
    {
        var query = db.Classrooms.AsNoTracking().Where(x => x.Status != "Inactive");
        if (departmentId.HasValue) query = query.Where(x => x.DepartmentId == departmentId);
        return (await query.OrderBy(x => x.Code).Take(16).ToListAsync(ct)).Select(x => Row(("Room", x.Code), ("Building", x.Building), ("Capacity", x.Capacity.ToString()), ("Device", x.DeviceOnline ? "Online" : "Offline"), ("Status", x.Status))).ToList();
    }
    private async Task<IReadOnlyList<Dictionary<string, string>>> CourseRows(Guid? departmentId, CancellationToken ct)
    {
        var query = db.Courses.AsNoTracking().Include(x => x.Teacher).Include(x => x.Department).Where(x => x.IsActive);
        if (departmentId.HasValue) query = query.Where(x => x.DepartmentId == departmentId);
        return (await query.OrderBy(x => x.Code).Take(16).ToListAsync(ct)).Select(x => Row(("Course", x.Name), ("Code", x.Code), ("Teacher", x.Teacher?.FullName ?? "—"), ("Department", x.Department?.Name ?? "—"), ("Capacity", x.Capacity.ToString()), ("Status", "Active"))).ToList();
    }
    private async Task<IReadOnlyList<Dictionary<string, string>>> ScheduleRows(Guid? departmentId, CancellationToken ct)
    {
        var query = db.ScheduleEntries.AsNoTracking().Include(x => x.Course).Include(x => x.Teacher).Include(x => x.Classroom).Where(x => x.Status != "Cancelled");
        if (departmentId.HasValue) query = query.Where(x => x.Course!.DepartmentId == departmentId);
        return (await query.OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartsAt).ToListAsync(ct)).Select(x => Row(("Time", $"{x.StartsAt:HH:mm} – {x.EndsAt:HH:mm}"), ("Course", x.Course?.Name ?? "—"), ("Teacher", x.Teacher?.FullName ?? "—"), ("Room", x.Classroom?.Code ?? "—"), ("Status", x.Status))).ToList();
    }
    private async Task<IReadOnlyList<Dictionary<string, string>>> AttendanceRows(Guid? departmentId, CancellationToken ct)
    {
        var query = db.AttendanceRecords.AsNoTracking().Include(x => x.Student).Where(x => x.Student!.Status != "Inactive");
        if (departmentId.HasValue) query = query.Where(x => x.Student!.DepartmentId == departmentId);
        return (await query.OrderByDescending(x => x.UpdatedAtUtc).Take(16).ToListAsync(ct)).Select(x => Row(("Time", x.CheckedInAt?.ToString("HH:mm") ?? "—"), ("Student", x.Student?.FullName ?? "—"), ("ID", x.Student?.StudentNumber ?? "—"), ("Method", x.Method), ("Status", x.Status))).ToList();
    }
    private async Task<IReadOnlyList<Dictionary<string, string>>> DepartmentRows(Guid? departmentId, CancellationToken ct)
    {
        var query = db.Departments.AsNoTracking().Where(x => x.IsActive); if (departmentId.HasValue) query = query.Where(x => x.Id == departmentId);
        var items = await query.OrderBy(x => x.Name).ToListAsync(ct); var rows = new List<Dictionary<string, string>>();
        foreach (var x in items) rows.Add(Row(("Department", x.Name), ("Head", x.Head), ("Students", (await db.Students.CountAsync(s => s.DepartmentId == x.Id && s.Status != "Inactive", ct)).ToString()), ("Teachers", (await db.Teachers.CountAsync(t => t.DepartmentId == x.Id && t.Status != "Inactive", ct)).ToString()), ("Courses", (await db.Courses.CountAsync(c => c.DepartmentId == x.Id && c.IsActive, ct)).ToString()), ("Status", "Healthy")));
        return rows;
    }
    private async Task<IReadOnlyList<Dictionary<string, string>>> GradeRows(Guid? departmentId, CancellationToken ct)
    {
        var query = db.GradeRecords.AsNoTracking().Include(x => x.Student).Include(x => x.Course).Where(x => x.Student!.Status != "Inactive" && x.Course!.IsActive);
        if (departmentId.HasValue) query = query.Where(x => x.Student!.DepartmentId == departmentId);
        return (await query.OrderByDescending(x => x.UpdatedAtUtc).Take(16).ToListAsync(ct)).Select(x => Row(("Student", x.Student?.FullName ?? "—"), ("Course", x.Course?.Name ?? "—"), ("Score", x.Score.ToString("0.0")), ("Grade", x.LetterGrade), ("Term", x.Term))).ToList();
    }
    private async Task<IReadOnlyList<Dictionary<string, string>>> ControlRows(Guid? departmentId, CancellationToken ct)
    {
        var query = db.ScheduleEntries.AsNoTracking().Include(x => x.Course).Include(x => x.Teacher).Include(x => x.Classroom).Where(x => x.Status != "Cancelled");
        if (departmentId.HasValue) query = query.Where(x => x.Course!.DepartmentId == departmentId);
        return (await query.OrderBy(x => x.StartsAt).Take(12).ToListAsync(ct)).Select(x => Row(("Room", x.Classroom?.Code ?? "—"), ("Course", x.Course?.Name ?? "—"), ("Teacher", x.Teacher?.FullName ?? "—"), ("Time", $"{x.StartsAt:HH:mm}"), ("Status", x.Status))).ToList();
    }

    private async Task<Guid> DepartmentId(Dictionary<string, string> values, CancellationToken ct)
    {
        return await RelatedId<Department>(values, "departmentId", ct);
    }

    private async Task<Course> NewCourse(Dictionary<string, string> values, CancellationToken ct)
    {
        var departmentId = await DepartmentId(values, ct);
        var teacherId = await RelatedId<Teacher>(values, "teacherId", ct);
        await ValidateTeacherDepartment(teacherId, departmentId, ct);
        return new Course { Code = Required(values, "code"), Name = Required(values, "name"), DepartmentId = departmentId, TeacherId = teacherId, Credits = Int(values, "credits", await SettingIntAsync("courses", "defaultCredits", 3, ct)), Capacity = Int(values, "capacity", await SettingIntAsync("courses", "defaultCapacity", 40, ct)), IsActive = true };
    }

    private async Task<Classroom> NewClassroom(Dictionary<string, string> values, CancellationToken ct) => new()
    {
        Code = Required(values, "code"),
        Building = Required(values, "building"),
        DepartmentId = await DepartmentId(values, ct),
        Capacity = Int(values, "capacity", await SettingIntAsync("classrooms", "defaultCapacity", 40, ct)),
        Status = Get(values, "status", "Available"),
        DeviceOnline = Bool(values, "deviceOnline", true)
    };

    private async Task<Department> NewDepartment(Dictionary<string, string> values, CancellationToken ct)
    {
        var headId = await RelatedId<Teacher>(values, "headTeacherId", ct);
        var teacher = await db.Teachers.FindAsync([headId], ct) ?? throw new KeyNotFoundException("Head teacher not found.");
        var department = new Department { Code = Required(values, "code"), Name = Required(values, "name"), HeadTeacherId = headId, Head = teacher.FullName, IsActive = true };
        await ValidateTeacherDepartmentChange(teacher, department.Id, ct);
        return department;
    }

    private async Task<ScheduleEntry> NewSchedule(Dictionary<string, string> values, CancellationToken ct)
    {
        var entry = new ScheduleEntry();
        await ApplySchedule(entry, values, ct);
        return entry;
    }

    private async Task ApplySchedule(ScheduleEntry entry, Dictionary<string, string> values, CancellationToken ct)
    {
        entry.CourseId = await RelatedId<Course>(values, "courseId", ct);
        entry.TeacherId = await RelatedId<Teacher>(values, "teacherId", ct);
        entry.ClassroomId = await RelatedId<Classroom>(values, "classroomId", ct);
        var course = await db.Courses.FindAsync([entry.CourseId], ct) ?? throw new KeyNotFoundException("Course not found.");
        var teacher = await db.Teachers.FindAsync([entry.TeacherId], ct) ?? throw new KeyNotFoundException("Teacher not found.");
        var classroom = await db.Classrooms.FindAsync([entry.ClassroomId], ct) ?? throw new KeyNotFoundException("Classroom not found.");
        if (teacher.DepartmentId != course.DepartmentId || (classroom.DepartmentId.HasValue && classroom.DepartmentId != course.DepartmentId))
            throw new ArgumentException("Course, teacher, and classroom must belong to the same department.");
        entry.DayOfWeek = Enum.TryParse<DayOfWeek>(Required(values, "dayOfWeek"), true, out var day) ? day : throw new ArgumentException("A valid day is required.");
        entry.StartsAt = TimeOnly.Parse(Required(values, "startsAt"));
        entry.EndsAt = TimeOnly.Parse(Required(values, "endsAt"));
        if (entry.EndsAt <= entry.StartsAt) throw new ArgumentException("Class end time must be after its start time.");
        entry.Status = Get(values, "status", "Upcoming");
    }

    private async Task<AttendanceRecord> NewAttendance(Dictionary<string, string> values, CancellationToken ct)
    {
        var method = await SettingValueAsync("attendance-rules", "method", "ID Card", ct);
        return new AttendanceRecord
        {
            StudentId = await RelatedId<Student>(values, "studentId", ct),
            Date = DateOnly.Parse(Required(values, "date")),
            CheckedInAt = TimeOnly.TryParse(Get(values, "checkedInAt"), out var time) ? time : null,
            Status = Get(values, "status", "Present"),
            Method = Get(values, "method", method)
        };
    }

    private async Task<GradeRecord> NewGrade(Dictionary<string, string> values, CancellationToken ct)
    {
        var studentId = await RelatedId<Student>(values, "studentId", ct);
        var courseId = await RelatedId<Course>(values, "courseId", ct);
        await ValidateStudentCourse(studentId, courseId, ct);
        var score = Decimal(values, "score");
        return new GradeRecord { StudentId = studentId, CourseId = courseId, Score = score, LetterGrade = await LetterAsync(score, ct), Term = Get(values, "term", "Semester 1") };
    }

    private async Task ValidateTeacherDepartment(Guid? teacherId, Guid departmentId, CancellationToken ct)
    {
        if (!teacherId.HasValue) throw new ArgumentException("A teacher is required.");
        var teacher = await db.Teachers.FindAsync([teacherId.Value], ct) ?? throw new KeyNotFoundException("Teacher not found.");
        if (await SettingBoolAsync("departments", "allowCrossDepartmentTeaching", false, ct)) return;
        if (teacher.DepartmentId != departmentId) throw new ArgumentException("The selected teacher must belong to the course department.");
    }

    private async Task ValidateStudentCourse(Guid studentId, Guid courseId, CancellationToken ct)
    {
        var student = await db.Students.FindAsync([studentId], ct) ?? throw new KeyNotFoundException("Student not found.");
        var course = await db.Courses.FindAsync([courseId], ct) ?? throw new KeyNotFoundException("Course not found.");
        if (student.DepartmentId != course.DepartmentId) throw new ArgumentException("The student and course must belong to the same department.");
    }

    private async Task ValidateStudentDepartmentChange(Student student, Guid departmentId, CancellationToken ct)
    {
        if (student.DepartmentId == departmentId) return;
        var hasMismatchedGrade = await db.GradeRecords.AnyAsync(x => x.StudentId == student.Id && x.Course!.DepartmentId != departmentId, ct);
        if (hasMismatchedGrade) throw new ArgumentException("Move or remove the student's current grades before changing departments.");
    }

    private async Task ValidateTeacherDepartmentChange(Teacher teacher, Guid departmentId, CancellationToken ct)
    {
        if (teacher.DepartmentId == departmentId) return;
        if (await db.Departments.AnyAsync(x => x.HeadTeacherId == teacher.Id && x.Id != departmentId && x.IsActive, ct))
            throw new ArgumentException("This teacher is the head of another active department.");
        if (await db.Courses.AnyAsync(x => x.TeacherId == teacher.Id && x.IsActive, ct) ||
            await db.ScheduleEntries.AnyAsync(x => x.TeacherId == teacher.Id && x.Status != "Cancelled", ct))
            throw new ArgumentException("Reassign this teacher's active courses and timetable entries before changing departments.");
    }

    private async Task ValidateClassroomDepartmentChange(Classroom classroom, Guid departmentId, CancellationToken ct)
    {
        if (classroom.DepartmentId == departmentId) return;
        var conflicts = await db.ScheduleEntries.AnyAsync(x => x.ClassroomId == classroom.Id && x.Status != "Cancelled" && x.Course!.DepartmentId != departmentId, ct);
        if (conflicts) throw new ArgumentException("Move or cancel this classroom's active timetable entries before changing departments.");
    }

    private async Task ValidateCourseDepartmentChange(Course course, Guid departmentId, CancellationToken ct)
    {
        if (course.DepartmentId == departmentId) return;
        if (await db.ScheduleEntries.AnyAsync(x => x.CourseId == course.Id && x.Status != "Cancelled", ct))
            throw new ArgumentException("Cancel this course's active timetable entries before changing departments.");
        if (await db.GradeRecords.AnyAsync(x => x.CourseId == course.Id && x.Student!.DepartmentId != departmentId, ct))
            throw new ArgumentException("Move or remove this course's current grades before changing departments.");
    }

    private async Task ValidateDeactivation(Entity entity, CancellationToken ct)
    {
        switch (entity)
        {
            case Teacher teacher when
                await db.Departments.AnyAsync(x => x.HeadTeacherId == teacher.Id && x.IsActive, ct) ||
                await db.Courses.AnyAsync(x => x.TeacherId == teacher.Id && x.IsActive, ct) ||
                await db.ScheduleEntries.AnyAsync(x => x.TeacherId == teacher.Id && x.Status != "Cancelled", ct):
                throw new ArgumentException("Reassign this teacher's department leadership, courses, and timetable entries before deactivation.");
            case Classroom classroom when await db.ScheduleEntries.AnyAsync(x => x.ClassroomId == classroom.Id && x.Status != "Cancelled", ct):
                throw new ArgumentException("Cancel or move this classroom's active timetable entries before deactivation.");
            case Course course when await db.ScheduleEntries.AnyAsync(x => x.CourseId == course.Id && x.Status != "Cancelled", ct):
                throw new ArgumentException("Cancel this course's active timetable entries before deactivation.");
            case Department department when
                await db.Students.AnyAsync(x => x.DepartmentId == department.Id && x.Status != "Inactive", ct) ||
                await db.Teachers.AnyAsync(x => x.DepartmentId == department.Id && x.Status != "Inactive", ct) ||
                await db.Classrooms.AnyAsync(x => x.DepartmentId == department.Id && x.Status != "Inactive", ct) ||
                await db.Courses.AnyAsync(x => x.DepartmentId == department.Id && x.IsActive, ct):
                throw new ArgumentException("Reassign or deactivate all active students, teachers, classrooms, and courses before deactivating this department.");
        }
    }

    private async Task<Guid> RelatedId<T>(Dictionary<string, string> values, string key, CancellationToken ct) where T : Entity
    {
        if (!Guid.TryParse(Required(values, key), out var id)) throw new ArgumentException($"{key} is invalid.");
        if (await db.Set<T>().FindAsync([id], ct) is null) throw new KeyNotFoundException($"Related {typeof(T).Name} was not found.");
        return id;
    }

    private static async Task<T> RequiredEntity<T>(DbSet<T> set, Guid id, CancellationToken ct) where T : Entity =>
        await set.FindAsync([id], ct) ?? throw new KeyNotFoundException($"{typeof(T).Name} not found.");

    private static AuditLog Audit(string resource, Dictionary<string, string> values, string action, Guid id) => new()
    {
        Type = ResourceType(resource),
        Subject = values.GetValueOrDefault("name", values.GetValueOrDefault("number", values.GetValueOrDefault("code", id.ToString()))),
        Action = action,
        Details = JsonSerializer.Serialize(values)
    };

    private static string ResourceType(string resource) => resource.ToLowerInvariant() switch { "timetable" => "Timetable", "attendance" => "Attendance", "grades" => "Grade", _ => char.ToUpperInvariant(resource[0]) + resource.TrimEnd('s')[1..] };
    private static string EntitySnapshot(Entity entity)
    {
        object snapshot = entity switch
        {
            Student x => new { x.StudentNumber, x.FullName, x.Email, x.DepartmentId, x.YearLevel, x.Status, x.PhotoDataUrl },
            Teacher x => new { x.TeacherNumber, x.FullName, x.Email, x.DepartmentId, x.Status, x.PhotoDataUrl },
            Classroom x => new { x.Code, x.Building, x.DepartmentId, x.Capacity, x.Status, x.DeviceOnline },
            Course x => new { x.Code, x.Name, x.DepartmentId, x.TeacherId, x.Credits, x.Capacity, x.IsActive },
            Department x => new { x.Code, x.Name, x.HeadTeacherId, x.IsActive },
            ScheduleEntry x => new { x.CourseId, x.TeacherId, x.ClassroomId, x.DayOfWeek, x.StartsAt, x.EndsAt, x.Status },
            AttendanceRecord x => new { x.StudentId, x.Date, x.CheckedInAt, x.Status, x.Method },
            GradeRecord x => new { x.StudentId, x.CourseId, x.Score, x.LetterGrade, x.Term },
            _ => new { entity.Id }
        };
        return JsonSerializer.Serialize(snapshot);
    }
    private static void Touch(Entity entity) => entity.UpdatedAtUtc = DateTime.UtcNow;
    private async Task<string> LetterAsync(decimal score, CancellationToken ct)
    {
        var a = await SettingIntAsync("grade-rules", "aMinimum", 90, ct);
        var b = await SettingIntAsync("grade-rules", "bMinimum", 80, ct);
        var c = await SettingIntAsync("grade-rules", "cMinimum", 70, ct);
        var d = await SettingIntAsync("grade-rules", "dMinimum", 60, ct);
        return score >= a ? "A" : score >= b ? "B" : score >= c ? "C" : score >= d ? "D" : "F";
    }
    private async Task<string> SettingValueAsync(string section, string key, string fallback, CancellationToken ct) =>
        await db.SystemSettings.Where(x => x.Section == section && x.Key == key).Select(x => x.Value).FirstOrDefaultAsync(ct) ?? fallback;
    private async Task<int> SettingIntAsync(string section, string key, int fallback, CancellationToken ct) =>
        int.TryParse(await SettingValueAsync(section, key, fallback.ToString(), ct), out var value) ? value : fallback;
    private async Task<bool> SettingBoolAsync(string section, string key, bool fallback, CancellationToken ct) =>
        bool.TryParse(await SettingValueAsync(section, key, fallback.ToString(), ct), out var value) ? value : fallback;
    private static decimal Percent(List<decimal> values, decimal min, decimal max) => values.Count == 0 ? 0 : Math.Round(values.Count(x => x >= min && x < max) * 100m / values.Count);
    private static bool Match(string? search, params string?[] values) => string.IsNullOrWhiteSpace(search) || values.Any(x => x?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);
    private static CatalogItemDto Item(Guid id, params (string Key, string Value)[] values) => new(id, values.ToDictionary(x => x.Key, x => x.Value));
    private static Dictionary<string, string> Row(params (string Key, string Value)[] values) => values.ToDictionary(x => x.Key, x => x.Value);
    private static string Required(Dictionary<string, string> values, string key) => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException($"{key} is required.");
    private static string Get(Dictionary<string, string> values, string key, string fallback = "") => values.GetValueOrDefault(key, fallback);
    private static int Int(Dictionary<string, string> values, string key, int fallback) => int.TryParse(Get(values, key), out var number) ? number : fallback;
    private static decimal Decimal(Dictionary<string, string> values, string key) => decimal.TryParse(Required(values, key), out var number) ? number : throw new ArgumentException($"{key} must be a number.");
    private static bool Bool(Dictionary<string, string> values, string key, bool fallback) => bool.TryParse(Get(values, key), out var result) ? result : fallback;

    private static void ValidateSettings(string section, Dictionary<string, string> values)
    {
        if (section is "academic-year" or "semester" &&
            DateOnly.TryParse(Get(values, "startsOn"), out var startsOn) &&
            DateOnly.TryParse(Get(values, "endsOn"), out var endsOn) && endsOn <= startsOn)
            throw new ArgumentException("The end date must be after the start date.");
        if (section == "grade-rules")
        {
            var a = Int(values, "aMinimum", 90); var b = Int(values, "bMinimum", 80);
            var c = Int(values, "cMinimum", 70); var d = Int(values, "dMinimum", 60);
            if (a > 100 || d < 0 || !(a > b && b > c && c > d))
                throw new ArgumentException("Grade minimums must descend from A to D and stay between 0 and 100.");
        }
        if (section == "attendance-rules" && Int(values, "lateThresholdMinutes", 15) < 0)
            throw new ArgumentException("The late threshold cannot be negative.");
        if (section is "courses" or "classrooms")
            foreach (var value in values.Where(x => x.Key.Contains("default", StringComparison.OrdinalIgnoreCase) && int.TryParse(x.Value, out _)))
                if (int.Parse(value.Value) <= 0) throw new ArgumentException("Default capacities and credits must be greater than zero.");
    }

    private async Task<T?> ReadCacheAsync<T>(string key)
    {
        if (redis is null || !redis.IsConnected) return default;
        try
        {
            var value = await redis.GetDatabase().StringGetAsync(key);
            return value.HasValue ? JsonSerializer.Deserialize<T>(value.ToString()) : default;
        }
        catch (RedisException) { return default; }
    }

    private async Task WriteCacheAsync<T>(string key, T value)
    {
        if (redis is null || !redis.IsConnected) return;
        try { await redis.GetDatabase().StringSetAsync(key, JsonSerializer.Serialize(value), TimeSpan.FromSeconds(30)); }
        catch (RedisException) { }
    }

    private async Task InvalidateDashboardAsync()
    {
        if (redis is null || !redis.IsConnected) return;
        try { await redis.GetDatabase().KeyDeleteAsync("dashboard:summary"); }
        catch (RedisException) { }
    }
}
