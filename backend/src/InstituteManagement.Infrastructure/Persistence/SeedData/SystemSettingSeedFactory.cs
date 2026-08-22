using InstituteManagement.Domain.Entities;

namespace InstituteManagement.Infrastructure.Persistence.SeedData;

public static class SystemSettingSeedFactory
{
    public static IEnumerable<SystemSetting> Create()
    {
        var sections = new Dictionary<string, Dictionary<string, string>>
        {
            ["institute"] = new() { ["name"] = "Institude of New Khmer", ["shortName"] = "INK", ["email"] = "info@ink.edu.kh", ["phone"] = "+855 23 000 000", ["address"] = "Phnom Penh, Cambodia" },
            ["academic-year"] = new() { ["currentYear"] = "2026–2027", ["startsOn"] = "2026-08-03", ["endsOn"] = "2027-06-18" },
            ["semester"] = new()
            {
                ["currentTerm"] = "Semester 1",
                ["startsOn"] = "2026-08-03",
                ["endsOn"] = "2026-12-18",
                ["semester1StartsOn"] = "2026-08-03",
                ["semester1EndsOn"] = "2026-12-18",
                ["semester2StartsOn"] = "2027-01-04",
                ["semester2EndsOn"] = "2027-06-18"
            },
            ["departments"] = new() { ["requireDepartmentHead"] = "true", ["allowCrossDepartmentTeaching"] = "false", ["defaultStatus"] = "Active" },
            ["courses"] = new() { ["defaultCapacity"] = "40", ["requireAssignedTeacher"] = "true" },
            ["classrooms"] = new() { ["defaultCapacity"] = "40", ["attendanceDeviceRequired"] = "true", ["allowSharedRooms"] = "false" },
            ["attendance-rules"] = new() { ["method"] = "ID Card", ["lateThresholdMinutes"] = "15", ["autoAbsent"] = "true", ["autoPercentage"] = "true", ["notifyTeacher"] = "true", ["notifyAdministrator"] = "true", ["allowCorrection"] = "true", ["requireCorrectionReason"] = "false" },
            ["grade-rules"] = new() { ["aMinimum"] = "90", ["bMinimum"] = "80", ["cMinimum"] = "70", ["dMinimum"] = "60", ["eMinimum"] = "50" },
            ["notifications"] = new() { ["attendanceAlerts"] = "true", ["deviceAlerts"] = "true", ["gradeReminders"] = "true", ["dailySummary"] = "true" },
            ["system"] = new() { ["timeZone"] = "Asia/Bangkok", ["language"] = "English", ["dateFormat"] = "DD MMM YYYY", ["autoRefreshSeconds"] = "30" }
        };
        return sections.SelectMany(section => section.Value.Select(item => new SystemSetting { Section = section.Key, Key = item.Key, Value = item.Value }));
    }
}
