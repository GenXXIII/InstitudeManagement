import type { ManagementItem, ManagementResource } from "@/features/management/management-types";
import { managementCode } from "@/features/management/management-id";
import { workflowSourceSearch } from "@/lib/workflow-code";
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

export type SearchMatch = {
  label: string;
  field: string;
  rank: number;
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
    href: base ? `/${base}${slug ? `/${slug}` : ""}` : "/",
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
  const terms = searchTerms(query);
  if (!terms.length) return [];
  return moduleDestinations
    .filter(item => matchesAllTerms(`${item.label} ${item.section} ${item.href}`, terms))
    .toSorted((left, right) => moduleSearchMatch(left, query).rank - moduleSearchMatch(right, query).rank || left.label.localeCompare(right.label))
    .slice(0, 6);
}

export function searchQuerySeed(query: string) {
  const [first] = query.trim().split(/\s+/);
  return workflowSourceSearch(first ?? query);
}

export function suggestionSearchMatch(item: SearchSuggestion, query: string): SearchMatch | undefined {
  return classifyMatch([
    { label: "Code", value: item.code },
    { label: "Name", value: item.label },
    { label: "Details", value: item.detail },
  ], query);
}

export function moduleSearchMatch(item: ModuleSearchResult, query: string): SearchMatch {
  return classifyMatch([
    { label: "Page", value: item.label },
    { label: "Section", value: item.section },
    { label: "Route", value: item.href },
  ], query) ?? { label: "Related page", field: item.section, rank: 6 };
}

export function filterAndRankSuggestions(items: SearchSuggestion[], query: string) {
  return items
    .map(item => ({ item, match: suggestionSearchMatch(item, query) }))
    .filter((result): result is { item: SearchSuggestion; match: SearchMatch } => Boolean(result.match))
    .toSorted((left, right) => left.match.rank - right.match.rank || left.item.label.localeCompare(right.item.label, undefined, { numeric: true }));
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

function classifyMatch(fields: { label: string; value: string }[], query: string): SearchMatch | undefined {
  const normalized = query.trim().toLowerCase();
  const terms = searchTerms(query);
  if (!normalized || !terms.length) return undefined;
  const normalizedFields = fields.filter(field => field.value).map(field => ({ ...field, normalized: field.value.toLowerCase() }));
  const exact = normalizedFields.find(field => field.normalized === normalized);
  if (exact) return { label: `Exact ${exact.label.toLowerCase()}`, field: exact.label, rank: exact.label === "Code" ? 0 : 1 };
  const starts = normalizedFields.find(field => field.normalized.startsWith(normalized));
  if (starts) return { label: "Starts with", field: starts.label, rank: 2 };
  const word = normalizedFields.find(field => field.normalized.split(/[^a-z0-9]+/).some(value => value.startsWith(normalized)));
  if (word) return { label: "Word starts with", field: word.label, rank: 3 };
  const phrase = normalizedFields.find(field => field.normalized.includes(normalized));
  if (phrase) return { label: "Contains characters", field: phrase.label, rank: 4 };
  const combined = normalizedFields.map(field => field.normalized).join(" ");
  if (matchesAllTerms(combined, terms)) return { label: terms.length > 1 ? "Matches all words" : "Contains characters", field: "Record", rank: 5 };
  return undefined;
}

function searchTerms(query: string) {
  return query.trim().toLowerCase().split(/\s+/).filter(Boolean);
}

function matchesAllTerms(value: string, terms: string[]) {
  const normalized = value.toLowerCase();
  return terms.every(term => normalized.includes(term));
}
