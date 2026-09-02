import { settingSections, type AdministrationCategory, type AdministrationSectionDefinition, type ConfigurationGroup, type SettingFieldDefinition, type SettingSection } from "./administration-types";
import { parseCsv } from "./settings-codec";
import { organizationAcademicGroups, organizationAcademicLinks } from "./schema/organization-academic-schema";
import { peopleAccessGroups, peopleAccessLinks } from "./schema/people-access-schema";
import { platformGroups } from "./schema/platform-schema";
import { policyGroups } from "./schema/policy-schema";
import { recordCodeExample } from "./schema/schema-helpers";

export const administrationCategories: ReadonlyArray<{ id: AdministrationCategory; title: string; description: string }> = [
  { id: "general", title: "General", description: "Institute identity, branding, contact, address, and regional profile." },
  { id: "academic", title: "Academic", description: "Academic calendar and defaults for record-backed academic resources." },
  { id: "access", title: "Users & access", description: "Future account lifecycle, roles, statuses, and permission catalog." },
  { id: "people", title: "Students & teachers", description: "Identifiers, enrollment, status, and assignment policies." },
  { id: "policies", title: "Attendance & grading", description: "Institute-wide academic rules used by live workflows." },
  { id: "platform", title: "Communication, system & security", description: "Delivery, localization, maintenance, logging, and access policy." },
];

const groups = {
  ...organizationAcademicGroups,
  ...peopleAccessGroups,
  ...policyGroups,
  ...platformGroups,
} as Record<SettingSection, readonly ConfigurationGroup[]>;

const simpleSettingKeys: Record<SettingSection, readonly string[]> = {
  institute: ["name", "shortName", "code", "logoUrl", "email", "phone", "address"],
  "academic-year": ["currentYear", "code", "startsOn", "endsOn", "status"],
  semester: ["currentTerm"],
  departments: ["codePrefix", "codeIncludeYear", "codePaddingWidth", "codeSeparator", "codeExample", "defaultStatus", "requireDepartmentHead", "allowCrossDepartmentTeaching"],
  courses: ["codePrefix", "codeIncludeYear", "codePaddingWidth", "codeSeparator", "codeExample", "defaultCapacity", "requireAssignedTeacher"],
  classrooms: ["codePrefix", "codeIncludeYear", "codePaddingWidth", "codeSeparator", "codeExample", "defaultCapacity", "attendanceDeviceRequired"],
  "users-access": ["defaultUserStatus", "availableRoles"],
  "student-rules": ["idPrefix", "includeYear", "startingNumber", "paddingWidth", "separator", "identifierExample", "maximumCoursesPerSemester", "statuses"],
  "teacher-rules": ["idPrefix", "includeYear", "startingNumber", "paddingWidth", "separator", "identifierExample", "statuses", "maximumCourses", "maximumClasses"],
  "attendance-rules": ["method", "attendanceRequired", "lateThresholdMinutes", "absentAfterMinutes", "autoAbsent", "teacherCanRecord", "notifyAdministrator"],
  "grade-rules": ["gradingSystem", "maximumScore", "passMark", "gpaEnabled", "overallPassMark", "coursePassMark"],
  notifications: ["notificationCodePrefix", "announcementCodePrefix", "historyCodePrefix", "codeIncludeYear", "codeStartingNumber", "codePaddingWidth", "codeSeparator", "notificationCodeExample", "announcementCodeExample", "historyCodeExample", "emailEnabled", "inAppEnabled", "attendanceAlerts", "deviceAlerts", "gradeReminders", "dailySummary"],
  system: ["language", "dateFormat", "timeFormat", "timeZone", "autoRefreshSeconds"],
  security: ["passwordMinimumLength", "maximumLoginAttempts", "lockoutDurationMinutes", "twoFactorMode"],
};

