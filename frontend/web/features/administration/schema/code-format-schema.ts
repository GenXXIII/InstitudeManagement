import type { ConfigurationGroup, SettingFieldDefinition } from "../administration-types";
import { field, options } from "./schema-helpers";

const stages = ["management", "enrollment", "operation", "record", "history"] as const;
const resources = [
  ["student", "Student", ["STU", "ESTU", "OPE", "REC", "HIS"]],
  ["teacher", "Teacher", ["TEA", "ETEA", "OPE", "REC", "HIS"]],
  ["department", "Department", ["DEP", "EDEP", "OPE", "REC", "HIS"]],
  ["course", "Course", ["COU", "ECOU", "OPE", "REC", "HIS"]],
  ["classroom", "Classroom", ["CLA", "ECLA", "OPE", "REC", "HIS"]],
  ["timetable", "Timetable", ["TIM", "ETIM", "OPE", "REC", "HIS"]],
  ["attendance", "Attendance", ["ATT", "EATT", "OPE", "REC", "HIS"]],
  ["grade", "Grade", ["GRD", "EGRD", "OPE", "REC", "HIS"]],
  ["session", "Class session", ["SES", "ESES", "OPE", "REC", "HIS"]],
  ["alert", "Alert", ["ALT", "EALT", "OPE", "REC", "HIS"]],
] as const;

export const codeFormatGroups: readonly ConfigurationGroup[] = [
  {
    title: "Shared linked-code format",
    description: "The API assigns the Management identity and automatically builds every linked stage code with the same numeric sequence.",
    fields: [
      field("codeIncludeYear", "Include year", "Place the active academic year's first year inside the Management code.", "toggle"),
      field("codeStartingNumber", "Starting number", "Lowest sequence used when the backend assigns a new Management code.", "number", { required: true, min: 0, max: 999999999999 }),
      field("codePaddingWidth", "Padding width", "Digits shared by the Management and stage suffixes; 4 produces STU-0001-ESTU-0001.", "number", { required: true, min: 1, max: 12 }),
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
  return `${managementCode}${separator}${stagePrefix}${separator}${sequence}`;
}

function capitalize(value: string) {
  return `${value[0].toUpperCase()}${value.slice(1)}`;
}
