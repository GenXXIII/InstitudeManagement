export type WorkflowCodeResource = "student" | "teacher" | "course" | "classroom" | "timetable" | "department" | "attendance" | "grade" | "session" | "alert";
export type WorkflowCodeStage = "management" | "enrollment" | "operation" | "record" | "history";

type ResourcePrefixes = Record<WorkflowCodeStage, string>;

const fallbackPrefixes: Record<WorkflowCodeResource, ResourcePrefixes> = {
  student: prefixes("STU", "ESTU", "OPE", "REC", "HIS"),
  teacher: prefixes("TEA", "ETEA", "OPE", "REC", "HIS"),
  course: prefixes("COU", "ECOU", "OPE", "REC", "HIS"),
  classroom: prefixes("CLA", "ECLA", "OPE", "REC", "HIS"),
  timetable: prefixes("TIM", "ETIM", "OPE", "REC", "HIS"),
  department: prefixes("DEP", "EDEP", "OPE", "REC", "HIS"),
  attendance: prefixes("ATT", "EATT", "OPE", "REC", "HIS"),
  grade: prefixes("GRD", "EGRD", "OPE", "REC", "HIS"),
  session: prefixes("SES", "ESES", "OPE", "REC", "HIS"),
  alert: prefixes("ALT", "EALT", "OPE", "REC", "HIS"),
};

export const workflowStages: WorkflowCodeStage[] = ["management", "enrollment", "operation", "record", "history"];
let runtimeValues: Record<string, string> = fallbackValues();
let runtimeYear = new Date().getFullYear().toString();

export function configureWorkflowCodes(values: Record<string, string>, academicYear?: string) {
  runtimeValues = { ...fallbackValues(), ...values };
  runtimeYear = academicYear?.match(/\d{4}/)?.[0] ?? new Date().getFullYear().toString();
}

export function formatAssignedCode(sourceCode: string | undefined, resource: WorkflowCodeResource, stage: WorkflowCodeStage = "management") {
  const raw = (sourceCode ?? "").trim();
  if (!raw) return "";
  const separator = configuredSeparator();
  const prefix = configuredPrefix(resource, "management");
  let sequence = stripPrefix(raw.toUpperCase(), workflowStages.map(value => configuredPrefix(resource, value)));
  if (runtimeValues.codeIncludeYear === "true" && sequence.startsWith(`${runtimeYear}${separator}`)) sequence = sequence.slice(runtimeYear.length + separator.length);
  if (/^\d+$/.test(sequence)) sequence = sequence.padStart(configuredPadding(), "0");
  if (!sequence) return "";
  const management = [prefix, ...(runtimeValues.codeIncludeYear === "true" ? [runtimeYear] : []), sequence].join(separator).toUpperCase();
  if (stage === "management") return management;
  const numericSequence = numericSuffix(management);
  return numericSequence ? linkedCode(management, resource, stage, numericSequence.padStart(configuredPadding(), "0")) : management;
}

export function formatNotificationCode(sourceCode: string | undefined) {
  const raw = (sourceCode ?? "").trim();
  if (!raw) return "";
  const separator = configuredSeparator();
  const prefix = runtimeValues.notificationCodePrefix?.trim().toUpperCase() || "NOT";
  let sequence = stripPrefix(raw.toUpperCase(), [prefix]);
  if (runtimeValues.codeIncludeYear === "true" && sequence.startsWith(`${runtimeYear}${separator}`)) sequence = sequence.slice(runtimeYear.length + separator.length);
  if (/^\d+$/.test(sequence)) sequence = sequence.padStart(configuredPadding(), "0");
  if (!sequence) return "";
  return [prefix, ...(runtimeValues.codeIncludeYear === "true" ? [runtimeYear] : []), sequence].join(separator).toUpperCase();
}

export function notificationCodeExample() {
  return formatNotificationCode(runtimeValues.codeStartingNumber || "1");
}

export function workflowCode(sourceCode: string | undefined, resource: WorkflowCodeResource, stage: WorkflowCodeStage = "management") {
  const management = managementSource(sourceCode, resource);
  if (!management) return `${configuredPrefix(resource, "management")}${configuredSeparator()}UNASSIGNED`;
  if (stage === "management") return management;
  const sequence = numericSuffix(management);
  return sequence ? linkedCode(management, resource, stage, sequence.padStart(configuredPadding(), "0")) : management;
}

export function workflowCodeExample(resource: WorkflowCodeResource, stage: WorkflowCodeStage = "management") {
  return formatAssignedCode(runtimeValues.codeStartingNumber || "1", resource, stage);
}

