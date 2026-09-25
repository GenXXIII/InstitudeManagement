import type { ConfigurationGroup, ManagementLink, SettingSection } from "../administration-types";
import { field, options } from "./schema-helpers";

const statusOptions = options("Active", "Upcoming", "Inactive", "Completed");

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
  finance: [
    {
      title: "Payment prices",
      description: "Default semester price used when Finance declares a student payment.",
      fields: [
        field("semesterPrice", "Semester payment", "Default tuition charged for one enrolled semester.", "number", { required: true, min: 0, max: 1000000, step: 0.01 }),
        field("otherFee", "Other fee", "Default non-tuition charge added to the selected payment price.", "number", { required: true, min: 0, max: 1000000, step: 0.01 }),
        field("paymentDueDays", "Payment expires after", "Number of days in the payment countdown. The countdown starts when Finance declares the payment to students.", "number", { required: true, min: 0, max: 365, unit: "days" }),
        field("latePenaltyPerDay", "Punishment per missed day", "Extra amount charged for every calendar day after the payment expires.", "number", { required: true, min: 0, max: 1000000, step: 0.01, unit: "per day" }),
      ],
    },
    {
      title: "Financial rules",
      description: "Rules Finance applies to payments, adjustments, balances, and enrollment eligibility.",
      fields: [
        field("allowPartialPayments", "Allow partial payments", "Permit a payment smaller than the current account balance.", "toggle"),
        field("allowOverpayment", "Allow overpayment", "Permit a payment larger than the current account balance.", "toggle"),
        field("maximumAdjustmentAmount", "Maximum adjustment", "Maximum absolute discount or extra charge allowed on one account.", "number", { required: true, min: 0, max: 1000000, step: 0.01 }),
        field("requirePaidForAdvancement", "Require Paid to advance", "Enrollment may advance only when Finance reports the account as Paid.", "toggle"),
      ],
    },
    {
      title: "Personal Bakong QR",
      description: "Use an individual Bakong app account without merchant registration. Each student gets a payment-specific QR with the exact amount and reference, and Finance verifies the transfer through the Bakong API. The API token remains private in BAKONG_API_TOKEN.",
      fields: [
        field("bakongEnabled", "Enable personal Bakong QR", "Allow students to generate dynamic KHQR payments to an individual Bakong account.", "toggle"),
        field("bakongEnvironment", "Bakong environment", "Use Production for a real Bakong app account and real $0.01 transfer. SIT works only with sandbox accounts such as @devb.", "select", { required: true, options: options("SIT", "Production"), showWhen: values => values.bakongEnabled === "true" }),
        field("bakongAccountId", "Personal Bakong ID", "The receiver ID shown in the Bakong app or personal KHQR, usually in the form name@bank.", "text", { showWhen: values => values.bakongEnabled === "true" }),
        field("bakongMerchantName", "Account holder name", "Personal account name encoded into the QR and displayed in the payment popup.", "text", { showWhen: values => values.bakongEnabled === "true" }),
        field("bakongAccountInformation", "Account number or phone", "Optional personal account number or phone number encoded in KHQR.", "text", { showWhen: values => values.bakongEnabled === "true" }),
        field("bakongAcquiringBank", "Bank name", "Optional receiving bank name displayed with the personal Bakong payment.", "text", { showWhen: values => values.bakongEnabled === "true" }),
        field("bakongMerchantCity", "City", "Optional KHQR city. Phnom Penh is used when blank.", "text", { showWhen: values => values.bakongEnabled === "true" }),
      ],
    },
    {
      title: "Mock payment testing",
      description: "Allow administrators to issue a short-lived student-specific QR for testing the complete scan and payment-status workflow without transferring real money.",
      fields: [
        field("mockPaymentEnabled", "Enable Mock Scan QR Pay", "Show the mock scanner to students and allow Finance to generate signed test QRs. Keep disabled outside controlled testing.", "toggle"),
      ],
    },
  ],
  departments: [
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
