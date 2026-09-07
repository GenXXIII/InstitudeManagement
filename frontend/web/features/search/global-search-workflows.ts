import type { ManagementResource } from "@/features/management/management-types";
import { workflowCode, workflowResource, type WorkflowCodeStage } from "@/lib/workflow-code";
import { scopedHref, type SearchSuggestion } from "@/features/shell/topbar-search-model";

export type GlobalSearchWorkflow = Exclude<WorkflowCodeStage, "operation">;

export const globalSearchWorkflows: { id: GlobalSearchWorkflow; label: string; detail: string; icon: "settings" | "users" | "folder" | "archive" }[] = [
  { id: "management", label: "Management", detail: "Master profiles and institute resources", icon: "settings" },
  { id: "enrollment", label: "Enrollment", detail: "Academic assignments and placement", icon: "users" },
  { id: "record", label: "Record", detail: "Current semester evidence", icon: "folder" },
  { id: "history", label: "History", detail: "Completed read-only records", icon: "archive" },
];

export function workflowSuggestion(item: SearchSuggestion, resource: ManagementResource, workflow: GlobalSearchWorkflow): SearchSuggestion {
  return { ...item, code: workflowCode(item.code, workflowResource(resource), workflow) };
}

export function workflowResultHref(workflow: GlobalSearchWorkflow, resource: ManagementResource, query: string, departmentId: string, year: string) {
  const base = workflow === "management"
    ? `/management/${resource}`
    : workflow === "enrollment"
      ? `/enrollment/${resource}`
      : workflow === "record"
        ? `/record/${resource}`
        : `/records/${resource}`;
  return scopedHref(base, departmentId, year, query);
}

export function isGlobalSearchWorkflow(value: string | null): value is GlobalSearchWorkflow {
  return globalSearchWorkflows.some(workflow => workflow.id === value);
}

