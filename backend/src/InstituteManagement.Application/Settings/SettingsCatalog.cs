namespace InstituteManagement.Application.Settings;

public static partial class SettingsCatalog
{
    private const string UserStatuses = "Active,Inactive,Suspended,Pending,Locked";
    private const string RoleLabels = "Super Administrator,Administrator,Teacher,Staff,Student";
    private const string PermissionLabels = "Dashboard,View Students,Create Students,Edit Students,Delete Students,View Teachers,Create Teachers,Edit Teachers,Delete Teachers,View Courses,Create Courses,Edit Courses,Delete Courses,View Attendance,Create Attendance,Edit Attendance,View Grades,Create Grades,Edit Grades,Publish Grades,Manage Users,Manage Roles,Manage Settings,View System Logs";
    private const string StudentStatuses = "Applicant,Active,Inactive,Suspended,Graduated,Withdrawn,Expelled";
    private const string StudentRequiredInformation = "fullName,dateOfBirth,gender,phone,email,address,emergencyContact,profilePhoto,identificationDocument,previousEducation";
    private const string TeacherStatuses = "Active,Inactive,On Leave,Suspended,Terminated";
    private const string NotificationTemplates = "Student Enrollment Confirmation,Course Enrollment,Attendance Alert,Grade Published,Password Reset,Account Created,Announcement,Academic Year Started";

