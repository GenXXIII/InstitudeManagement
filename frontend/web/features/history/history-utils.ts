import type { RecordGroup, RecordItem } from "./history-types";

export function groupRecords(rows: RecordItem[]): RecordGroup[] {
  const aliases = new Map<string, string>();
  for (const row of rows.filter(item => item.resourceId && item.id === item.resourceId)) {
    const key = `${row.type}:${row.resourceId}`; aliases.set(alias(row.type, row.subject), key);
    for (const [name, value] of parseDetails(row.details)) if (["name", "studentCode", "teacherCode", "departmentCode", "courseCode", "classroomCode", "timetableCode", "attendanceCode", "gradeCode", "fullName"].includes(name)) aliases.set(alias(row.type, value), key);
  }
  const grouped = new Map<string, RecordItem[]>();
  for (const row of rows) { const key = row.resourceId ? `${row.type}:${row.resourceId}` : aliases.get(alias(row.type, row.subject)) ?? `${row.type}:subject:${row.subject.toLowerCase()}`; grouped.set(key, [...(grouped.get(key) ?? []), row]); }
  return [...grouped.entries()].map(([key, entries]) => { entries.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()); const current = entries.find(entry => entry.resourceId && entry.id === entry.resourceId); const terminal = entries.find(entry => isInactive(entry.action)); return { key, type: current?.type ?? entries[0].type, subject: current?.subject ?? entries[0].subject, status: current?.action ?? terminal?.action ?? "Historical", entries, values: parseDetails((current ?? entries[0]).details) }; }).sort((a, b) => new Date(b.entries[0].date).getTime() - new Date(a.entries[0].date).getTime());
}

export function parseDetails(details: string): [string, string][] { try { const value = JSON.parse(details) as Record<string, unknown>; return Object.entries(value).map(([key, item]) => [key, formatValue(item)]); } catch { return [["Details", details]]; } }
export function displayValue(key: string, value: string) { if (key.toLowerCase().includes("photo")) return value === "false" ? "Not stored" : "4×6 photo stored"; if (value === "true") return "Yes"; if (value === "false") return "No"; return value; }
export function isTechnicalField(key: string) { const value = key.toLowerCase(); return value.endsWith("id") || value.includes("photo") || value.includes("createdat") || value.includes("updatedat"); }
export function isInactive(status: string) { return ["inactive", "deactivated", "removed", "cancelled", "archived"].some(value => status.toLowerCase().includes(value)); }
export function slug(value: string) { return value.toLowerCase().replace(/[^a-z0-9]+/g, "-"); }
export function pretty(value: string) { return value.replace(/([A-Z])/g, " $1").replace(/^./, first => first.toUpperCase()); }
export function formatDate(value: string) { return new Date(value).toLocaleDateString(undefined, { day: "numeric", month: "short", year: "numeric" }); }
export function exportCsv(rows: RecordItem[]) { const csv = ["AuditLogCode,Date,Type,Subject,Action,Details", ...rows.map(row => [row.auditLogCode, row.date, row.type, row.subject, row.action, row.details].map(value => `"${String(value).replaceAll('"', '""')}"`).join(","))].join("\n"); const link = document.createElement("a"); link.href = URL.createObjectURL(new Blob([csv], { type: "text/csv" })); link.download = "institute-record-register.csv"; link.click(); URL.revokeObjectURL(link.href); }
function alias(type: string, value: string) { return `${type}:${value.trim().toLowerCase()}`; }
function formatValue(value: unknown): string { if (value === null || value === undefined || value === "") return "—"; return typeof value === "object" ? JSON.stringify(value) : String(value); }
