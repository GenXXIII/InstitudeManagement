namespace InstituteManagement.Application.Features.Administration.Settings;

public static partial class SettingsCatalog
{
    private static readonly SettingsSectionDefinition InstituteSection = Section("institute",
        Text("name", "Institude of New Khmer", 200),
        Code("shortName", "INK", 32),
        Code("code", "INK", 32),
        Integer("establishedYear", "2018", 1000, 9999),
        Text("description", "The academic management platform for Institude of New Khmer.", 1000),
        OptionalPath("logoUrl", "/branding/ink-logo.png"),
        OptionalPath("faviconUrl", "/icon.png"),
        Email("email", "info@ink.edu.kh"),
        Text("phone", "+855 23 555 888", 40),
        OptionalText("mobile", "+855 12 555 888", 40),
        OptionalUri("website", "https://www.ink.edu.kh"),
        Text("country", "Cambodia", 100),
        Text("city", "Phnom Penh", 100),
        Text("province", "Phnom Penh", 100),
        OptionalText("district", "Sen Sok", 100),
        Text("address", "Street 1986, Sangkat Phnom Penh Thmey, Khan Sen Sok, Phnom Penh", 500),
        OptionalText("postalCode", "12101", 20));
}
