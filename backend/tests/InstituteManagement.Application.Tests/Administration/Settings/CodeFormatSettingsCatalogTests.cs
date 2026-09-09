using InstituteManagement.Application.Features.Administration.Settings;

namespace InstituteManagement.Application.Tests.Administration.Settings;

public sealed class CodeFormatSettingsCatalogTests
{
    [Fact]
    public void Notification_and_alert_codes_belong_to_code_formats()
    {
        var codeFormatKeys = SettingsCatalog.GetSection("code-formats").SettingsByKey.Keys;
        var notificationKeys = SettingsCatalog.GetSection("notifications").SettingsByKey.Keys;

        Assert.Contains("alertCodePrefix", codeFormatKeys);
        Assert.Contains("notificationCodePrefix", codeFormatKeys);
        Assert.Contains("historyCodePrefix", codeFormatKeys);
        Assert.DoesNotContain("notificationCodePrefix", notificationKeys);
        Assert.DoesNotContain("alertCodePrefix", notificationKeys);
        Assert.DoesNotContain("historyCodePrefix", notificationKeys);
        Assert.DoesNotContain("codePaddingWidth", notificationKeys);
        Assert.DoesNotContain("alertManagementPrefix", codeFormatKeys);
    }

    [Fact]
    public void Code_format_defaults_are_valid_with_notification_prefixes()
    {
        var defaults = SettingsCatalog.Defaults("code-formats");

        var normalized = SettingsCatalog.NormalizeAndValidate("code-formats", defaults);

        Assert.Equal("ALT", normalized["alertCodePrefix"]);
        Assert.Equal("NOT", normalized["notificationCodePrefix"]);
        Assert.Equal("NHS", normalized["historyCodePrefix"]);
    }
}
