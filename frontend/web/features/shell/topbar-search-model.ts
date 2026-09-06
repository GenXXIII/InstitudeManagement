import type { ManagementItem, ManagementResource } from "@/features/management/management-types";
import { managementCode } from "@/features/management/management-id";
import {
  announceNavigation,
  enrollmentNavigation,
  historyNavigation,
  managementNavigation,
  operationNavigation,
  recordNavigation,
  settingsNavigation,
} from "./navigation-config";

export type SearchResourceDefinition = {
  id: ManagementResource;
  label: string;
  icon: string;
};

export type SearchSuggestion = {
  id: string;
  code: string;
  label: string;
  detail: string;
};

export type ModuleSearchResult = {
  id: string;
  label: string;
  section: string;
  href: string;
  icon: string;
};

export const searchResources: SearchResourceDefinition[] = [
  { id: "students", label: "Students", icon: "users" },
  { id: "teachers", label: "Teachers", icon: "teacher" },
  { id: "courses", label: "Courses", icon: "book" },
  { id: "classrooms", label: "Classrooms", icon: "room" },
  { id: "timetable", label: "Schedule", icon: "calendar" },
  { id: "departments", label: "Departments", icon: "building" },
];

const moduleSections = [
  { section: "Dashboard", base: "", items: [["", "Institude Dashboard", "dashboard"]] as const },
  { section: "Operation", base: "operation", items: operationNavigation },
  { section: "Enrollment", base: "enrollment", items: enrollmentNavigation },
  { section: "Management", base: "management", items: managementNavigation },
  { section: "Record", base: "record", items: recordNavigation },
  { section: "History", base: "records", items: historyNavigation },
  { section: "Announce", base: "announce", items: announceNavigation },
  { section: "Administration", base: "settings", items: settingsNavigation },
];

const moduleDestinations: ModuleSearchResult[] = moduleSections.flatMap(({ section, base, items }) =>
  items.map(([slug, label, icon]) => ({
    id: `${base || "dashboard"}-${slug || "home"}`,
    label,
    section,
    href: base ? `/${base}/${slug}` : "/",
    icon,
  })),
);

export function itemSuggestion(item: ManagementItem, resource: ManagementResource): SearchSuggestion {
  const values = item.values;
  const code = managementCode(resource, values);
  const label = values.name
    ?? values.student
    ?? values.course
    ?? (resource === "classrooms" ? `Classroom ${code}` : code)
    ?? "Institute record";
  const schedule = values.dayOfWeek && values.startsAt ? `${values.dayOfWeek} ${values.startsAt}` : "";
  const detail = [code, values.department, values.email, values.teacher, values.classroom, schedule].filter(Boolean).join(" · ");
  return { id: item.id, code, label, detail };
}

export function moduleSearchResults(query: string) {
  const normalized = query.trim().toLowerCase();
  if (!normalized) return [];
  return moduleDestinations
    .filter(item => `${item.label} ${item.section} ${item.href}`.toLowerCase().includes(normalized))
    .toSorted((left, right) => moduleRank(left, normalized) - moduleRank(right, normalized) || left.label.localeCompare(right.label))
    .slice(0, 6);
}

export function scopedHref(href: string, departmentId: string, year: string, query = "") {
  const params = new URLSearchParams();
  if (query.trim()) params.set("q", query.trim());
  if (departmentId) params.set("departmentId", departmentId);
  if (year) params.set("year", year);
  return `${href}${params.size ? `?${params}` : ""}`;
}

export function managementSearchHref(resource: ManagementResource, query: string, departmentId: string, year: string) {
  return scopedHref(`/management/${resource}`, departmentId, year, query);
}

export function globalSearchHref(query: string, departmentId: string, year: string) {
  return scopedHref("/search", departmentId, year, query);
}

export function resourceFromPath(pathname: string): ManagementResource {
  const segment = pathname.split("/")[2] as ManagementResource;
  return searchResources.some(resource => resource.id === segment) ? segment : "students";
}

export function matchesYear(item: ManagementItem, year: string) {
  return !year || !item.values.year && !item.values.yearLevel || item.values.year === year || item.values.yearLevel === year;
}

function moduleRank(item: ModuleSearchResult, query: string) {
  const label = item.label.toLowerCase();
  if (label === query) return 0;
  if (label.startsWith(query)) return 1;
  if (label.split(/\s+/).some(word => word.startsWith(query))) return 2;
  return 3;
}
