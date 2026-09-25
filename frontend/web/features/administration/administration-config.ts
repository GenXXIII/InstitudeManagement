import { settingSections, type AdministrationCategory, type AdministrationSectionDefinition, type ConfigurationGroup, type SettingFieldDefinition, type SettingSection } from "./administration-types";
import { parseCsv } from "./settings-codec";
import { organizationAcademicGroups, organizationAcademicLinks } from "./schema/organization-academic-schema";
import { peopleAccessGroups, peopleAccessLinks } from "./schema/people-access-schema";
import { platformGroups } from "./schema/platform-schema";
import { policyGroups } from "./schema/policy-schema";
import { codeFormatGroups } from "./schema/code-format-schema";

export const administrationCategories: ReadonlyArray<{ id: AdministrationCategory; title: string; description: string }> = [
  { id: "essential", title: "1. Essential institute setup", description: "Institute identity, branding, contact details, and location." },
  { id: "calendar", title: "2. Academic calendar", description: "Current academic year, semester, term windows, and lifecycle dates." },
  { id: "finance", title: "3. Finance", description: "Fees, payment timing, QR providers, and advancement eligibility rules." },
  { id: "structure", title: "4. Academic structure rules", description: "Defaults and governance for departments, courses, and classrooms." },
  { id: "people", title: "5. People and access", description: "Student, teacher, role, status, and workload policies." },
  { id: "policies", title: "6. Attendance and grading", description: "Attendance outcomes, academic weights, thresholds, and grade boundaries." },
  { id: "communication", title: "7. Communication", description: "Notification channels, audiences, templates, and operational events." },
  { id: "platform", title: "8. Advanced platform controls", description: "Code formats, localization, maintenance, logging, and security policy." },
];

const groups = {
  ...organizationAcademicGroups,
  ...peopleAccessGroups,
  ...policyGroups,
  ...platformGroups,
  "code-formats": codeFormatGroups,
} as Record<SettingSection, readonly ConfigurationGroup[]>;

const simpleSettingKeys: Record<SettingSection, readonly string[]> = {
  institute: ["name", "shortName", "code", "logoUrl", "email", "phone", "address"],
  "academic-year": ["currentYear", "code", "startsOn", "endsOn", "status"],
  semester: ["currentTerm"],
  finance: ["semesterPrice", "otherFee", "paymentDueDays", "latePenaltyPerDay", "bakongEnabled", "bakongEnvironment", "bakongAccountId", "bakongAccountInformation", "bakongAcquiringBank", "bakongMerchantName", "bakongMerchantCity", "mockPaymentEnabled", "allowPartialPayments", "allowOverpayment", "maximumAdjustmentAmount", "requirePaidForAdvancement"],
  departments: ["defaultStatus", "requireDepartmentHead", "allowCrossDepartmentTeaching"],
  courses: ["defaultCapacity", "requireAssignedTeacher"],
  classrooms: ["defaultCapacity", "attendanceDeviceRequired"],
  "code-formats": ["codeIncludeYear", "codeStartingNumber", "codePaddingWidth", "codeSeparator", "studentManagementPrefix", "studentEnrollmentPrefix", "studentOperationPrefix", "studentRecordPrefix", "studentHistoryPrefix", "financeCodePrefix", "resultCodePrefix", "studentPublicIdPrefix", "teacherPublicIdPrefix", "alertCodePrefix", "alertCodeExample", "notificationCodePrefix", "notificationCodeExample", "historyCodePrefix", "historyCodeExample"],
  "users-access": ["defaultUserStatus", "availableRoles"],
  "student-rules": ["maximumCoursesPerSemester", "statuses"],
  "teacher-rules": ["statuses", "maximumCourses", "maximumClasses"],
  "attendance-rules": ["method", "attendanceRequired", "lateThresholdMinutes", "absentAfterMinutes", "autoAbsent", "absentScoreDeduction", "permissionScoreDeduction", "retakeAbsentSections", "failAbsentSections", "retakePermissionSections", "failPermissionSections", "notifyAdministrator"],
  "grade-rules": ["gradingSystem", "attendanceWeight", "assignmentWeight", "midtermWeight", "finalExamWeight", "expectedCourseCount", "semesterCalculation", "passMark", "gpaEnabled"],
  notifications: ["emailEnabled", "inAppEnabled", "attendanceAlerts", "deviceAlerts", "gradeReminders", "dailySummary"],
  system: ["language", "dateFormat", "timeFormat", "timeZone", "autoRefreshSeconds"],
  security: ["passwordMinimumLength", "maximumLoginAttempts", "lockoutDurationMinutes", "twoFactorMode"],
};

