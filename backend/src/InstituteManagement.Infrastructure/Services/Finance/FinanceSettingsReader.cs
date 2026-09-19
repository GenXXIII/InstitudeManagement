using System.Globalization;
using InstituteManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Finance;

public sealed record FinanceSettings(
    decimal TuitionFee,
    decimal OtherFee,
    string Currency,
    int PaymentDueDays,
    IReadOnlyList<string> PaymentMethods,
    bool AllowPartialPayments,
    bool AllowOverpayment,
    decimal MaximumAdjustmentAmount,
    bool RequirePaidForAdvancement,
    bool BakongEnabled,
    string BakongEnvironment,
    string BakongAccountId,
    string BakongAccountInformation,
    string BakongAcquiringBank,
    string BakongMerchantName,
    string BakongMerchantCity);

public sealed class FinanceSettingsReader(InstituteDbContext db)
{
    public async Task<FinanceSettings> GetAsync(CancellationToken cancellationToken)
    {
        var values = await db.SystemSettings.AsNoTracking()
            .Where(setting => setting.Section == "finance")
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, cancellationToken);
        var tuitionFee = Decimal(values, "semesterPrice", 500m);
        var otherFee = Decimal(values, "otherFee", 0m);
        var dueDays = int.TryParse(values.GetValueOrDefault("paymentDueDays"), out var configuredDays)
            ? configuredDays
            : 14;
        var currency = values.GetValueOrDefault("currency", "USD");
        var bakongEnabled = Boolean(values, "bakongEnabled", false);
        var methods = values.GetValueOrDefault("paymentMethods", "Cash,Bakong,ABA,ACLEDA,Wing,Bank Transfer,Other")
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (bakongEnabled && !methods.Contains("Bakong", StringComparer.OrdinalIgnoreCase)) methods.Add("Bakong");
        if (methods.Count == 0) methods.Add("Other");
        return new(
            decimal.Max(0, tuitionFee),
            decimal.Max(0, otherFee),
            currency is "USD" or "KHR" ? currency : "USD",
            Math.Clamp(dueDays, 0, 365),
            methods,
            Boolean(values, "allowPartialPayments", true),
            Boolean(values, "allowOverpayment", false),
            decimal.Max(0, Decimal(values, "maximumAdjustmentAmount", 1000000m)),
            Boolean(values, "requirePaidForAdvancement", true),
            bakongEnabled,
            values.GetValueOrDefault("bakongEnvironment", "SIT") is "Production" ? "Production" : "SIT",
            values.GetValueOrDefault("bakongAccountId", "").Trim(),
            values.GetValueOrDefault("bakongAccountInformation", "").Trim(),
            values.GetValueOrDefault("bakongAcquiringBank", "").Trim(),
            values.GetValueOrDefault("bakongMerchantName", "Institude of New Khmer").Trim(),
            values.GetValueOrDefault("bakongMerchantCity", "Phnom Penh").Trim());
    }

    private static decimal Decimal(IReadOnlyDictionary<string, string> values, string key, decimal fallback) =>
        decimal.TryParse(values.GetValueOrDefault(key), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : fallback;

    private static bool Boolean(IReadOnlyDictionary<string, string> values, string key, bool fallback) =>
        bool.TryParse(values.GetValueOrDefault(key), out var value) ? value : fallback;
}
