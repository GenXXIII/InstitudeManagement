using InstituteManagement.Domain.Entities;

namespace InstituteManagement.Infrastructure.Persistence.SeedData;

public static class NotificationSeedFactory
{
    public static Notification[] Create() => [new() { Title = "Classroom device offline", Message = "Attendance device in 403 needs attention.", Severity = "Critical" }, new() { Title = "Late arrivals", Message = "10 students were marked late today.", Severity = "Warning" }, new() { Title = "Grades pending", Message = "24 student results are waiting for submission.", Severity = "Warning" }];
}
