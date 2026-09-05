namespace InstituteManagement.Application.Features.Administration.Settings;

public static partial class SettingsCatalog
{
    private static SettingsSectionDefinition Section(string name, params SettingDefinition[] settings) => new(name, settings);
    private static SettingDefinition Text(string key, string value, int maximumLength = 2048) => new(key, value, MaximumLength: maximumLength);
    private static SettingDefinition OptionalText(string key, string value, int maximumLength = 2048) => new(key, value, MaximumLength: maximumLength, AllowEmpty: true);
    private static SettingDefinition Boolean(string key, bool value) => new(key, value ? "true" : "false", SettingValueType.Boolean);
    private static SettingDefinition Integer(string key, string value, int minimum, int maximum) => new(key, value, SettingValueType.Integer, minimum, maximum);
    private static SettingDefinition OptionalInteger(string key, string value, int minimum, int maximum) => new(key, value, SettingValueType.Integer, minimum, maximum, AllowEmpty: true);
    private static SettingDefinition Decimal(string key, string value, decimal minimum, decimal maximum) => new(key, value, SettingValueType.Decimal, minimum, maximum);
    private static SettingDefinition Date(string key, string value) => new(key, value, SettingValueType.Date, MaximumLength: 10);
    private static SettingDefinition Email(string key, string value) => new(key, value, SettingValueType.Email, MaximumLength: 320);
    private static SettingDefinition OptionalEmail(string key, string value) => new(key, value, SettingValueType.Email, MaximumLength: 320, AllowEmpty: true);
    private static SettingDefinition OptionalUri(string key, string value) => new(key, value, SettingValueType.Uri, MaximumLength: 500, AllowEmpty: true);
    private static SettingDefinition TimeZone(string key, string value) => new(key, value, SettingValueType.TimeZone, MaximumLength: 128);
    private static SettingDefinition Option(string key, string value, params string[] options) => new(key, value, SettingValueType.Option, Options: options);
    private static SettingDefinition OptionalOption(string key, string value, params string[] options) => new(key, value, SettingValueType.Option, AllowEmpty: true, Options: options);
    private static SettingDefinition Code(string key, string value, int maximumLength) => new(key, value, SettingValueType.Code, MaximumLength: maximumLength);
    private static SettingDefinition OptionalCode(string key, string value, int maximumLength) => new(key, value, SettingValueType.Code, MaximumLength: maximumLength, AllowEmpty: true);
    private static SettingDefinition Digits(string key, string value, int maximumLength) => new(key, value, SettingValueType.Digits, MaximumLength: maximumLength);
    private static SettingDefinition OptionalPath(string key, string value) => new(key, value, SettingValueType.Path, MaximumLength: 500, AllowEmpty: true);
    private static SettingDefinition List(string key, string value) => new(key, value, SettingValueType.List);
    private static SettingDefinition OptionalList(string key, string value) => new(key, value, SettingValueType.List, AllowEmpty: true);
}
