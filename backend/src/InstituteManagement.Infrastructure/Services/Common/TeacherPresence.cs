namespace InstituteManagement.Infrastructure.Services.Common;

public static class TeacherPresence
{
    public static string Attendance(string? teacherStatus, string? assignmentStatus = null)
    {
        var status = assignmentStatus is "On leave" or "Permission" or "Absent" ? assignmentStatus : teacherStatus;
        return status?.Trim().ToLowerInvariant() switch
        {
            "on leave" or "permission" => "Permission",
            "inactive" or "absent" => "Absent",
            _ => "Present"
        };
    }

    public static bool IsPresent(string attendance) => attendance is "Present" or "Late";
    public static string SessionStatus(string attendance) => IsPresent(attendance) ? "Running" : "Not running";
    public static string Reason(string attendance) => attendance switch
    {
        "Permission" => "Teacher has permission; the assigned course was not held and the classroom remained available.",
        "Absent" => "Teacher absent; the assigned course was not held and the classroom remained available.",
        _ => "Teacher present; the assigned course was held in the classroom."
    };
}
