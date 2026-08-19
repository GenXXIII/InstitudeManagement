using InstituteManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(InstituteDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await EnsureCurrentSchemaAsync(db, cancellationToken);
        if (await db.Departments.AnyAsync(cancellationToken))
        {
            await BackfillCurrentDataAsync(db, cancellationToken);
            return;
        }

        var departments = new[]
        {
            new Department { Name = "Information Technology", Code = "IT" },
            new Department { Name = "Accounting & Finance", Code = "ACC" },
            new Department { Name = "Engineering", Code = "ENG", Head = "Dr. Helen Wong" },
            new Department { Name = "Arts & Humanities", Code = "ART", Head = "Prof. Sophia Reed" },
            new Department { Name = "Science", Code = "SCI", Head = "Dr. Noah Kim" }
        };
        db.Departments.AddRange(departments);

        var teacherNames = new[] { "David Smith", "Anna Wilson", "John Carter", "Sarah Miller", "Mike Chen", "Maya Patel", "Oliver Brown", "Emma Davis", "Liam Martin", "Nora James", "Leo Garcia", "Ava Thompson" };
        var teachers = teacherNames.Select((name, index) => new Teacher
        {
            TeacherNumber = $"T-{index + 275:D5}", FullName = name,
            Email = name.ToLowerInvariant().Replace(" ", ".") + "@northstar.edu",
            PhotoDataUrl = Avatar(name, "4267b2"),
            DepartmentId = departments[index % departments.Length].Id,
            Status = index < 6 ? "Teaching" : index < 10 ? "Available" : "On leave"
        }).ToArray();
        db.Teachers.AddRange(teachers);

        var rooms = Enumerable.Range(1, 12).Select(index => new Classroom
        {
            Code = $"{(char)('A' + ((index - 1) / 4))}{100 + ((index - 1) % 4) + 1}",
            Building = $"{(char)('A' + ((index - 1) / 4))} Building",
            Capacity = 35 + (index % 3) * 5,
            DepartmentId = departments[(index - 1) % departments.Length].Id,
            Status = index <= 6 ? "Running" : index <= 9 ? "Available" : index == 12 ? "Offline" : "Starting",
            DeviceOnline = index != 12
        }).ToArray();
        db.Classrooms.AddRange(rooms);

        var courseNames = new[] { "Mathematics", "Physics", "English", "Biology", "Chemistry", "Web Engineering", "Data Analytics", "Business Strategy" };
        var courses = courseNames.Select((name, index) => new Course
        {
            Code = $"{departments[index % departments.Length].Code}-{101 + index}", Name = name,
            DepartmentId = departments[index % departments.Length].Id, TeacherId = teachers[index].Id,
            Credits = index % 3 + 2, Capacity = 35 + (index % 3) * 5
        }).ToArray();
        db.Courses.AddRange(courses);

        var firstNames = new[] { "John", "Mia", "Ethan", "Sofia", "Lucas", "Isla", "Noah", "Amelia", "James", "Lily" };
        var lastNames = new[] { "Smith", "Nguyen", "Brown", "Chen", "Wilson", "Garcia" };
        var students = Enumerable.Range(1, 120).Select(index => new Student
        {
            StudentNumber = $"ST-{4700 + index:D6}", FullName = $"{firstNames[(index - 1) % firstNames.Length]} {lastNames[(index - 1) % lastNames.Length]}",
            Email = $"student{4700 + index}@northstar.edu", DepartmentId = departments[(index - 1) % departments.Length].Id,
            PhotoDataUrl = Avatar($"{firstNames[(index - 1) % firstNames.Length]} {lastNames[(index - 1) % lastNames.Length]}", "2f72d6"),
            YearLevel = ((index - 1) % 4) + 1, Status = index % 29 == 0 ? "Inactive" : "Active"
        }).ToArray();
        db.Students.AddRange(students);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.AttendanceRecords.AddRange(students.Select((student, index) => new AttendanceRecord
        {
            StudentId = student.Id, Date = today,
            CheckedInAt = index % 12 == 0 ? new TimeOnly(8, 18) : new TimeOnly(7, 45).AddMinutes(index % 28),
            Status = index % 17 == 0 ? "Absent" : index % 12 == 0 ? "Late" : "Present"
        }));

        db.GradeRecords.AddRange(students.Take(96).Select((student, index) =>
        {
            var score = 68 + index % 29;
            return new GradeRecord { StudentId = student.Id, CourseId = courses[index % courses.Length].Id, Score = score, LetterGrade = ToLetter(score) };
        }));

        db.ScheduleEntries.AddRange(courses.Take(6).Select((course, index) => new ScheduleEntry
        {
            CourseId = course.Id, TeacherId = teachers[index].Id, ClassroomId = rooms[index].Id,
            DayOfWeek = DateTime.Today.DayOfWeek, StartsAt = new TimeOnly(8 + index, 0), EndsAt = new TimeOnly(9 + index, 0),
            Status = index < 3 ? "Running" : "Upcoming"
        }));

        db.Notifications.AddRange(
            new Notification { Title = "Classroom device offline", Message = "Attendance device in C104 needs attention.", Severity = "Critical" },
            new Notification { Title = "Late arrivals", Message = "10 students were marked late today.", Severity = "Warning" },
            new Notification { Title = "Grades pending", Message = "24 student results are waiting for submission.", Severity = "Warning" });

        db.AuditLogs.AddRange(
            new AuditLog { Type = "Attendance", Subject = students[0].StudentNumber, Action = "Present", Details = "Attendance recorded by ID card" },
            new AuditLog { Type = "Grade", Subject = students[1].StudentNumber, Action = "Grade A", Details = "Mathematics result submitted" },
            new AuditLog { Type = "Timetable", Subject = rooms[0].Code, Action = "Changed", Details = "Class moved to 10:00" },
            new AuditLog { Type = "Course", Subject = courses[1].Name, Action = "Updated", Details = "Course capacity updated" });

        var settings = new Dictionary<string, Dictionary<string, string>>
        {
            ["institute"] = new() { ["name"] = "Northstar Institute", ["shortName"] = "NSI", ["email"] = "hello@northstar.edu", ["phone"] = "+1 555 014 2040", ["address"] = "18 Learning Avenue, River City" },
            ["academic-year"] = new() { ["currentYear"] = "2026–2027", ["startsOn"] = "2026-08-03", ["endsOn"] = "2027-06-18" },
            ["semester"] = new() { ["currentTerm"] = "Semester 1", ["startsOn"] = "2026-08-03", ["endsOn"] = "2026-12-18" },
            ["departments"] = new() { ["requireDepartmentHead"] = "true", ["allowCrossDepartmentTeaching"] = "false", ["defaultStatus"] = "Active" },
            ["courses"] = new() { ["defaultCredits"] = "3", ["defaultCapacity"] = "40", ["requireAssignedTeacher"] = "true" },
            ["classrooms"] = new() { ["defaultCapacity"] = "40", ["attendanceDeviceRequired"] = "true", ["allowSharedRooms"] = "false" },
            ["attendance-rules"] = new() { ["method"] = "ID Card", ["lateThresholdMinutes"] = "15", ["autoAbsent"] = "true", ["autoPercentage"] = "true", ["notifyTeacher"] = "true", ["notifyAdministrator"] = "true", ["allowCorrection"] = "true", ["requireCorrectionReason"] = "true" },
            ["grade-rules"] = new() { ["aMinimum"] = "90", ["bMinimum"] = "80", ["cMinimum"] = "70", ["dMinimum"] = "60" },
            ["notifications"] = new() { ["attendanceAlerts"] = "true", ["deviceAlerts"] = "true", ["gradeReminders"] = "true", ["dailySummary"] = "true" },
            ["system"] = new() { ["timeZone"] = "Asia/Bangkok", ["language"] = "English", ["dateFormat"] = "DD MMM YYYY", ["autoRefreshSeconds"] = "30" }
        };
        foreach (var section in settings)
            foreach (var item in section.Value)
                db.SystemSettings.Add(new SystemSetting { Section = section.Key, Key = item.Key, Value = item.Value });

        await db.SaveChangesAsync(cancellationToken);
        foreach (var department in departments)
        {
            var head = teachers.First(x => x.DepartmentId == department.Id);
            department.HeadTeacherId = head.Id;
            department.Head = head.FullName;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string ToLetter(decimal score) => score >= 90 ? "A" : score >= 80 ? "B" : score >= 70 ? "C" : score >= 60 ? "D" : "F";

    private static async Task EnsureCurrentSchemaAsync(InstituteDbContext db, CancellationToken ct)
    {
        if (!db.Database.IsRelational()) return;
        await db.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH('Students', 'PhotoDataUrl') IS NULL ALTER TABLE [Students] ADD [PhotoDataUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Students_PhotoDataUrl] DEFAULT '';
            IF COL_LENGTH('Teachers', 'PhotoDataUrl') IS NULL ALTER TABLE [Teachers] ADD [PhotoDataUrl] nvarchar(max) NOT NULL CONSTRAINT [DF_Teachers_PhotoDataUrl] DEFAULT '';
            IF COL_LENGTH('Departments', 'HeadTeacherId') IS NULL ALTER TABLE [Departments] ADD [HeadTeacherId] uniqueidentifier NULL;
            IF COL_LENGTH('Classrooms', 'DepartmentId') IS NULL ALTER TABLE [Classrooms] ADD [DepartmentId] uniqueidentifier NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Departments_HeadTeacherId') CREATE INDEX [IX_Departments_HeadTeacherId] ON [Departments] ([HeadTeacherId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Classrooms_DepartmentId') CREATE INDEX [IX_Classrooms_DepartmentId] ON [Classrooms] ([DepartmentId]);
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Departments_Teachers_HeadTeacherId') ALTER TABLE [Departments] ADD CONSTRAINT [FK_Departments_Teachers_HeadTeacherId] FOREIGN KEY ([HeadTeacherId]) REFERENCES [Teachers] ([Id]);
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Classrooms_Departments_DepartmentId') ALTER TABLE [Classrooms] ADD CONSTRAINT [FK_Classrooms_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]);
            """, ct);
    }

    private static async Task BackfillCurrentDataAsync(InstituteDbContext db, CancellationToken ct)
    {
        var departments = await db.Departments.ToListAsync(ct);
        var computerScience = departments.FirstOrDefault(x => x.Code == "CS");
        if (computerScience is not null) { computerScience.Code = "IT"; computerScience.Name = "Information Technology"; }
        var business = departments.FirstOrDefault(x => x.Code == "BUS");
        if (business is not null) { business.Code = "ACC"; business.Name = "Accounting & Finance"; }

        var teachers = await db.Teachers.ToListAsync(ct);
        foreach (var teacher in teachers.Where(x => string.IsNullOrWhiteSpace(x.PhotoDataUrl))) teacher.PhotoDataUrl = Avatar(teacher.FullName, "4267b2");
        var students = await db.Students.ToListAsync(ct);
        foreach (var student in students.Where(x => string.IsNullOrWhiteSpace(x.PhotoDataUrl))) student.PhotoDataUrl = Avatar(student.FullName, "2f72d6");
        var classrooms = await db.Classrooms.ToListAsync(ct);
        for (var i = 0; i < classrooms.Count; i++) classrooms[i].DepartmentId ??= departments[i % departments.Count].Id;
        foreach (var department in departments.Where(x => x.HeadTeacherId is null))
        {
            var head = teachers.FirstOrDefault(x => x.DepartmentId == department.Id);
            if (head is not null) { department.HeadTeacherId = head.Id; department.Head = head.FullName; }
        }
        var settingDefaults = new Dictionary<string, Dictionary<string, string>>
        {
            ["departments"] = new() { ["requireDepartmentHead"] = "true", ["allowCrossDepartmentTeaching"] = "false", ["defaultStatus"] = "Active" },
            ["courses"] = new() { ["defaultCredits"] = "3", ["defaultCapacity"] = "40", ["requireAssignedTeacher"] = "true" },
            ["classrooms"] = new() { ["defaultCapacity"] = "40", ["attendanceDeviceRequired"] = "true", ["allowSharedRooms"] = "false" }
        };
        var currentSettings = await db.SystemSettings.Select(x => new { x.Section, x.Key }).ToListAsync(ct);
        foreach (var section in settingDefaults)
            foreach (var item in section.Value)
                if (!currentSettings.Any(x => x.Section == section.Key && x.Key == item.Key))
                    db.SystemSettings.Add(new SystemSetting { Section = section.Key, Key = item.Key, Value = item.Value });
        await db.SaveChangesAsync(ct);
    }

    private static string Avatar(string name, string color)
    {
        var initials = string.Join("", name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x => char.ToUpperInvariant(x[0])));
        return $"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='400' height='600' viewBox='0 0 400 600'%3E%3Crect width='400' height='600' fill='%23{color}'/%3E%3Ccircle cx='200' cy='210' r='96' fill='%23ffffff' fill-opacity='.22'/%3E%3Ctext x='200' y='245' text-anchor='middle' font-family='Arial' font-size='82' font-weight='700' fill='white'%3E{initials}%3C/text%3E%3Cpath d='M55 600c15-135 75-210 145-210s130 75 145 210' fill='%23ffffff' fill-opacity='.18'/%3E%3C/svg%3E";
    }
}
