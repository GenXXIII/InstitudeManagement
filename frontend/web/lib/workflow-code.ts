export type WorkflowCodeResource = "student" | "teacher" | "course" | "classroom" | "timetable" | "department" | "attendance" | "grade" | "session";
export type WorkflowCodeStage = "management" | "enrollment" | "operation" | "record" | "history";

const resourcePrefixes: Record<WorkflowCodeResource, string> = {
  student: "STU",
  teacher: "TEA",
  course: "COU",
  classroom: "CLA",
  timetable: "TIM",
  department: "DEP",
  attendance: "ATT",
  grade: "GRD",
  session: "SES",
};

const stagePrefixes: Record<WorkflowCodeStage, string> = {
  management: "",
  enrollment: "E",
  operation: "O",
  record: "R",
  history: "H",
};

export const workflowStages: WorkflowCodeStage[] = ["management", "enrollment", "operation", "record", "history"];

export function workflowCode(sourceCode: string | undefined, resource: WorkflowCodeResource, stage: WorkflowCodeStage) {
  const basePrefix = resourcePrefixes[resource];
  const prefix = `${stagePrefixes[stage]}${basePrefix}`;
  const normalized = (sourceCode ?? "").trim();
  if (!normalized) return `${prefix}-UNASSIGNED`;
  if (normalized.toUpperCase().startsWith(`${prefix}-`)) return normalized.toUpperCase();
  const separator = normalized.indexOf("-");
  const suffix = separator >= 0 ? normalized.slice(separator + 1) : normalized;
  return `${prefix}-${suffix}`.toUpperCase();
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
  const match = trimmed.match(/^(?:EA|[EORH])?(?:STU|TEA|COU|CLA|TIM|DEP|ATT|GRD|SES)-(.+)$/i);
  return match?.[1] ?? trimmed;
}
