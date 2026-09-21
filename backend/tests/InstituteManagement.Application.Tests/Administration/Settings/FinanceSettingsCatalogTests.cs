using InstituteManagement.Application.Features.Administration.Settings;

namespace InstituteManagement.Application.Tests.Administration.Settings;

public sealed class FinanceSettingsCatalogTests
{
    [Fact]
    public void Finance_settings_expose_semester_payment_without_year_plan()
    {
        var values = SettingsCatalog.Defaults("finance");

        var normalized = SettingsCatalog.NormalizeAndValidate("finance", values);

        Assert.Equal("500.00", normalized["semesterPrice"]);
        Assert.DoesNotContain("yearPrice", normalized.Keys);
        Assert.DoesNotContain("defaultPaymentPlan", normalized.Keys);
    }

    [Fact]
    public void Enabled_bank_requires_account_details()
    {
        var values = SettingsCatalog.Defaults("finance");
        values["abaEnabled"] = "true";

        var error = Assert.Throws<ArgumentException>(() => SettingsCatalog.NormalizeAndValidate("finance", values));

        Assert.Contains("ABA AccountName", error.Message);
        Assert.Contains("ABA AccountCode", error.Message);
    }

    [Fact]
    public void Configured_enabled_bank_is_accepted()
    {
        var values = SettingsCatalog.Defaults("finance");
        values["acledaEnabled"] = "true";
        values["acledaAccountName"] = "Institude of New Khmer";
        values["acledaAccountCode"] = "INK-001";

        var normalized = SettingsCatalog.NormalizeAndValidate("finance", values);

        Assert.Equal("true", normalized["acledaEnabled"]);
    }

    [Fact]
    public void Personal_bakong_qr_requires_only_receiver_id_and_account_name()
    {
        var values = SettingsCatalog.Defaults("finance");
        values["bakongEnabled"] = "true";

        var error = Assert.Throws<ArgumentException>(() => SettingsCatalog.NormalizeAndValidate("finance", values));

        Assert.Contains("bakongAccountId", error.Message);
        Assert.DoesNotContain("bakongAccountInformation", error.Message);
        Assert.DoesNotContain("bakongAcquiringBank", error.Message);
    }
}
