import type { ConfigurationGroup, ManagementLink, SettingSection } from "../administration-types";
import { field, labelledOptions, options } from "./schema-helpers";

const userStatuses = options("Active", "Inactive", "Suspended", "Pending", "Locked");
const roles = options("Super Administrator", "Administrator", "Teacher", "Staff", "Student");
const studentStatuses = options("Applicant", "Active", "Inactive", "Suspended", "Graduated", "Withdrawn", "Expelled");
const teacherStatuses = options("Active", "Inactive", "On Leave", "Suspended", "Terminated");

const requiredStudentInformation = labelledOptions([
  ["fullName", "Full name"],
  ["dateOfBirth", "Date of birth"],
  ["gender", "Gender"],
  ["phone", "Phone"],
  ["email", "Email"],
  ["address", "Address"],
  ["emergencyContact", "Emergency contact"],
  ["profilePhoto", "Profile photo"],
  ["identificationDocument", "Identification document"],
  ["previousEducation", "Previous education"],
]);

const permissions = options(
  "Dashboard",
  "View Students", "Create Students", "Edit Students", "Delete Students",
  "View Teachers", "Create Teachers", "Edit Teachers", "Delete Teachers",
  "View Courses", "Create Courses", "Edit Courses", "Delete Courses",
  "View Attendance", "Create Attendance", "Edit Attendance",
  "View Grades", "Create Grades", "Edit Grades", "Publish Grades",
  "Manage Users", "Manage Roles", "Manage Settings", "View System Logs",
);

export const peopleAccessGroups = {
  "users-access": [
    {
      title: "User lifecycle defaults",
      description: "Defaults for a future identity module. These settings do not create accounts or grant access by themselves.",
      fields: [
        field("defaultUserStatus", "Default user status", "Initial status proposed when a future user account is created.", "select", { required: true, options: userStatuses }),
        field("userStatuses", "Available user statuses", "Lifecycle states administrators may use.", "multiselect", { required: true, options: userStatuses }),
      ],
    },
    {
      title: "Role and permission catalog",
      description: "Reference catalogs only. Enforceable assignments require authentication, users, roles, and permission records.",
      fields: [
        field("availableRoles", "Available roles", "Roles expected by the supplied institute structure.", "multiselect", { required: true, options: roles }),
        field("permissionCatalog", "Permission catalog", "Capabilities available for future role-to-permission assignment.", "checklist", { required: true, options: permissions }),
      ],
    },
  ],
  "student-rules": [
    {
      title: "Enrollment rules",
      description: "Requirements applied to student enrollment workflows.",
      fields: [
        field("requireApplication", "Require an application", "Require an application before enrollment.", "toggle"),
        field("requireDocuments", "Require supporting documents", "Require document evidence before enrollment approval.", "toggle"),
        field("allowLateEnrollment", "Allow late enrollment", "Allow enrollment after the standard closing date.", "toggle"),
        field("lateEnrollmentDays", "Late enrollment period", "Number of days the late window stays open.", "number", { required: true, min: 0, max: 365, unit: "days" }),
        field("requireEnrollmentApproval", "Require enrollment approval", "Require an administrator to approve enrollment.", "toggle"),
        field("maximumCoursesPerSemester", "Maximum courses per semester", "Maximum active course assignments per student and semester.", "number", { required: true, min: 1, max: 50, unit: "courses" }),
      ],
    },
    {
      title: "Student statuses",
      description: "Lifecycle states available to student records.",
      fields: [field("statuses", "Available statuses", "Select every status administrators may assign.", "multiselect", { required: true, options: studentStatuses })],
    },
    {
      title: "Required student information",
      description: "Stable field codes that must be supplied before a student record is considered complete.",
      fields: [field("requiredInformation", "Required fields", "Choose the profile evidence required by institute policy.", "checklist", { required: true, options: requiredStudentInformation })],
    },
  ],
  "teacher-rules": [
    {
      title: "Teacher statuses",
      description: "Lifecycle states available to teacher records.",
      fields: [field("statuses", "Available statuses", "Select every status administrators may assign.", "multiselect", { required: true, options: teacherStatuses })],
    },
    {
      title: "Assignment rules",
      description: "Workload and relationship requirements applied during teaching assignments.",
      fields: [
        field("maximumCourses", "Maximum courses", "Maximum active courses assigned to one teacher.", "number", { required: true, min: 1, max: 50, unit: "courses" }),
        field("maximumClasses", "Maximum classes", "Maximum active class assignments for one teacher.", "number", { required: true, min: 1, max: 100, unit: "classes" }),
        field("allowMultipleDepartments", "Allow multiple departments", "Allow a teacher to work with more than one department.", "toggle"),
        field("requireDepartmentAssignment", "Require department assignment", "Require a primary department before activation.", "toggle"),
        field("requireCourseAssignment", "Require course assignment", "Require at least one course relationship before activation.", "toggle"),
      ],
    },
  ],
} satisfies Partial<Record<SettingSection, readonly ConfigurationGroup[]>>;

export const peopleAccessLinks: Partial<Record<SettingSection, readonly ManagementLink[]>> = {
  "users-access": [
    { title: "Users", description: "Account records are deferred until an authentication module is introduced.", label: "Identity module deferred" },
    { title: "Roles and permissions", description: "Role assignment cannot be enforced without authentication and authorization.", label: "Access module deferred" },
  ],
};
