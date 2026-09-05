import type { ConfigurationGroup, SettingSection } from "../administration-types";
import { field, options, utcOffset } from "./schema-helpers";

const languageOptions = options("English", "Khmer");
const timeZoneOptions = options("Asia/Phnom_Penh", "Asia/Bangkok", "Asia/Ho_Chi_Minh", "UTC");
const twoFactorOptions = options("Disabled", "Optional", "Required");

function notificationCodeExample(values: Record<string, string>, prefixKey: string, fallbackPrefix: string) {
  const prefix = values[prefixKey]?.trim().toUpperCase() || fallbackPrefix;
  const separator = values.codeSeparator || "-";
  const year = values.codeIncludeYear === "true" ? `${separator}${new Date().getFullYear()}` : "";
  const width = Math.max(1, Math.min(12, Number(values.codePaddingWidth) || 8));
  const parsedStart = Number(values.codeStartingNumber);
  const startingNumber = values.codeStartingNumber?.trim() && Number.isFinite(parsedStart) ? Math.max(0, parsedStart) : 1;
  const sequence = String(startingNumber).padStart(width, "0");
  return `${prefix}${year}${separator}${sequence}`;
}

export const platformGroups = {
  notifications: [
    {
      title: "Notification code format",
      description: "Generate readable linked codes for automatic notifications and permanent notification history entries. Announcement codes are entered manually when alerts are created.",
      fields: [
        field("notificationCodePrefix", "Notification prefix", "Prefix for new notification records.", "text", { required: true }),
        field("historyCodePrefix", "Notification history prefix", "Prefix for new permanent notification lifecycle entries; this is separate from Record History.", "text", { required: true }),
        field("codeIncludeYear", "Include current year", "Place the current institute-local year before the sequence.", "toggle"),
        field("codeStartingNumber", "Starting number", "Lowest sequence considered for a newly selected prefix and format.", "number", { required: true, min: 0, max: 999999999999 }),
        field("codePaddingWidth", "Number padding", "Minimum digits used for each notification sequence.", "number", { required: true, min: 1, max: 12, unit: "digits" }),
        field("codeSeparator", "Separator", "Character placed between prefix, optional year, and sequence.", "select", { required: true, options: options("-", "/", ".") }),
        field("notificationCodeExample", "Notification example", "Preview for the next notification code.", "derived", { derive: values => notificationCodeExample(values, "notificationCodePrefix", "NOT") }),
        field("historyCodeExample", "Notification history example", "Preview for the next notification history code.", "derived", { derive: values => notificationCodeExample(values, "historyCodePrefix", "NHS") }),
      ],
    },
    {
      title: "Email delivery",
      description: "Public SMTP routing details. Credentials must remain in environment secrets and are never shown here.",
      fields: [
        field("emailEnabled", "Enable email", "Allow the notification service to send email.", "toggle"),
        field("smtpHost", "SMTP host", "Mail server host name.", "text", { required: true }),
        field("smtpPort", "SMTP port", "Mail server connection port.", "number", { required: true, min: 1, max: 65535 }),
        field("emailEncryption", "Encryption", "Transport security expected by the SMTP server.", "select", { required: true, options: options("TLS", "SSL", "None") }),
        field("senderName", "Sender name", "Display name used on institute email.", "text", { required: true }),
        field("senderEmail", "Sender email", "From address used on institute email.", "email", { required: true }),
      ],
    },
    {
      title: "SMS delivery",
      description: "Provider selection and public sender identity. Provider credentials remain outside the settings database.",
      fields: [
        field("smsEnabled", "Enable SMS", "Allow the notification service to send text messages.", "toggle"),
        field("smsProvider", "Provider", "Configured SMS integration provider.", "select", { required: true, options: options("None", "Twilio", "Custom") }),
        field("smsSenderId", "Sender ID", "Short sender identity supported by the provider.", "text"),
      ],
    },
    {
      title: "In-app notifications",
      description: "Choose which audiences receive notifications inside the application.",
      fields: [
        field("inAppEnabled", "Enable in-app notifications", "Master switch for application notifications.", "toggle"),
        field("studentNotifications", "Student notifications", "Deliver relevant messages to students when user access is available.", "toggle"),
        field("teacherNotifications", "Teacher notifications", "Deliver relevant messages to teachers when user access is available.", "toggle"),
        field("staffNotifications", "Staff notifications", "Deliver relevant messages to staff when user access is available.", "toggle"),
        field("administratorNotifications", "Administrator notifications", "Deliver operational messages to administrators.", "toggle"),
      ],
    },
    {
      title: "Notification templates",
      description: "Enable supplied template types. Editable subjects and bodies require a future template-record module.",
      fields: [field("enabledTemplates", "Enabled templates", "Select the notification templates available to workflows.", "checklist", { options: options("Student Enrollment Confirmation", "Course Enrollment", "Attendance Alert", "Grade Published", "Password Reset", "Account Created", "Announcement", "Academic Year Started") })],
    },
    {
      title: "Operational events",
      description: "Existing switches consumed by attendance, classrooms, grades, and daily recording workflows.",
      fields: [
        field("attendanceAlerts", "Attendance alerts", "Create Late and Absent event notifications.", "toggle"),
        field("deviceAlerts", "Device alerts", "Warn administrators when a classroom device goes offline.", "toggle"),
        field("gradeReminders", "Grade reminders", "Create support reminders for low grades.", "toggle"),
        field("dailySummary", "Daily summary", "Create a summary after completed class periods are recorded.", "toggle"),
      ],
    },
  ],
  system: [
    {
      title: "Language and regional display",
      description: "Localization metadata used by the shell, clock, dates, and browser language.",
      fields: [
        field("language", "Default language", "Primary application language metadata.", "select", { required: true, options: languageOptions }),
        field("availableLanguages", "Available languages", "Languages administrators may offer when translations are available.", "multiselect", { required: true, options: languageOptions }),
        field("dateFormat", "Date format", "Date layout shown in the application header.", "select", { required: true, options: options("DD/MM/YYYY", "DD MMM YYYY", "MM/DD/YYYY", "YYYY-MM-DD") }),
        field("timeFormat", "Time format", "Clock format used by the application.", "select", { required: true, options: options("24 Hour", "12 Hour") }),
        field("timeZone", "Runtime time zone", "IANA time zone used for schedules, live state, and academic rollover.", "select", { required: true, options: timeZoneOptions }),
        field("utcOffset", "Current UTC offset", "Derived from the selected time zone.", "derived", { derive: utcOffset }),
        field("firstDayOfWeek", "First day of week", "First day used by weekly calendars.", "select", { required: true, options: options("Monday", "Sunday", "Saturday") }),
        field("autoRefreshSeconds", "Operation refresh interval", "Frequency for reloading live Operation data.", "number", { required: true, min: 5, max: 3600, unit: "seconds" }),
      ],
    },
    {
      title: "Maintenance mode",
      description: "Prepare a clear maintenance state. Administrator bypass requires authenticated roles before it can be enforced.",
      fields: [
        field("maintenanceEnabled", "Enable maintenance mode", "Show the maintenance experience when backend enforcement is active.", "toggle"),
        field("maintenanceMessage", "Maintenance message", "Message displayed while maintenance mode is active.", "textarea", { required: true }),
        field("allowAdministratorsDuringMaintenance", "Allow administrators", "Permit authenticated administrators to continue during maintenance.", "toggle"),
      ],
    },
    {
      title: "System logs",
      description: "Logging categories and data-retention policy.",
      fields: [
        field("loggingEnabled", "Enable system logging", "Master switch for configured application logs.", "toggle"),
        field("loginLogs", "Login logs", "Record authentication activity when authentication exists.", "toggle"),
        field("userActivityLogs", "User activity logs", "Record user actions when identity is available.", "toggle"),
        field("securityLogs", "Security logs", "Record lockout, policy, and security events.", "toggle"),
        field("configurationChangeLogs", "Configuration change logs", "Record Settings changes in audit history.", "toggle"),
        field("logRetentionDays", "Retention period", "Number of days application logs are retained.", "number", { required: true, min: 1, max: 3650, unit: "days" }),
      ],
    },
  ],
  security: [
    {
      title: "Password policy",
      description: "Policy values are stored now; enforcement requires an authentication provider.",
      fields: [
        field("passwordMinimumLength", "Minimum length", "Minimum accepted password length.", "number", { required: true, min: 6, max: 128, unit: "characters" }),
        field("requireUppercase", "Require uppercase", "Require at least one uppercase letter.", "toggle"),
        field("requireLowercase", "Require lowercase", "Require at least one lowercase letter.", "toggle"),
        field("requireNumber", "Require number", "Require at least one numeric character.", "toggle"),
        field("requireSpecialCharacter", "Require special character", "Require at least one symbol.", "toggle"),
        field("passwordExpirationDays", "Password expiration", "Number of days before password renewal is required; zero can represent no expiry.", "number", { required: true, min: 0, max: 3650, unit: "days" }),
        field("preventPasswordReuse", "Prevent password reuse", "Number of previous passwords blocked from reuse.", "number", { required: true, min: 0, max: 100, unit: "passwords" }),
      ],
    },
    {
      title: "Session timeout",
      description: "Role-specific idle-session durations for a future authenticated application.",
      fields: [
        field("administratorSessionMinutes", "Administrator session", "Administrator idle timeout.", "number", { required: true, min: 5, max: 10080, unit: "minutes" }),
        field("staffSessionMinutes", "Staff session", "Staff idle timeout.", "number", { required: true, min: 5, max: 10080, unit: "minutes" }),
        field("teacherSessionMinutes", "Teacher session", "Teacher idle timeout.", "number", { required: true, min: 5, max: 10080, unit: "minutes" }),
        field("studentSessionMinutes", "Student session", "Student idle timeout.", "number", { required: true, min: 5, max: 10080, unit: "minutes" }),
        field("rememberMeEnabled", "Allow Remember Me", "Allow persistent sessions when authentication supports them.", "toggle"),
      ],
    },
    {
      title: "Login attempts",
      description: "Lockout policy for repeated failed authentication.",
      fields: [
        field("maximumLoginAttempts", "Maximum attempts", "Failed attempts allowed before lockout.", "number", { required: true, min: 1, max: 100 }),
        field("lockoutDurationMinutes", "Lockout duration", "How long a locked account remains unavailable.", "number", { required: true, min: 1, max: 10080, unit: "minutes" }),
        field("resetAttemptCounterMinutes", "Reset counter after", "Time without a failure before the counter resets.", "number", { required: true, min: 1, max: 10080, unit: "minutes" }),
        field("logFailedAttempts", "Log failed attempts", "Write failed authentication attempts to security logs.", "toggle"),
      ],
    },
    {
      title: "Two-factor authentication",
      description: "Desired two-factor policy. It cannot be enforced until identity and authentication are implemented.",
      fields: [
        field("twoFactorMode", "Institute default", "Default two-factor requirement.", "select", { required: true, options: twoFactorOptions }),
        field("administratorTwoFactor", "Administrator", "Two-factor requirement for administrators.", "select", { required: true, options: twoFactorOptions }),
        field("staffTwoFactor", "Staff", "Two-factor requirement for staff.", "select", { required: true, options: twoFactorOptions }),
        field("teacherTwoFactor", "Teacher", "Two-factor requirement for teachers.", "select", { required: true, options: twoFactorOptions }),
        field("studentTwoFactor", "Student", "Two-factor requirement for students.", "select", { required: true, options: twoFactorOptions }),
        field("twoFactorMethods", "Available methods", "Supported verification methods.", "checklist", { required: true, options: options("Authenticator App", "Email OTP") }),
      ],
    },
  ],
} satisfies Partial<Record<SettingSection, readonly ConfigurationGroup[]>>;
