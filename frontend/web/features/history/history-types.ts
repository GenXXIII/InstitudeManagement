export type RecordItem = { id: string; resourceId?: string | null; date: string; type: string; subject: string; action: string; details: string; auditLogCode: string };
export type LifecycleFilter = "all" | "current" | "inactive";
export type RecordGroup = { key: string; type: string; subject: string; status: string; entries: RecordItem[]; values: [string, string][] };
