import type { ConfigurationGroup, SettingFieldDefinition } from "../administration-types";
import { field, options } from "./schema-helpers";

const stages = ["management", "enrollment", "operation", "record", "history"] as const;
const resources = [
  ["student", "Student", ["STU", "ESTU", "OSTU", "RSTU", "HSTU"]],
  ["teacher", "Teacher", ["TEA", "ETEA", "OTEA", "RTEA", "HTEA"]],
  ["department", "Department", ["DEP", "EDEP", "ODEP", "RDEP", "HDEP"]],
  ["course", "Course", ["COU", "ECOU", "OCOU", "RCOU", "HCOU"]],
  ["classroom", "Classroom", ["CLA", "ECLA", "OCLA", "RCLA", "HCLA"]],
  ["timetable", "Timetable", ["TIM", "ETIM", "OTIM", "RTIM", "HTIM"]],
  ["attendance", "Attendance", ["ATT", "EATT", "OATT", "RATT", "HATT"]],
  ["grade", "Grade", ["GRD", "EGRD", "OGRD", "RGRD", "HGRD"]],
  ["session", "Class session", ["SES", "ESES", "OSES", "RSES", "HSES"]],
] as const;

export const codeFormatGroups: readonly ConfigurationGroup[] = [
  {
    title: "Shared code format",
    description: "These rules format every business-code field. Entering 1 uses the selected prefix, optional year, separator, and padding.",
    fields: [
      field("codeIncludeYear", "Include year", "Place the active academic year's first year after the prefix.", "toggle"),
      field("codeStartingNumber", "Starting number", "Sequence used by examples and automatic code creation.", "number", { required: true, min: 0, max: 999999999999 }),
      field("codePaddingWidth", "Padding width", "Minimum digits in a numeric value; choose 1 for STU-1 or 4 for STU-0001.", "number", { required: true, min: 1, max: 12 }),
      field("codeSeparator", "Separator", "Character between the prefix, optional year, and assigned value.", "select", { required: true, options: options("-", "/", ".", "_") }),
    ],
  },
  ...resources.map(([resource, label, prefixes]) => ({
    title: `${label} workflow codes`,
    description: `Prefixes for the same ${label.toLowerCase()} as it moves through Management, Enrollment, Operation, Record, and History.`,
    fields: stages.flatMap((stage, index): SettingFieldDefinition[] => [
      field(`${resource}${capitalize(stage)}Prefix`, `${capitalize(stage)} prefix`, `Prefix used in the ${stage} stage.`, "text", { required: true }),
      field(`${resource}${capitalize(stage)}Example`, `${capitalize(stage)} example`, "Preview using the current shared format.", "derived", { derive: values => example(values, resource, stage, prefixes[index]) }),
    ]),
  })),
];

function example(values: Record<string, string>, resource: string, stage: string, fallback: string) {
  const prefix = values[`${resource}${capitalize(stage)}Prefix`]?.trim().toUpperCase() || fallback;
  const separator = ["-", "/", ".", "_"].includes(values.codeSeparator) ? values.codeSeparator : "-";
  const startingNumber = /^\d+$/.test(values.codeStartingNumber || "") ? values.codeStartingNumber : "1";
  const width = Math.min(12, Math.max(1, Number(values.codePaddingWidth) || 1));
  const sequence = startingNumber.padStart(width, "0");
  const year = values.codeIncludeYear === "true" ? `${new Date().getFullYear()}${separator}` : "";
  return `${prefix}${separator}${year}${sequence}`;
}

function capitalize(value: string) {
  return `${value[0].toUpperCase()}${value.slice(1)}`;
}
