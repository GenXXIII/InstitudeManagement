export type CatalogItem = { id: string; values: Record<string, string> };
export type ManagementModule = "overview" | "students" | "teachers" | "classrooms" | "courses" | "timetable" | "attendance" | "departments" | "grades";
export type References = { departments: CatalogItem[]; teachers: CatalogItem[]; students: CatalogItem[]; classrooms: CatalogItem[]; courses: CatalogItem[] };
export type Field = { key: string; label: string; type?: "text" | "email" | "number" | "select" | "photo" | "date" | "time" | "checkbox"; source?: keyof References; options?: string[]; required?: boolean };
export type LayoutProps = { items: CatalogItem[]; onEdit: (item: CatalogItem) => void; onDeactivate: (item: CatalogItem) => void };
