namespace InstituteManagement.Application.Features.Administration.Settings;

public static partial class SettingsCatalog
{
    private const string UserStatuses = "Active,Inactive,Suspended,Pending,Locked";
    private const string RoleLabels = "Super Administrator,Administrator,Teacher,Staff,Student";
    private const string PermissionLabels = "Dashboard,View Students,Create Students,Edit Students,Delete Students,View Teachers,Create Teachers,Edit Teachers,Delete Teachers,View Courses,Create Courses,Edit Courses,Delete Courses,View Attendance,Create Attendance,Edit Attendance,View Grades,Create Grades,Edit Grades,Publish Grades,Manage Users,Manage Roles,Manage Settings,View System Logs";
    private const string StudentStatuses = "Applicant,Active,Inactive,Suspended,Graduated,Withdrawn,Expelled";
    private const string StudentRequiredInformation = "fullName,dateOfBirth,gender,phone,email,address,emergencyContact,profilePhoto,identificationDocument,previousEducation";
    private const string TeacherStatuses = "Active,Inactive,On Leave,Suspended,Terminated";

    private static readonly SettingsSectionDefinition UsersAccessSection = Section("users-access",
        Option("defaultUserStatus", "Active", "Active", "Inactive", "Suspended", "Pending", "Locked"),
        List("userStatuses", UserStatuses),
        List("availableRoles", RoleLabels),
        List("permissionCatalog", PermissionLabels));

    private static readonly SettingsSectionDefinition StudentRulesSection = Section("student-rules",
        Boolean("requireApplication", true),
        Boolean("requireDocuments", true),
        Boolean("allowLateEnrollment", true),
        Integer("lateEnrollmentDays", "14", 0, 365),
        Boolean("requireEnrollmentApproval", true),
        Integer("maximumCoursesPerSemester", "6", 1, 50),
        List("statuses", StudentStatuses),
        List("requiredInformation", StudentRequiredInformation));

    private static readonly SettingsSectionDefinition TeacherRulesSection = Section("teacher-rules",
        List("statuses", TeacherStatuses),
        Integer("maximumCourses", "4", 1, 100),
        Integer("maximumClasses", "6", 1, 100),
        Boolean("allowMultipleDepartments", true),
        Boolean("requireDepartmentAssignment", true),
        Boolean("requireCourseAssignment", true));
}
