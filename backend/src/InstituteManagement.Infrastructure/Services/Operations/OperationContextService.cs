using InstituteManagement.Application.Features.Dashboard;
using InstituteManagement.Application.Features.Operations;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Operations;

public sealed class OperationContextService(InstituteDbContext db)
{
    public async Task<OperationContext> GetAsync(Guid? departmentId, CancellationToken cancellationToken)
    {
        var department = departmentId.HasValue
            ? await db.Departments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == departmentId && x.IsActive, cancellationToken) ?? throw new KeyNotFoundException("Department not found.")
            : null;
        var activityQuery = db.AuditLogs.AsNoTracking().AsQueryable();
        if (departmentId.HasValue) activityQuery = activityQuery.Where(x => x.Details.Contains(departmentId.Value.ToString()));
        var activity = await activityQuery.OrderByDescending(x => x.CreateAt).Take(4).Select(x => new ActivityDto(x.CreateAt.ToString("HH:mm"), x.Action, x.Subject)).ToListAsync(cancellationToken);
        var attention = departmentId.HasValue ? [] : await db.Notifications.AsNoTracking().Where(x => !x.IsRead).Take(4).Select(x => new ActivityDto("Now", x.Title, x.Message, x.Severity.ToLower(), x.NotificationCode)).ToListAsync(cancellationToken);
        return new OperationContext(department is null ? "the whole institute" : $"{department.Name} ({department.DepartmentCode})", activity, attention);
    }
}