    private static readonly IReadOnlyList<SettingsSectionDefinition> CatalogSections =
    [
        Section("institute",
            Text("name", "Nexa Institute of Technology", 200),
            Code("shortName", "NIT", 32),
            Code("code", "NIT", 32),
            Integer("establishedYear", "2018", 1000, 9999),
            Text("description", "A modern institute providing professional education and technology-focused training programs.", 1000),
            OptionalPath("logoUrl", "/uploads/settings/institute-logo.png"),
            OptionalPath("faviconUrl", "/uploads/settings/favicon.ico"),
            Email("email", "info@nexa-institute.edu.kh"),
            Text("phone", "+855 23 555 888", 40),
            OptionalText("mobile", "+855 12 555 888", 40),
            OptionalUri("website", "https://www.nexa-institute.edu.kh"),
            Text("country", "Cambodia", 100),
            Text("city", "Phnom Penh", 100),
            Text("province", "Phnom Penh", 100),
            OptionalText("district", "Sen Sok", 100),
            Text("address", "Street 1986, Sangkat Phnom Penh Thmey, Khan Sen Sok, Phnom Penh", 500),
            OptionalText("postalCode", "12101", 20),
            TimeZone("timeZone", "Asia/Phnom_Penh")),

        Section("academic-year",
            Text("currentYear", "2026–2027", 32),
            Code("code", "AY2026", 32),
            Date("startsOn", "2026-09-01"),
            Date("endsOn", "2027-08-31"),
            Option("status", "Active", "Active", "Upcoming", "Completed", "Inactive")),

        Section("semester",
            Option("currentTerm", "Semester 1", "Semester 1", "Semester 2", "Summer Term"),
            Date("startsOn", "2026-09-01"),
            Date("endsOn", "2027-01-31"),
            Text("semester1Name", "Semester 1", 64),
            Code("semester1Code", "SEM1", 32),
            Date("semester1StartsOn", "2026-09-01"),
            Date("semester1EndsOn", "2027-01-31"),
            Option("semester1Status", "Active", "Active", "Upcoming", "Completed", "Inactive"),
            Text("semester2Name", "Semester 2", 64),
            Code("semester2Code", "SEM2", 32),
            Date("semester2StartsOn", "2027-02-01"),
            Date("semester2EndsOn", "2027-06-30"),
            Option("semester2Status", "Upcoming", "Active", "Upcoming", "Completed", "Inactive"),
            Text("summerName", "Summer Term", 64),
            Code("summerCode", "SUMMER", 32),
            Date("summerStartsOn", "2027-07-01"),
            Date("summerEndsOn", "2027-08-31"),
            Option("summerStatus", "Upcoming", "Active", "Upcoming", "Completed", "Inactive")),

        Section("departments",
            Code("codePrefix", "DEP", 16),
            Boolean("codeIncludeYear", false),
            Digits("codeStartingNumber", "1", 12),
            Integer("codePaddingWidth", "4", 1, 12),
            Option("codeSeparator", "-", "-", "/", "."),
            Option("defaultStatus", "Active", "Active", "Inactive"),
            Boolean("requireDepartmentHead", true),
            Boolean("allowCrossDepartmentTeaching", false)),

        Section("courses",
            Code("codePrefix", "CRS", 16),
            Boolean("codeIncludeYear", false),
            Digits("codeStartingNumber", "1", 12),
            Integer("codePaddingWidth", "4", 1, 12),
            Option("codeSeparator", "-", "-", "/", "."),
            Integer("defaultCapacity", "40", 1, 10000),
            Boolean("requireAssignedTeacher", true)),

        Section("classrooms",
            Code("codePrefix", "ROOM", 16),
            Boolean("codeIncludeYear", false),
            Digits("codeStartingNumber", "1", 12),
            Integer("codePaddingWidth", "4", 1, 12),
            Option("codeSeparator", "-", "-", "/", "."),
            Integer("defaultCapacity", "40", 1, 10000),
            Boolean("attendanceDeviceRequired", true)),

        Section("users-access",
            Option("defaultUserStatus", "Active", "Active", "Inactive", "Suspended", "Pending", "Locked"),
            List("userStatuses", UserStatuses),
            List("availableRoles", RoleLabels),
            List("permissionCatalog", PermissionLabels)),

        Section("student-rules",
            Code("idPrefix", "STU", 16),
            Boolean("includeYear", true),
            Digits("startingNumber", "1", 12),
            Integer("paddingWidth", "4", 1, 12),
            Option("separator", "-", "-", "/", "."),
            Boolean("requireApplication", true),
            Boolean("requireDocuments", true),
            Boolean("allowLateEnrollment", true),
            Integer("lateEnrollmentDays", "14", 0, 365),
            Boolean("requireEnrollmentApproval", true),
            Integer("maximumCoursesPerSemester", "6", 1, 50),
            List("statuses", StudentStatuses),
            List("requiredInformation", StudentRequiredInformation)),

        Section("teacher-rules",
            Code("idPrefix", "TCH", 16),
            Boolean("includeYear", true),
            Digits("startingNumber", "1", 12),
            Integer("paddingWidth", "4", 1, 12),
            Option("separator", "-", "-", "/", "."),
            List("statuses", TeacherStatuses),
            Integer("maximumCourses", "4", 1, 100),
            Integer("maximumClasses", "6", 1, 100),
            Boolean("allowMultipleDepartments", true),
            Boolean("requireDepartmentAssignment", true),
            Boolean("requireCourseAssignment", true)),

        Section("attendance-rules",
            Boolean("attendanceRequired", true),
            Boolean("checkInRequired", true),
            Boolean("checkOutRequired", false),
            Boolean("teacherCanRecord", true),
            Boolean("studentCanView", true),
            Option("method", "ID Card", "Manual", "ID Card", "QR Code", "Biometric"),
            Integer("onTimeThresholdMinutes", "14", 0, 1440),
            Integer("lateThresholdMinutes", "15", 0, 1440),
            Integer("veryLateThresholdMinutes", "30", 0, 1440),
            Integer("absentAfterMinutes", "30", 0, 1440),
            Boolean("autoAbsent", true),
            Boolean("autoPercentage", true),
            Boolean("excusedAbsenceEnabled", true),
            Boolean("requireExcuseApproval", true),
            Integer("maximumExcusedAbsences", "10", 0, 365),
            Boolean("teacherCanEdit", true),
            Boolean("allowCorrection", true),
            Integer("correctionPeriodDays", "7", 0, 365),
            Boolean("requireAdminApproval", true),
            Boolean("requireCorrectionReason", true),
            Boolean("keepChangeHistory", true),
            Boolean("notifyTeacher", true),
            Boolean("notifyAdministrator", true)),

        Section("grade-rules",
            Option("gradingSystem", "Percentage + Letter Grade", "Percentage + Letter Grade"),
            Decimal("maximumScore", "100", 1, 1000),
            Decimal("minimumScore", "0", 0, 999),
            Decimal("passMark", "50", 0, 1000),
            Boolean("gpaEnabled", true),
            Decimal("maximumGpa", "4.00", 0, 10),
            Decimal("aPlusMinimum", "95", 0, 1000),
            Decimal("aMinimum", "90", 0, 1000),
            Decimal("bPlusMinimum", "85", 0, 1000),
            Decimal("bMinimum", "80", 0, 1000),
            Decimal("cPlusMinimum", "75", 0, 1000),
            Decimal("cMinimum", "70", 0, 1000),
            Decimal("dMinimum", "60", 0, 1000),
            Decimal("aPlusGpa", "4.00", 0, 10),
            Decimal("aGpa", "4.00", 0, 10),
            Decimal("bPlusGpa", "3.50", 0, 10),
            Decimal("bGpa", "3.00", 0, 10),
            Decimal("cPlusGpa", "2.50", 0, 10),
            Decimal("cGpa", "2.00", 0, 10),
            Decimal("dGpa", "1.00", 0, 10),
            Decimal("fGpa", "0.00", 0, 10),
            Decimal("overallPassMark", "50", 0, 1000),
            Decimal("coursePassMark", "50", 0, 1000),
            Decimal("finalExamMinimum", "40", 0, 1000),
            Decimal("gpaScale", "4.00", 0, 10),
            Boolean("includeFailedCourses", true),
            Boolean("includeWithdrawnCourses", false),
            Integer("gpaDecimalPlaces", "2", 0, 6)),

        Section("notifications",
            Boolean("attendanceAlerts", true),
            Boolean("deviceAlerts", true),
            Boolean("gradeReminders", true),
            Boolean("dailySummary", true),
            Boolean("emailEnabled", true),
            OptionalText("smtpHost", "smtp.nexa-institute.edu.kh", 255),
            OptionalInteger("smtpPort", "587", 1, 65535),
            OptionalOption("emailEncryption", "TLS", "None", "TLS", "SSL"),
            OptionalText("senderName", "Nexa Institute", 200),
            OptionalEmail("senderEmail", "noreply@nexa-institute.edu.kh"),
            Boolean("smsEnabled", false),
            Text("smsProvider", "None", 100),
            OptionalCode("smsSenderId", "NEXA", 32),
            Boolean("inAppEnabled", true),
            Boolean("studentNotifications", true),
            Boolean("teacherNotifications", true),
            Boolean("staffNotifications", true),
            Boolean("administratorNotifications", true),
            OptionalList("enabledTemplates", NotificationTemplates)),

        Section("system",
            Option("language", "English", "English", "Khmer"),
            List("availableLanguages", "English,Khmer"),
            Option("dateFormat", "DD/MM/YYYY", "DD/MM/YYYY", "MM/DD/YYYY", "YYYY-MM-DD", "DD MMM YYYY"),
            Option("timeFormat", "24 Hour", "12 Hour", "24 Hour"),
            TimeZone("timeZone", "Asia/Phnom_Penh"),
            Option("firstDayOfWeek", "Monday", "Monday", "Sunday"),
            Integer("autoRefreshSeconds", "30", 5, 3600),
            Boolean("maintenanceEnabled", false),
            Text("maintenanceMessage", "System is currently under maintenance. Please try again later.", 1000),
            Boolean("allowAdministratorsDuringMaintenance", true),
            Boolean("loggingEnabled", true),
            Boolean("loginLogs", true),
            Boolean("userActivityLogs", true),
            Boolean("securityLogs", true),
            Boolean("configurationChangeLogs", true),
            Integer("logRetentionDays", "90", 1, 3650)),

        Section("security",
            Integer("passwordMinimumLength", "8", 6, 128),
            Boolean("requireUppercase", true),
            Boolean("requireLowercase", true),
            Boolean("requireNumber", true),
            Boolean("requireSpecialCharacter", true),
            Integer("passwordExpirationDays", "90", 0, 3650),
            Integer("preventPasswordReuse", "5", 0, 100),
            Integer("administratorSessionMinutes", "30", 1, 10080),
            Integer("staffSessionMinutes", "60", 1, 10080),
            Integer("teacherSessionMinutes", "60", 1, 10080),
            Integer("studentSessionMinutes", "120", 1, 10080),
            Boolean("rememberMeEnabled", true),
            Integer("maximumLoginAttempts", "5", 1, 100),
            Integer("lockoutDurationMinutes", "15", 1, 10080),
            Integer("resetAttemptCounterMinutes", "30", 1, 10080),
            Boolean("logFailedAttempts", true),
            Option("twoFactorMode", "Optional", "Disabled", "Optional", "Required"),
            Option("administratorTwoFactor", "Required", "Disabled", "Optional", "Required"),
            Option("staffTwoFactor", "Optional", "Disabled", "Optional", "Required"),
            Option("teacherTwoFactor", "Optional", "Disabled", "Optional", "Required"),
            Option("studentTwoFactor", "Optional", "Disabled", "Optional", "Required"),
            List("twoFactorMethods", "Authenticator App,Email OTP"))
    ];