export const administrationSections: readonly AdministrationSectionDefinition[] = [
  section("institute", "General settings", "General", "Institute identity, branding, contact details, and address.", "general", "building"),
  section("academic-year", "Academic year", "Academic year", "Active academic-year identity, dates, and lifecycle status.", "academic", "calendar"),
  section("semester", "Semester and term", "Terms", "Current term plus Semester 1, Semester 2, and Summer Term windows.", "academic", "calendar"),
  section("departments", "Department rules", "Departments", "Code generation, defaults, and governance rules; department records remain in Management.", "academic", "building"),
  section("courses", "Course rules", "Courses", "Code generation, defaults, and assignment requirements; course records remain in Management.", "academic", "book"),
  section("classrooms", "Classroom rules", "Classrooms", "Code generation and learning-space defaults; classroom records remain in Management.", "academic", "room"),
  section("users-access", "Users and access", "Users & access", "Future account statuses, roles, and permission catalog without fake user records.", "access", "users"),
  section("student-rules", "Student settings", "Students", "Student identifiers, enrollment rules, statuses, and required information.", "people", "users"),
  section("teacher-rules", "Teacher settings", "Teachers", "Teacher identifiers, statuses, workloads, and assignment requirements.", "people", "teacher"),
  section("attendance-rules", "Attendance settings", "Attendance", "Capture, threshold, absence, correction, audit, and alert rules.", "policies", "check"),
  section("grade-rules", "Grading settings", "Grading", "Percentage, A+ through F boundaries, pass rules, and GPA behavior.", "policies", "grade"),
  section("notifications", "Notification settings", "Notifications", "Email, SMS, in-app audiences, templates, and operational events.", "platform", "bell"),
  section("system", "System settings", "System", "Localization, time, live refresh, and logging policy.", "platform", "settings"),
  section("security", "Security policy", "Security", "Password, session, lockout, and two-factor policy readiness.", "platform", "archive"),
];

export function isSettingSection(value: string): value is SettingSection {
  return settingSections.includes(value as SettingSection);
}

export function sectionDefinition(sectionName: SettingSection) {
  return administrationSections.find(item => item.section === sectionName) ?? administrationSections[0];
}

export function sectionFields(sectionName: SettingSection) {
  return sectionDefinition(sectionName).groups.flatMap(group => group.fields);
}

export function editableSectionFields(sectionName: SettingSection) {
  return sectionFields(sectionName).filter(fieldDefinition => fieldDefinition.type !== "derived" && !fieldDefinition.readOnly);
}

export function simpleSectionFieldKeys(sectionName: SettingSection, values: Record<string, string>) {
  if (sectionName !== "semester") return simpleSettingKeys[sectionName];
  const prefix = values.currentTerm === "Semester 2" ? "semester2" : values.currentTerm === "Summer Term" ? "summer" : "semester1";
  return ["currentTerm", `${prefix}StartsOn`, `${prefix}EndsOn`, `${prefix}Status`];
}

export function fieldDefinition(sectionName: SettingSection, key: string): SettingFieldDefinition | undefined {
  return sectionFields(sectionName).find(item => item.key === key);
}

export function configurationSummary(sectionName: SettingSection, values: Record<string, string>) {
  if (sectionName === "institute") return `${values.shortName || values.code || "Institute"} · ${values.city || values.country || "Address required"}`;
  if (sectionName === "academic-year") return `${values.currentYear || "Year required"} · ${values.status || "Status required"}`;
  if (sectionName === "semester") return `${values.currentTerm || "Term required"} · ${values.startsOn || "Start date required"}`;
  if (sectionName === "departments") return `${recordCodeExample(values)} · ${values.requireDepartmentHead === "true" ? "Head required" : "Head optional"}`;
  if (sectionName === "courses") return `${recordCodeExample(values)} · ${values.defaultCapacity || "–"} default seats`;
  if (sectionName === "classrooms") return `${recordCodeExample(values)} · ${values.defaultCapacity || "–"} default seats`;
  if (sectionName === "users-access") return `${parseCsv(values.availableRoles).length} roles · ${parseCsv(values.permissionCatalog).length} permissions`;
  if (sectionName === "student-rules") return `${values.idPrefix || "STU"} identifiers · ${values.maximumCoursesPerSemester || "–"} courses per term`;
  if (sectionName === "teacher-rules") return `${values.idPrefix || "TCH"} identifiers · ${values.maximumCourses || "–"} courses maximum`;
  if (sectionName === "attendance-rules") return `${values.method || "Method required"} · late from ${values.lateThresholdMinutes || "0"} minutes`;
  if (sectionName === "grade-rules") return `A+ from ${values.aPlusMinimum || "–"} · pass mark ${values.passMark || "–"}%`;
  if (sectionName === "notifications") return `${values.emailEnabled === "true" ? "Email on" : "Email off"} · ${parseCsv(values.enabledTemplates).length} templates`;
  if (sectionName === "system") return `${values.language || "Language required"} · ${(values.timeZone || "Time zone required").replaceAll("_", " ")}`;
  return `${values.passwordMinimumLength || "–"}+ character passwords · ${values.twoFactorMode || "2FA policy required"}`;
}

function section(
  sectionName: SettingSection,
  title: string,
  shortTitle: string,
  description: string,
  category: AdministrationCategory,
  icon: AdministrationSectionDefinition["icon"],
): AdministrationSectionDefinition {
  return {
    section: sectionName,
    title,
    shortTitle,
    description,
    category,
    icon,
    groups: groups[sectionName],
    managementLinks: organizationAcademicLinks[sectionName] ?? peopleAccessLinks[sectionName],
  };
}
