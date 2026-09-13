using InstituteManagement.Application.Features.Administration.Settings;

namespace InstituteManagement.Application.Tests.Administration.Settings;

public sealed class GradeRulesSettingsCatalogTests
{
    [Fact]
    public void Grade_rule_defaults_include_four_components_that_total_one_hundred()
    {
        var values = SettingsCatalog.NormalizeAndValidate("grade-rules", SettingsCatalog.Defaults("grade-rules"));

        Assert.Equal("10", values["attendanceWeight"]);
        Assert.Equal("20", values["assignmentWeight"]);
        Assert.Equal("20", values["midtermWeight"]);
        Assert.Equal("50", values["finalExamWeight"]);
        Assert.Equal("50", values["eMinimum"]);
        Assert.DoesNotContain("aPlusMinimum", values.Keys);
    }

    [Fact]
    public void Grade_rule_weights_must_total_one_hundred()
    {
        var values = SettingsCatalog.Defaults("grade-rules");
        values["finalExamWeight"] = "40";

        var error = Assert.Throws<ArgumentException>(() => SettingsCatalog.NormalizeAndValidate("grade-rules", values));

        Assert.Contains("must total exactly 100%", error.Message);
    }
}
