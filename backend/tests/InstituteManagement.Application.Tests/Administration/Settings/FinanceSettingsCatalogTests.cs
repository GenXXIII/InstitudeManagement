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
    public void Aba_and_acleda_settings_are_not_exposed()
    {
        var values = SettingsCatalog.Defaults("finance");

        Assert.DoesNotContain("abaEnabled", values.Keys);
        Assert.DoesNotContain("abaAccountName", values.Keys);
        Assert.DoesNotContain("abaAccountCode", values.Keys);
        Assert.DoesNotContain("acledaEnabled", values.Keys);
        Assert.DoesNotContain("acledaAccountName", values.Keys);
        Assert.DoesNotContain("acledaAccountCode", values.Keys);
        Assert.Equal("true", values["mockPaymentEnabled"]);
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
