namespace InstituteManagement.API.Routes;

public static class ApiRoutes
{
    public const string Settings = "api/settings";
    public const string Attendance = "api/attendance";
    public const string Dashboard = "api/dashboard";
    public const string Grades = "api/grades";
    public const string History = "api/records";
    public const string Operations = "api/operations";
    public const string OperationalRecords = "api/operational-records";
    public const string Results = "api/results";
    public const string Timetable = "api/timetable";

    public static class Catalog
    {
        public const string Students = "api/catalog/students";
        public const string Teachers = "api/catalog/teachers";
        public const string Courses = "api/catalog/courses";
        public const string Departments = "api/catalog/departments";
        public const string Classrooms = "api/catalog/classrooms";
        public const string Timetable = "api/catalog/timetable";
        public const string Attendance = "api/catalog/attendance";
        public const string Grades = "api/catalog/grades";
    }

    public static class Enrollment
    {
        public const string Students = "api/enrollment/students";
        public const string Teachers = "api/enrollment/teachers";
        public const string Courses = "api/enrollment/courses";
        public const string Departments = "api/enrollment/departments";
        public const string Classrooms = "api/enrollment/classrooms";
        public const string Timetable = "api/enrollment/timetable";
    }

    public static class NotificationCenter
    {
        public const string Notifications = "api/notification-center/notifications";
        public const string Announcements = "api/notification-center/alerts";
        public const string History = "api/notification-center/history";
    }
}
