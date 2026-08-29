using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InstituteManagement.Infrastructure.Persistence;

public sealed class InstituteDbContextFactory : IDesignTimeDbContextFactory<InstituteDbContext>
{
    public InstituteDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InstituteDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=INK_DesignTime;Integrated Security=True;TrustServerCertificate=True")
            .Options;
        return new InstituteDbContext(options);
    }
}