export const administrationSections: readonly AdministrationSectionDefinition[] = [
  section("institute", "General settings", "General", "Institute identity, branding, contact details, and address.", "essential", "building"),
  section("academic-year", "Academic year", "Academic year", "Active academic-year identity, dates, and lifecycle status.", "calendar", "calendar"),
  section("semester", "Semester and term", "Terms", "Current term plus Semester 1, Semester 2, and Summer Term windows.", "calendar", "calendar"),
  section("finance", "Finance settings", "Finance", "Configure fee defaults, declaration expiry, and financial rules used by Finance and the enrollment eligibility gate.", "finance", "finance"),
  section("departments", "Department rules", "Departments", "Defaults and governance rules; a DepartmentCode sequence is required during creation and remains permanent.", "structure", "building"),
  section("courses", "Course rules", "Courses", "Defaults and assignment requirements; a CourseCode sequence is required during creation and remains permanent.", "structure", "book"),
  section("classrooms", "Classroom rules", "Classrooms", "Learning-space defaults; a ClassroomCode sequence is required during creation and remains permanent.", "structure", "room"),
  section("code-formats", "Code formats", "Codes", "Configure formatting for assigned Management and Alert codes plus automatic Notification, Record, and History codes.", "platform", "settings"),
  section("users-access", "Users and access", "Users & access", "Future account statuses, roles, and permission catalog without fake user records.", "people", "users"),
  section("student-rules", "Student settings", "Students", "Enrollment rules, statuses, and required information; a unique StudentCode sequence is required.", "people", "users"),
  section("teacher-rules", "Teacher settings", "Teachers", "Statuses, workloads, and assignment requirements; a unique TeacherCode sequence is required.", "people", "teacher"),
  section("attendance-rules", "Attendance settings", "Attendance", "Capture, score deductions, Retake and Fail thresholds, correction, audit, and alert rules.", "policies", "check"),
  section("grade-rules", "Result settings", "Results", "Configurable attendance, assessment, and exam weights with semester course-count and grade rules.", "policies", "grade"),
  section("notifications", "Notification settings", "Notifications", "Email, SMS, in-app audiences, templates, and operational events.", "communication", "bell"),
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
  if (sectionName === "finance") return `${values.semesterPrice || "0"} per semester · expires ${values.paymentDueDays || "0"} days after declaration`;
  if (sectionName === "departments") return `Assigned DepartmentCode · ${values.requireDepartmentHead === "true" ? "Head required" : "Head optional"}`;
  if (sectionName === "courses") return `Assigned CourseCode · ${values.defaultCapacity || "–"} default seats`;
  if (sectionName === "classrooms") return `Assigned ClassroomCode · ${values.defaultCapacity || "–"} default seats`;
  if (sectionName === "code-formats") return `${values.studentManagementPrefix || "STU"}-1 → ${values.studentEnrollmentPrefix || "ENR"}-1-${values.studentManagementPrefix || "STU"}-1 · Alert ${values.alertCodePrefix || "ALT"} · Notification ${values.notificationCodePrefix || "NOT"}`;
  if (sectionName === "users-access") return `${parseCsv(values.availableRoles).length} roles · ${parseCsv(values.permissionCatalog).length} permissions`;
  if (sectionName === "student-rules") return `Assigned StudentCode · ${values.maximumCoursesPerSemester || "–"} courses per term`;
  if (sectionName === "teacher-rules") return `Assigned TeacherCode · ${values.maximumCourses || "–"} courses maximum`;
  if (sectionName === "attendance-rules") return `${values.absentScoreDeduction || "2"} absent / ${values.permissionScoreDeduction || "0.91"} permission deduction · Retake at ${values.retakeAbsentSections || "6"} absent`;
  if (sectionName === "grade-rules") return `${values.attendanceWeight || "10"}/${values.assignmentWeight || "20"}/${values.midtermWeight || "20"}/${values.finalExamWeight || "50"}% · ${values.expectedCourseCount || "5"} courses`;
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