export function workflowResourceForField(key: string): WorkflowCodeResource | undefined {
  if (key === "studentCode") return "student";
  if (key === "teacherCode") return "teacher";
  if (key === "courseCode") return "course";
  if (key === "classroomCode") return "classroom";
  if (key === "timetableCode") return "timetable";
  if (key === "departmentCode") return "department";
  if (key === "attendanceCode") return "attendance";
  if (key === "gradeCode") return "grade";
  if (key === "classSessionRecordCode") return "session";
  if (key === "announcementCode") return "alert";
  return undefined;
}

export function workflowResource(value: string): WorkflowCodeResource {
  const key = value.toLowerCase().replaceAll("-", "");
  if (key.startsWith("student")) return "student";
  if (key.startsWith("teacher")) return "teacher";
  if (key.startsWith("course")) return "course";
  if (key.startsWith("classroom") || key.startsWith("room")) return "classroom";
  if (key.startsWith("timetable") || key.startsWith("schedule")) return "timetable";
  if (key.startsWith("department")) return "department";
  if (key.startsWith("attendance")) return "attendance";
  if (key.startsWith("grade") || key.startsWith("result")) return "grade";
  if (key.startsWith("alert") || key.startsWith("announcement")) return "alert";
  return "session";
}

export function workflowStageLabel(stage: WorkflowCodeStage) {
  return stage === "management" ? "Management" : stage === "enrollment" ? "Enrollment" : stage === "operation" ? "Operation" : stage === "record" ? "Record" : "History";
}

export function workflowSourceSearch(query: string) {
  const trimmed = query.trim();
  if (!trimmed) return "";
  for (const resource of Object.keys(fallbackPrefixes) as WorkflowCodeResource[]) {
    const source = managementSource(trimmed, resource);
    if (source !== trimmed.toUpperCase()) return source;
  }
  return trimmed;
}

function linkedCode(management: string, resource: WorkflowCodeResource, stage: Exclude<WorkflowCodeStage, "management">, sequence: string) {
  const separator = configuredSeparator();
  return `${management}${separator}${configuredPrefix(resource, stage)}${separator}${sequence}`.toUpperCase();
}

function managementSource(sourceCode: string | undefined, resource: WorkflowCodeResource) {
  let normalized = (sourceCode ?? "").trim().toUpperCase();
  if (!normalized) return "";
  const separator = escapeRegExp(configuredSeparator());
  for (const stage of workflowStages.filter(value => value !== "management")) {
    const prefix = escapeRegExp(configuredPrefix(resource, stage));
    normalized = normalized.replace(new RegExp(`${separator}${prefix}${separator}\\d+$`, "i"), "");
  }
  return normalized;
}

function stripPrefix(value: string, prefixesToStrip: string[]) {
  for (const prefix of prefixesToStrip.filter(Boolean).toSorted((left, right) => right.length - left.length)) {
    if (!value.startsWith(prefix)) continue;
    const remainder = value.slice(prefix.length);
    if (!remainder) return "";
    if (["-", "/", ".", "_"].includes(remainder[0])) return remainder.slice(1);
    if (/^\d/.test(remainder)) return remainder;
  }
  return value;
}

function numericSuffix(value: string) {
  return value.match(/\d+$/)?.[0] ?? "";
}

function configuredPrefix(resource: WorkflowCodeResource, stage: WorkflowCodeStage) {
  return runtimeValues[`${resource}${capitalize(stage)}Prefix`]?.trim().toUpperCase() || fallbackPrefixes[resource][stage];
}

function configuredSeparator() {
  return ["-", "/", ".", "_"].includes(runtimeValues.codeSeparator) ? runtimeValues.codeSeparator : "-";
}

function configuredPadding() {
  const value = Number(runtimeValues.codePaddingWidth);
  return Number.isInteger(value) && value >= 1 && value <= 12 ? value : 1;
}

function fallbackValues() {
  const format = { codeIncludeYear: "false", codeStartingNumber: "1", codePaddingWidth: "1", codeSeparator: "-" };
  return Object.assign({}, format, ...Object.entries(fallbackPrefixes).flatMap(([resource, values]) =>
    workflowStages.map(stage => ({ [`${resource}${capitalize(stage)}Prefix`]: values[stage] }))));
}

function prefixes(management: string, enrollment: string, operation: string, record: string, history: string): ResourcePrefixes {
  return { management, enrollment, operation, record, history };
}

function capitalize(value: string) {
  return `${value[0].toUpperCase()}${value.slice(1)}`;
}

function escapeRegExp(value: string) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}
