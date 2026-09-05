using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;

namespace InstituteManagement.Infrastructure.Services.Enrollment;

internal sealed class EnrollmentChangeCommitter(InstituteDbContext db, InstituteCache cache)
{
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);
        await cache.InvalidateDashboardAsync(cancellationToken);
    }
}