    private static readonly IReadOnlyDictionary<string, SettingsSectionDefinition> CatalogByName =
        CatalogSections.ToDictionary(section => section.Name, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<SettingsSectionDefinition> Sections => CatalogSections;

    public static SettingsSectionDefinition GetSection(string section) =>
        !string.IsNullOrWhiteSpace(section) && CatalogByName.TryGetValue(section, out var definition)
            ? definition
            : throw new KeyNotFoundException("Settings section not found.");

    public static Dictionary<string, string> Defaults(string section) =>
        GetSection(section).Settings.ToDictionary(setting => setting.Key, setting => setting.DefaultValue, StringComparer.Ordinal);

    private static SettingsSectionDefinition Section(string name, params SettingDefinition[] settings) => new(name, settings);
    private static SettingDefinition Text(string key, string value, int maximumLength = 2048) => new(key, value, MaximumLength: maximumLength);
    private static SettingDefinition OptionalText(string key, string value, int maximumLength = 2048) => new(key, value, MaximumLength: maximumLength, AllowEmpty: true);
    private static SettingDefinition Boolean(string key, bool value) => new(key, value ? "true" : "false", SettingValueType.Boolean);
    private static SettingDefinition Integer(string key, string value, int minimum, int maximum) => new(key, value, SettingValueType.Integer, minimum, maximum);
    private static SettingDefinition OptionalInteger(string key, string value, int minimum, int maximum) => new(key, value, SettingValueType.Integer, minimum, maximum, AllowEmpty: true);
    private static SettingDefinition Decimal(string key, string value, decimal minimum, decimal maximum) => new(key, value, SettingValueType.Decimal, minimum, maximum);
    private static SettingDefinition Date(string key, string value) => new(key, value, SettingValueType.Date, MaximumLength: 10);
    private static SettingDefinition Email(string key, string value) => new(key, value, SettingValueType.Email, MaximumLength: 320);
    private static SettingDefinition OptionalEmail(string key, string value) => new(key, value, SettingValueType.Email, MaximumLength: 320, AllowEmpty: true);
    private static SettingDefinition Uri(string key, string value) => new(key, value, SettingValueType.Uri, MaximumLength: 500);
    private static SettingDefinition OptionalUri(string key, string value) => new(key, value, SettingValueType.Uri, MaximumLength: 500, AllowEmpty: true);
    private static SettingDefinition TimeZone(string key, string value) => new(key, value, SettingValueType.TimeZone, MaximumLength: 128);
    private static SettingDefinition Option(string key, string value, params string[] options) => new(key, value, SettingValueType.Option, Options: options);
    private static SettingDefinition OptionalOption(string key, string value, params string[] options) => new(key, value, SettingValueType.Option, AllowEmpty: true, Options: options);
    private static SettingDefinition Code(string key, string value, int maximumLength) => new(key, value, SettingValueType.Code, MaximumLength: maximumLength);
    private static SettingDefinition OptionalCode(string key, string value, int maximumLength) => new(key, value, SettingValueType.Code, MaximumLength: maximumLength, AllowEmpty: true);
    private static SettingDefinition Digits(string key, string value, int maximumLength) => new(key, value, SettingValueType.Digits, MaximumLength: maximumLength);
    private static SettingDefinition Path(string key, string value) => new(key, value, SettingValueType.Path, MaximumLength: 500);
    private static SettingDefinition OptionalPath(string key, string value) => new(key, value, SettingValueType.Path, MaximumLength: 500, AllowEmpty: true);
    private static SettingDefinition UtcOffset(string key, string value) => new(key, value, SettingValueType.UtcOffset, MaximumLength: 16);
    private static SettingDefinition List(string key, string value) => new(key, value, SettingValueType.List);
    private static SettingDefinition OptionalList(string key, string value) => new(key, value, SettingValueType.List, AllowEmpty: true);
}
