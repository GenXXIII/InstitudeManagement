import type { ConfigurationGroup, ManagementLink, SettingSection } from "../administration-types";
import { field, options, recordCodeExample, utcOffset } from "./schema-helpers";

const statusOptions = options("Active", "Upcoming", "Inactive", "Completed");
const timeZoneOptions = options("Asia/Phnom_Penh", "Asia/Bangkok", "Asia/Ho_Chi_Minh", "UTC");

export const organizationAcademicGroups = {
  institute: [
    {
      title: "Institute information",
      description: "The official identity shown across the management system.",
      fields: [
        field("name", "Institute name", "Displayed in the sidebar, page title, and institute profile.", "text", { required: true }),
        field("shortName", "Short name", "A compact name used when space is limited.", "text", { required: true }),
        field("code", "Institute code", "A stable uppercase code used on official references.", "text", { required: true }),
        field("establishedYear", "Established year", "The four-digit year the institute was established.", "number", { required: true, min: 1800, max: 2200 }),
        field("description", "Description", "A concise public description of the institute.", "textarea", { required: true }),
      ],
    },
    {
      title: "Branding",
      description: "Store a URL or application path. The current INK artwork remains the fallback if a file cannot be loaded.",
      fields: [
        field("logoUrl", "Institute logo", "Use an existing image URL or a path such as /uploads/settings/institute-logo.png.", "asset", { accept: "image/png,image/jpeg,image/webp,image/svg+xml" }),
        field("faviconUrl", "Browser favicon", "Use an ICO, PNG, or SVG URL/path. No image generation or automatic redrawing is performed.", "asset", { accept: "image/x-icon,image/png,image/svg+xml" }),
      ],
    },
    {
      title: "Contact information",
      description: "Public contact details shown in the institute profile.",
      fields: [
        field("email", "Email", "Main public email address.", "email", { required: true }),
        field("phone", "Phone", "Institute landline or primary office number.", "tel", { required: true }),
        field("mobile", "Mobile", "Institute mobile contact number.", "tel"),
        field("website", "Website", "Full public website URL, including https://.", "url"),
      ],
    },
    {
      title: "Address",
      description: "Structured location data used on institute documents and profiles.",
      fields: [
        field("country", "Country", "Country name.", "text", { required: true }),
        field("city", "City", "City or municipality.", "text", { required: true }),
        field("province", "Province", "Province or administrative region.", "text", { required: true }),
        field("district", "District", "District or khan.", "text"),
        field("address", "Street address", "Complete delivery and visitor address.", "textarea", { required: true }),
        field("postalCode", "Postal code", "Postal or ZIP code.", "text"),
      ],
    },
    {
      title: "Regional time",
      description: "Profile time-zone metadata. Runtime scheduling uses the matching value under System preferences.",
      fields: [
        field("timeZone", "Time zone", "Use an IANA time-zone identifier.", "select", { required: true, options: timeZoneOptions }),
        field("utcOffset", "Current UTC offset", "Derived from the selected time zone and current daylight-saving rules.", "derived", { derive: utcOffset }),
      ],
    },
  ],
  "academic-year": [
    {
      title: "Active academic year",
      description: "The academic window applied to enrollment, attendance, grades, dashboards, and records.",
      fields: [
        field("currentYear", "Academic year name", "For example, 2026–2027.", "text", { required: true }),
        field("code", "Academic year code", "Stable business code for the year.", "text", { required: true }),
        field("startsOn", "Start date", "First day of the academic year.", "date", { required: true }),
        field("endsOn", "End date", "Final day of the academic year.", "date", { required: true }),
        field("status", "Status", "Current lifecycle state.", "select", { required: true, options: statusOptions }),
      ],
    },
  ],
  semester: [
    {
      title: "Current term",
      description: "Select the term currently receiving attendance, grades, and records.",
      fields: [field("currentTerm", "Active term", "Its dates are copied to the current runtime window when settings are applied.", "select", { required: true, options: options("Semester 1", "Semester 2", "Summer Term") })],
    },
    {
      title: "Semester 1",
      description: "Primary first-semester record and date window.",
      fields: termFields("semester1", "Semester 1"),
    },
    {
      title: "Semester 2",
      description: "Primary second-semester record and date window.",
      fields: termFields("semester2", "Semester 2"),
    },
    {
      title: "Summer term",
      description: "Optional summer teaching window at the end of the academic year.",
      fields: termFields("summer", "Summer Term"),
    },
  ],
  departments: [
    codeFormatGroup("Department", "DEP"),
    {
      title: "Department defaults",
      description: "Rules applied when real department records and teaching relationships are saved.",
      fields: [
        field("defaultStatus", "New department status", "Initial status for a newly created department.", "select", { required: true, options: options("Active", "Inactive") }),
        field("requireDepartmentHead", "Require a department head", "Block a department from becoming active until a head teacher is assigned.", "toggle"),
        field("allowCrossDepartmentTeaching", "Allow cross-department teaching", "Allow teachers to be assigned outside their primary department.", "toggle"),
      ],
    },
  ],
  courses: [
    codeFormatGroup("Course", "CRS"),
    {
      title: "Course defaults",
      description: "Defaults and requirements used by course management and enrollment.",
      fields: [
        field("defaultCapacity", "Default course capacity", "Pre-filled seat capacity for new course assignments.", "number", { required: true, min: 1, max: 10000, unit: "students" }),
        field("requireAssignedTeacher", "Require an assigned teacher", "Block an active course assignment without a teacher.", "toggle"),
      ],
    },
  ],
  classrooms: [
    codeFormatGroup("Classroom", "ROOM"),
    {
      title: "Classroom defaults",
      description: "Defaults and safeguards for institute learning spaces.",
      fields: [
        field("defaultCapacity", "Default classroom capacity", "Pre-filled capacity for a newly created learning space.", "number", { required: true, min: 1, max: 10000, unit: "seats" }),
        field("attendanceDeviceRequired", "Require an online attendance device", "Require a device to be online before a classroom is treated as operational.", "toggle"),
      ],
    },
  ],
} satisfies Partial<Record<SettingSection, readonly ConfigurationGroup[]>>;

