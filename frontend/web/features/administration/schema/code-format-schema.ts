import type { ConfigurationGroup, SettingFieldDefinition } from "../administration-types";
import { field, options } from "./schema-helpers";

const stages = ["management", "enrollment", "operation", "record", "history"] as const;
const resources = [
  ["student", "Student", ["STU", "ENR", "OPE", "REC", "HIS"]],
  ["teacher", "Teacher", ["TEA", "ENR", "OPE", "REC", "HIS"]],
  ["department", "Department", ["DEP", "ENR", "OPE", "REC", "HIS"]],
  ["course", "Course", ["COU", "ENR", "OPE", "REC", "HIS"]],
  ["classroom", "Classroom", ["CLA", "ENR", "OPE", "REC", "HIS"]],
  ["timetable", "Timetable", ["TIM", "ENR", "OPE", "REC", "HIS"]],
  ["attendance", "Attendance", ["ATT", "ENR", "OPE", "REC", "HIS"]],
  ["grade", "Grade", ["GRD", "ENR", "OPE", "REC", "HIS"]],
  ["session", "Class session", ["SES", "ENR", "OPE", "REC", "HIS"]],
] as const;

export const codeFormatGroups: readonly ConfigurationGroup[] = [
  {
    title: "Shared linked-code format",
    description: "Management stays permanent and Enrollment keeps its own occurrence. Student Operation, Record, and History add the program semester before the complete Enrollment identity, such as REC-1-ENR-1-STU-1.",
    fields: [
      field("codeIncludeYear", "Include year", "Place the active academic year's first year inside the Management code.", "toggle"),
      field("codeStartingNumber", "Starting number", "Initial sequence shown in previews and used by records that the backend creates automatically.", "number", { required: true, min: 0, max: 999999999999 }),
      field("codePaddingWidth", "Padding width", "Digits shared by assigned Management, Alert, and linked-code suffixes; 5 turns sequence 1 into STU-00001 or NOT-00001.", "number", { required: true, min: 1, max: 12 }),
      field("codeSeparator", "Separator", "Character joining the permanent Management code and each stage segment.", "select", { required: true, options: options("-", "/", ".", "_") }),
    ],
  },
  ...resources.map(([resource, label, prefixes]) => ({
    title: `${label} linked codes`,
    description: `The ${label.toLowerCase()} Management code is the permanent identity; Enrollment, Operation, Record, and History codes remain traceable to it.`,
    fields: stages.flatMap((stage): SettingFieldDefinition[] => [
      field(`${resource}${capitalize(stage)}Prefix`, `${capitalize(stage)} prefix`, `Prefix used for the ${stage} segment.`, "text", { required: true }),
      field(`${resource}${capitalize(stage)}Example`, `${capitalize(stage)} example`, "Preview of the backend-generated linked code.", "derived", { derive: values => example(values, resource, stage, prefixes) }),
    ]),
  })),
  {
    title: "Enrollment-scoped access and outcome codes",
    description: "Finance, paid-payment, and Result codes use the program semester and the complete Student Enrollment identity. Public IDs use the Enrollment database GUID.",
    fields: [
      field("financeCodePrefix", "Finance prefix", "Prefix for a Student finance account linked to one program semester and Enrollment.", "text", { required: true }),
      field("financeCodeExample", "Finance example", "Example: FIN-1-ENR-1-STU-1.", "derived", { derive: values => studentPeriodExample(values, "financeCodePrefix", "FIN") }),
      field("paymentCodePrefix", "Paid payment prefix", "Prefix for the paid-payment identity linked to one program semester and Enrollment.", "text", { required: true }),
      field("paymentCodeExample", "Paid payment example", "Example: PAY-1-ENR-1-STU-1.", "derived", { derive: values => studentPeriodExample(values, "paymentCodePrefix", "PAY") }),
      field("resultCodePrefix", "Academic Result prefix", "Prefix for one Student semester result.", "text", { required: true }),
      field("resultCodeExample", "Academic Result example", "Example: RES-1-ENR-1-STU-1.", "derived", { derive: values => studentPeriodExample(values, "resultCodePrefix", "RES") }),
      field("studentPublicIdPrefix", "Student Public ID prefix", "Issued only when the Student is enrolled and joined to the Enrollment database GUID.", "text", { required: true }),
      field("studentPublicIdExample", "Student Public ID example", "Enrollment-scoped mobile sign-in identity, such as STU-{GUID}.", "derived", { derive: values => publicIdExample(values, "studentPublicIdPrefix", "STU") }),
      field("teacherPublicIdPrefix", "Teacher Public ID prefix", "Issued only when the Teacher is assigned in Enrollment; the default format is TEA-{GUID}.", "text", { required: true }),
      field("teacherPublicIdExample", "Teacher Public ID example", "Assignment GUID mobile sign-in identity, such as TEA-{GUID}.", "derived", { derive: values => publicIdExample(values, "teacherPublicIdPrefix", "TEA") }),
    ],
  },
  {
    title: "Alert, notification, and history codes",
    description: "Alerts and system notifications keep separate identities while using the shared year, padding, and separator configured above.",
    fields: [
      field("alertCodePrefix", "Alert prefix", "Prefix used when a typed Alert sequence is formatted.", "text", { required: true }),
      field("alertCodeExample", "Alert example", "Preview of the permanent Alert identity.", "derived", { derive: values => standaloneExample(values, "alertCodePrefix", "ALT") }),
      field("notificationCodePrefix", "System notification prefix", "Prefix automatically assigned to system notifications.", "text", { required: true }),
      field("notificationCodeExample", "System notification example", "Preview of the automatic Notification identity.", "derived", { derive: values => standaloneExample(values, "notificationCodePrefix", "NOT") }),
      field("historyCodePrefix", "Notification history prefix", "Prefix assigned to permanent notification lifecycle entries; this is separate from Record History.", "text", { required: true }),
      field("historyCodeExample", "Notification history example", "Preview using the shared Code Formats rules.", "derived", { derive: values => standaloneExample(values, "historyCodePrefix", "NHS") }),
    ],
  },
];

