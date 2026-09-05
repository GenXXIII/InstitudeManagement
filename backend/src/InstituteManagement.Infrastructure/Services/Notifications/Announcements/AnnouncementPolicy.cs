using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Notifications.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Notifications.Announcements;

public sealed class AnnouncementPolicy(InstituteDbContext db)
{
    private static readonly string[] Types = ["General", "Attendance", "Emergency", "Result"];

    public async Task<string> ValidateTypeAsync(string? value, CancellationToken cancellationToken)
    {
        var type = NotificationContentValidator.Choice(value, Types, "Alert type");
        if (type == "Result"
            && !await db.GradeRecords.AnyAsync(cancellationToken)
            && !await db.AuditLogs.AnyAsync(item => item.Type == "Grade", cancellationToken))
            throw new InvalidOperationException("Result alerts require semester result data in History.");
        return type;
    }
}
