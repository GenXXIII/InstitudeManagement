using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Catalog;

public abstract class CatalogFeatureBase<TResponse>(InstituteDbContext db, InstituteCache cache)
{
    protected InstituteDbContext Db { get; } = db;
    protected InstituteCache Cache { get; } = cache;
    public abstract CatalogResource Resource { get; }
    public abstract Task<IReadOnlyList<TResponse>> GetAsync(string? search, Guid? departmentId, CancellationToken ct);
    public abstract Task<TResponse> CreateAsync(Dictionary<string, string> values, CancellationToken ct);
    public abstract Task<TResponse> UpdateAsync(Guid id, Dictionary<string, string> values, CancellationToken ct);
    protected abstract Task<Entity?> FindAsync(Guid id, CancellationToken ct);
    protected virtual Task ValidateDeleteAsync(Entity entity, CancellationToken ct) => Task.CompletedTask;
    protected abstract void Deactivate(Entity entity);
    protected abstract TResponse Response(Guid id, IReadOnlyDictionary<string, string> values);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await FindAsync(id, ct);
        if (entity is null) return false;
        await ValidateDeleteAsync(entity, ct);
        var subject = CatalogAuditFactory.Subject(entity);
        Deactivate(entity);
        Db.AuditLogs.Add(CatalogAuditFactory.ForEntity(
            Resource,
            entity,
            subject,
            entity is AttendanceRecord or GradeRecord ? "Removed" : "Deactivated"));
        await Db.SaveChangesAsync(ct);
        await Cache.InvalidateDashboardAsync(ct);
        return true;
    }

    protected async Task<TResponse> SaveCreatedAsync(Entity entity, Dictionary<string, string> values, CancellationToken ct)
    {
        Db.Add(entity);
        Db.AuditLogs.Add(Audit(entity.Id, values, "Created"));
        await Db.SaveChangesAsync(ct);
        await Cache.InvalidateDashboardAsync(ct);
        return Response(entity.Id, values);
    }

    protected async Task<TResponse> SaveUpdatedAsync(Guid id, Dictionary<string, string> values, CancellationToken ct)
    {
        Db.AuditLogs.Add(Audit(id, values, "Updated"));
        await Db.SaveChangesAsync(ct);
        await Cache.InvalidateDashboardAsync(ct);
        return Response(id, values);
    }

    protected AuditLog Audit(Guid id, Dictionary<string, string> values, string action) =>
        CatalogAuditFactory.ForValues(Resource, id, values, action);

    protected static bool Matches(string? search, params string?[] values) => string.IsNullOrWhiteSpace(search) || values.Any(x => x?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);

    protected static string Required(Dictionary<string, string> values, string key) =>
        CatalogValidation.Required(values, key);

    protected static string RequiredCode(Dictionary<string, string> values, string key) =>
        CatalogValidation.RequiredCode(values, key);

    protected Task<string> ConfiguredCodeAsync(Dictionary<string, string> values, string key, string resource, CancellationToken ct) =>
        BusinessCodeFormatter.FormatAsync(Db, values, key, resource, "management", ct);

    protected static string Get(IReadOnlyDictionary<string, string> values, string key, string fallback = "") =>
        CatalogValidation.Get(values, key, fallback);

    protected static string Email(Dictionary<string, string> values, string key) =>
        CatalogValidation.Email(values, key);

    protected static string OneOf(Dictionary<string, string> values, string key, string fallback, params string[] allowed) =>
        CatalogValidation.OneOf(values, key, fallback, allowed);

    protected static int Int(Dictionary<string, string> values, string key, int fallback) =>
        CatalogValidation.Int(values, key, fallback);

    protected static int IntInRange(Dictionary<string, string> values, string key, int fallback, int minimum, int maximum) =>
        CatalogValidation.IntInRange(values, key, fallback, minimum, maximum);

    protected static decimal DecimalInRange(Dictionary<string, string> values, string key, decimal minimum, decimal maximum) =>
        CatalogValidation.DecimalInRange(values, key, minimum, maximum);

    protected static bool Bool(Dictionary<string, string> values, string key, bool fallback) =>
        CatalogValidation.Bool(values, key, fallback);

    protected static void Touch(Entity entity) => entity.UpdatedAtUtc = DateTime.UtcNow;

    protected static Task<T> RequiredEntityAsync<T>(DbSet<T> set, Guid id, CancellationToken ct) where T : Entity =>
        CatalogValidation.RequiredEntityAsync(set, id, ct);

    protected static async Task EnsureUniqueAsync<T>(IQueryable<T> duplicates, string field, CancellationToken ct)
        where T : class =>
        await CatalogValidation.EnsureUniqueAsync(duplicates, field, ct);

    protected Task<Guid> RelatedIdAsync<T>(Dictionary<string, string> values, string key, CancellationToken ct) where T : Entity =>
        CatalogValidation.RelatedIdAsync<T>(Db, values, key, ct);

}
