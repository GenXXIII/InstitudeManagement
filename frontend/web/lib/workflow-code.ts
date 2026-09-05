export type WorkflowCodeResource = "student" | "teacher" | "course" | "classroom" | "timetable" | "department" | "attendance" | "grade" | "session";
export type WorkflowCodeStage = "management" | "enrollment" | "operation" | "record" | "history";

type ResourcePrefixes = Record<WorkflowCodeStage, string>;

const fallbackPrefixes: Record<WorkflowCodeResource, ResourcePrefixes> = {
  student: prefixes("STU", "ESTU", "OSTU", "RSTU", "HSTU"),
  teacher: prefixes("TEA", "ETEA", "OTEA", "RTEA", "HTEA"),
  course: prefixes("COU", "ECOU", "OCOU", "RCOU", "HCOU"),
  classroom: prefixes("CLA", "ECLA", "OCLA", "RCLA", "HCLA"),
  timetable: prefixes("TIM", "ETIM", "OTIM", "RTIM", "HTIM"),
  department: prefixes("DEP", "EDEP", "ODEP", "RDEP", "HDEP"),
  attendance: prefixes("ATT", "EATT", "OATT", "RATT", "HATT"),
  grade: prefixes("GRD", "EGRD", "OGRD", "RGRD", "HGRD"),
  session: prefixes("SES", "ESES", "OSES", "RSES", "HSES"),
};

export const workflowStages: WorkflowCodeStage[] = ["management", "enrollment", "operation", "record", "history"];
let runtimeValues: Record<string, string> = fallbackValues();
let runtimeYear = new Date().getFullYear().toString();


export function configureWorkflowCodes(values: Record<string, string>, academicYear?: string) {
  runtimeValues = { ...fallbackValues(), ...values };
  runtimeYear = academicYear?.match(/\d{4}/)?.[0] ?? new Date().getFullYear().toString();
}

export function formatAssignedCode(sourceCode: string | undefined, resource: WorkflowCodeResource, stage: WorkflowCodeStage) {
  const prefix = configuredPrefix(resource, stage);
  const separator = configuredSeparator();
  const rawSuffix = sourceSuffix(sourceCode, resource);
  if (!rawSuffix) return "";
  const includeYear = runtimeValues.codeIncludeYear === "true";
  const suffixWithoutYear = includeYear && rawSuffix.startsWith(`${runtimeYear}${separator}`)
    ? rawSuffix.slice(runtimeYear.length + separator.length)
    : rawSuffix;
  const suffix = /^\d+$/.test(suffixWithoutYear)
    ? suffixWithoutYear.padStart(configuredPadding(), "0")
    : suffixWithoutYear;
  return [prefix, ...(includeYear ? [runtimeYear] : []), suffix].join(separator).toUpperCase();
}

export function workflowCode(sourceCode: string | undefined, resource: WorkflowCodeResource, stage: WorkflowCodeStage) {
  const prefix = configuredPrefix(resource, stage);
  const suffix = sourceSuffix(sourceCode, resource);
  if (!suffix) return `${prefix}${configuredSeparator()}UNASSIGNED`;
  return `${prefix}${configuredSeparator()}${suffix}`.toUpperCase();
}

export function workflowCodeExample(resource: WorkflowCodeResource, stage: WorkflowCodeStage) {
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
  return "session";
}

export function workflowStageLabel(stage: WorkflowCodeStage) {
  return stage === "management" ? "Management" : stage === "enrollment" ? "Enrollment" : stage === "operation" ? "Operation" : stage === "record" ? "Record" : "History";
}

export function workflowSourceSearch(query: string) {
  const trimmed = query.trim();
  if (!trimmed) return "";
  for (const resource of Object.keys(fallbackPrefixes) as WorkflowCodeResource[]) {
    const suffix = sourceSuffix(trimmed, resource);
    if (suffix !== trimmed.toUpperCase()) return suffix;
  }
  return trimmed;
}

function sourceSuffix(sourceCode: string | undefined, resource: WorkflowCodeResource) {
  const normalized = (sourceCode ?? "").trim().toUpperCase();
  if (!normalized) return "";
  const knownPrefixes = workflowStages
    .map(stage => configuredPrefix(resource, stage).toUpperCase())
    .filter(Boolean)
    .toSorted((left, right) => right.length - left.length);
  for (const prefix of knownPrefixes) {
    if (!normalized.startsWith(prefix)) continue;
    const remainder = normalized.slice(prefix.length);
    if (!remainder) return "";
    if (/^[._/-]/.test(remainder)) return remainder.slice(1);
    if (/^\d/.test(remainder)) return remainder;
  }
  return normalized;
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
