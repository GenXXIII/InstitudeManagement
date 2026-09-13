using InstituteManagement.Application.Features.MobileAccess;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.MobileAccess;

public sealed class MobileAccessService(InstituteDbContext db) : IMobileAccessService
{
    private const string InitialPassword = "1234";

    public async Task<MobileSessionDto?> SignInAsync(string publicId, string password, CancellationToken cancellationToken)
    {
        var normalizedPublicId = publicId.Trim().ToUpperInvariant();
        if (normalizedPublicId.Length == 0 || password != InitialPassword) return null;

        var teacher = await db.Teachers.AsNoTracking()
            .Where(item => item.Status != "Inactive" && item.PublicId == normalizedPublicId)
            .Select(item => new MobileSessionDto("teacher", item.PublicId, item.Id))
            .SingleOrDefaultAsync(cancellationToken);
        if (teacher is not null) return teacher;

        return await db.Students.AsNoTracking()
            .Where(item => item.Status != "Inactive" && item.PublicId == normalizedPublicId)
            .Select(item => new MobileSessionDto("student", item.PublicId, item.Id))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