export const organizationAcademicLinks: Partial<Record<SettingSection, readonly ManagementLink[]>> = {
  departments: [
    { title: "Departments", description: "Create, edit, and deactivate real department records.", href: "/management/departments", label: "Manage departments" },
    { title: "Programs", description: "Program records require a future program management module.", label: "Record module deferred" },
  ],
  courses: [{ title: "Courses", description: "Maintain real course codes and names in Academic Management.", href: "/management/courses", label: "Manage courses" }],
  classrooms: [{ title: "Classrooms", description: "Maintain real rooms, buildings, types, capacities, and devices.", href: "/management/classrooms", label: "Manage classrooms" }],
};

function termFields(prefix: string, term: string) {
  return [
    field(`${prefix}Name`, "Name", `Display name for ${term}.`, "text", { required: true }),
    field(`${prefix}Code`, "Code", `Stable business code for ${term}.`, "text", { required: true }),
    field(`${prefix}StartsOn`, "Start date", `First day of ${term}.`, "date", { required: true }),
    field(`${prefix}EndsOn`, "End date", `Final day of ${term}.`, "date", { required: true }),
    field(`${prefix}Status`, "Status", `Lifecycle state for ${term}.`, "select", { required: true, options: statusOptions }),
  ];
}

function codeFormatGroup(resource: string, samplePrefix: string): ConfigurationGroup {
  return {
    title: `${resource} code format`,
    description: `Generate the next ${resource.toLowerCase()} code when its code field is left blank during creation. Manual codes remain supported.`,
    fields: [
      field("codePrefix", "Prefix", `Uppercase prefix such as ${samplePrefix}.`, "text", { required: true }),
      field("codeIncludeYear", "Include current year", "Place the current local year between the prefix and sequence.", "toggle"),
      field("codeStartingNumber", "Starting number", "Lowest sequence number considered when generating a new code.", "number", { required: true, min: 0, max: 999999999999 }),
      field("codePaddingWidth", "Number padding", "Minimum number of digits in the generated sequence.", "number", { required: true, min: 1, max: 12, unit: "digits" }),
      field("codeSeparator", "Separator", "Character placed between code parts.", "select", { required: true, options: options("-", "/", ".") }),
      field("codeExample", "Next-code example", "Preview generated from the current format.", "derived", { derive: recordCodeExample }),
    ],
  };
}
