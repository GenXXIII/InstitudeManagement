"use client";

import { FormEvent, useEffect, useId, useMemo, useRef, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { Icon } from "@/components/icon";
import { attendanceApi } from "@/features/attendance/attendance-api";
import type { AttendanceItem } from "@/features/attendance/attendance-types";
import { enrollmentApiFor } from "@/features/enrollment/enrollment-apis";
import type { EnrollmentResource } from "@/features/enrollment/common/enrollment-types";
import { managementApis } from "@/features/management/management-apis";
import { managementCode } from "@/features/management/management-id";
import type { ManagementItem, ManagementResource } from "@/features/management/management-types";
import { workflowSourceSearch } from "@/lib/workflow-code";

type SearchResource = ManagementResource | "attendance";
type SearchItem = ManagementItem | AttendanceItem;

const resources: { id: SearchResource; label: string }[] = [
  { id: "students", label: "Students" }, { id: "teachers", label: "Teachers" },
  { id: "courses", label: "Courses" }, { id: "classrooms", label: "Learning rooms" },
  { id: "timetable", label: "Timetable" }, { id: "attendance", label: "Attendance" },
  { id: "departments", label: "Departments" },
];

export function TopbarSearch({ departmentId, year }: { departmentId: string; year: string }) {
  const pathname = usePathname();
  const router = useRouter();
  const input = useRef<HTMLInputElement>(null);
  const resultsId = useId();
  const [resource, setResource] = useState<SearchResource>(() => resourceFromPath(pathname));
  const [query, setQuery] = useState("");
  const [items, setItems] = useState<SearchItem[]>([]);
  const [open, setOpen] = useState(false);
  const availableResources = useMemo(() => {
    const filtered = pathname.startsWith("/enrollment/")
      ? resources.filter(option => ["students", "teachers", "courses", "classrooms", "timetable", "departments"].includes(option.id))
      : pathname.startsWith("/management/")
        ? resources.filter(option => ["students", "teachers", "courses", "classrooms", "timetable", "departments"].includes(option.id))
        : resources;
    return pathname.startsWith("/management/") ? filtered.map(option => option.id === "timetable" ? { ...option, label: "Schedule" } : option) : filtered;
  }, [pathname]);
  const resourceLabel = availableResources.find(option => option.id === resource)?.label ?? resource;

  useEffect(() => { const timer = window.setTimeout(() => { setResource(resourceFromPath(pathname)); setQuery(""); setItems([]); }, 0); return () => window.clearTimeout(timer); }, [pathname]);
  useEffect(() => {
    const focusSearch = (event: KeyboardEvent) => { if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") { event.preventDefault(); input.current?.focus(); } };
    window.addEventListener("keydown", focusSearch);
    return () => window.removeEventListener("keydown", focusSearch);
  }, []);
  useEffect(() => {
    const text = workflowSourceSearch(query);
    if (!text) { const timer = window.setTimeout(() => setItems([]), 0); return () => window.clearTimeout(timer); }
    const timer = window.setTimeout(() => {
      const promise = pathname.startsWith("/enrollment/")
        ? enrollmentApiFor(resource as EnrollmentResource).get(text, departmentId, year)
        : resource === "attendance"
          ? attendanceApi.get(text, departmentId)
          : managementApis[resource].get(text, departmentId);
      promise.then(result => setItems((result as SearchItem[]).filter(item => matchesYear(item, year)))).catch(() => setItems([]));
    }, 160);
    return () => window.clearTimeout(timer);
  }, [departmentId, pathname, query, resource, year]);

  const suggestions = useMemo(() => items.map(item => suggestion(item, resource)).filter(item => startsWithWord(item.label, workflowSourceSearch(query)) || startsWithWord(item.detail, workflowSourceSearch(query))).slice(0, 9), [items, query, resource]);

  function navigate(value: string) {
    const text = value.trim();
    if (!text) return;
    const params = new URLSearchParams();
    params.set("q", text);
    if (departmentId) params.set("departmentId", departmentId);
    if (year) params.set("year", year);
    const section = pathname.split("/")[1];
    const operationalRecord = ["students", "teachers", "courses", "classrooms"].includes(resource);
    const target = section === "enrollment" && ["students", "teachers", "courses", "classrooms", "timetable", "departments"].includes(resource)
      ? `/enrollment/${resource}`
      : section === "records"
      ? `/records/${resource}`
      : section === "record-history" && operationalRecord
        ? `/record-history/${resource}`
        : section === "record" && operationalRecord
          ? `/record/${resource}`
          : `/management/${resource}`;
    setOpen(false);
    router.push(`${target}?${params}`);
  }

  function submit(event: FormEvent) { event.preventDefault(); navigate(query); }

  return <form className="global-search" onSubmit={submit}>
    <select aria-label="Search feature" value={resource} onChange={event => { setResource(event.target.value as SearchResource); setItems([]); setOpen(true); }}>
      {availableResources.map(option => <option value={option.id} key={option.id}>{option.label}</option>)}
    </select>
    <span className="global-search-input"><Icon name="search" size={17}/><input ref={input} aria-label={`Search ${resourceLabel}`} aria-autocomplete="list" aria-controls={resultsId} aria-expanded={open} role="combobox" placeholder={`Search in ${resourceLabel.toLowerCase()}…`} value={query} onFocus={() => setOpen(true)} onBlur={() => window.setTimeout(() => setOpen(false), 120)} onChange={event => { setQuery(event.target.value); setOpen(true); }}/><kbd>Ctrl K</kbd></span>
    {open && query.trim() && <div className="global-search-results" role="listbox" id={resultsId}>
      <header><strong>{resourceLabel}</strong><span>Matches the beginning of any name or word</span></header>
      {suggestions.length ? suggestions.map(item => <button type="button" role="option" aria-selected="false" onMouseDown={event => event.preventDefault()} onClick={() => navigate(item.label)} key={item.id}><span><strong>{item.label}</strong><small>{item.detail}</small></span><b>Open</b></button>) : <p>No {resource} begin with “{query.trim()}”.</p>}
    </div>}
  </form>;
}

function resourceFromPath(pathname: string): SearchResource {
  const segment = pathname.split("/")[2] as SearchResource;
  if (segment === "attendance") return "attendance";
  return resources.some(option => option.id === segment) ? segment : "students";
}
function startsWithWord(value: string, query: string) { const text = query.trim().toLowerCase(); return !text || value.toLowerCase().split(/\s+/).some(word => word.startsWith(text)); }
function matchesYear(item: SearchItem, year: string) { return !year || !item.values.year && !item.values.yearLevel || item.values.year === year || item.values.yearLevel === year; }
function suggestion(item: SearchItem, resource: SearchResource) {
  const values = item.values;
  const uiId = resource === "attendance" ? values.attendanceCode : managementCode(resource, values);
  const label = values.name ?? values.student ?? values.course ?? (resource === "classrooms" ? `Room ${uiId}` : uiId) ?? "Institute record";
  const detail = [uiId, values.department, values.email, values.dayOfWeek && values.startsAt ? `${values.dayOfWeek} ${values.startsAt}` : ""].filter(Boolean).join(" · ");
  return { id: item.id, label, detail };
}
