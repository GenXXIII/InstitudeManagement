using System.Globalization;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Finance;

public sealed record FinanceSettings(decimal SemesterPrice, string Currency, int PaymentDueDays);

public sealed class FinanceSettingsReader(InstituteDbContext db)
{
    public async Task<FinanceSettings> GetAsync(CancellationToken cancellationToken)
    {
        var values = await db.SystemSettings.AsNoTracking()
            .Where(setting => setting.Section == "finance")
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, cancellationToken);
        var price = decimal.TryParse(
            values.GetValueOrDefault("semesterPrice"),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var configuredPrice)
            ? configuredPrice
            : 500m;
        var dueDays = int.TryParse(values.GetValueOrDefault("paymentDueDays"), out var configuredDays)
            ? configuredDays
            : 14;
        var currency = values.GetValueOrDefault("currency", "USD");
        return new(decimal.Max(0, price), currency is "USD" or "KHR" ? currency : "USD", Math.Clamp(dueDays, 0, 365));
    }
}
