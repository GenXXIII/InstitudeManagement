using InstituteManagement.Application.Features.Notifications.Common;

namespace InstituteManagement.Application.Tests.Notifications;

public sealed class NotificationContentValidatorTests
{
    [Fact]
    public void Required_trims_valid_content() =>
        Assert.Equal("Notice", NotificationContentValidator.Required("  Notice  ", "Title", 20));

    [Fact]
    public void Code_rejects_transport_unsafe_characters() =>
        Assert.Throws<ArgumentException>(() => NotificationContentValidator.Code("notice #1", "Code"));

    [Fact]
    public void Choice_returns_the_canonical_allowed_value() =>
        Assert.Equal("Critical", NotificationContentValidator.Choice("critical", ["Info", "Critical"], "Severity"));
}