function example(values: Record<string, string>, resource: string, stage: typeof stages[number], fallbacks: readonly string[]) {
  const separator = ["-", "/", ".", "_"].includes(values.codeSeparator) ? values.codeSeparator : "-";
  const rawSequence = /^\d+$/.test(values.codeStartingNumber || "") ? values.codeStartingNumber : "1";
  const width = Math.min(12, Math.max(1, Number(values.codePaddingWidth) || 1));
  const sequence = rawSequence.padStart(width, "0");
  const managementPrefix = values[`${resource}ManagementPrefix`]?.trim().toUpperCase() || fallbacks[0];
  const year = values.codeIncludeYear === "true" ? `${new Date().getFullYear()}${separator}` : "";
  const managementCode = `${managementPrefix}${separator}${year}${sequence}`;
  if (stage === "management") return managementCode;
  const stageIndex = stages.indexOf(stage);
  const stagePrefix = values[`${resource}${capitalize(stage)}Prefix`]?.trim().toUpperCase() || fallbacks[stageIndex];
  const occurrence = "1".padStart(width, "0");
  const enrollmentPrefix = values[`${resource}EnrollmentPrefix`]?.trim().toUpperCase() || fallbacks[1];
  const enrollment = `${enrollmentPrefix}${separator}${occurrence}${separator}${managementCode}`;
  if (stage === "enrollment" || resource !== "student") return `${stagePrefix}${separator}${occurrence}${separator}${managementCode}`;
  return `${stagePrefix}${separator}${occurrence}${separator}${enrollment}`;
}

function studentPeriodExample(values: Record<string, string>, prefixKey: string, fallbackPrefix: string) {
  const separator = ["-", "/", ".", "_"].includes(values.codeSeparator) ? values.codeSeparator : "-";
  const width = Math.min(12, Math.max(1, Number(values.codePaddingWidth) || 1));
  const managementSequence = (/^\d+$/.test(values.codeStartingNumber || "") ? values.codeStartingNumber : "1").padStart(width, "0");
  const occurrence = "1".padStart(width, "0");
  const year = values.codeIncludeYear === "true" ? `${new Date().getFullYear()}${separator}` : "";
  const management = `${values.studentManagementPrefix?.trim().toUpperCase() || "STU"}${separator}${year}${managementSequence}`;
  const enrollment = `${values.studentEnrollmentPrefix?.trim().toUpperCase() || "ENR"}${separator}${occurrence}${separator}${management}`;
  return `${values[prefixKey]?.trim().toUpperCase() || fallbackPrefix}${separator}${occurrence}${separator}${enrollment}`;
}

function publicIdExample(values: Record<string, string>, prefixKey: string, fallbackPrefix: string) {
  const prefix = values[prefixKey]?.trim().toUpperCase() || fallbackPrefix;
  return `${prefix}-550e8400e29b41d4a716446655440000`;
}

function standaloneExample(values: Record<string, string>, prefixKey: string, fallback: string) {
  const separator = ["-", "/", ".", "_"].includes(values.codeSeparator) ? values.codeSeparator : "-";
  const prefix = values[prefixKey]?.trim().toUpperCase() || fallback;
  const rawSequence = /^\d+$/.test(values.codeStartingNumber || "") ? values.codeStartingNumber : "1";
  const width = Math.min(12, Math.max(1, Number(values.codePaddingWidth) || 1));
  const sequence = rawSequence.padStart(width, "0");
  const year = values.codeIncludeYear === "true" ? `${new Date().getFullYear()}${separator}` : "";
  return `${prefix}${separator}${year}${sequence}`;
}

function capitalize(value: string) {
  return `${value[0].toUpperCase()}${value.slice(1)}`;
}
