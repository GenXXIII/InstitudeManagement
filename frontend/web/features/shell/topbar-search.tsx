"use client";

import { FormEvent, useEffect, useId, useMemo, useRef, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { Icon } from "@/components/icon";
import { enrollmentApi, type EnrollmentResource } from "@/features/enrollment/enrollment-api";
import { managementApis } from "@/features/management/management-apis";
import { managementCode } from "@/features/management/management-id";
import type { ManagementItem, ManagementResource } from "@/features/management/management-types";

const resources: { id: ManagementResource; label: string }[] = [
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
  const [resource, setResource] = useState<ManagementResource>(() => resourceFromPath(pathname));
  const [query, setQuery] = useState("");
  const [items, setItems] = useState<ManagementItem[]>([]);
  const [open, setOpen] = useState(false);
  const availableResources = useMemo(() => pathname.startsWith("/enrollment/")
    ? resources.filter(option => ["students", "teachers", "courses", "classrooms", "timetable", "departments"].includes(option.id))
    : pathname.startsWith("/management/")
      ? resources.filter(option => ["students", "teachers", "courses", "classrooms", "timetable", "departments"].includes(option.id))
      : resources, [pathname]);

  useEffect(() => { const timer = window.setTimeout(() => { setResource(resourceFromPath(pathname)); setQuery(""); setItems([]); }, 0); return () => window.clearTimeout(timer); }, [pathname]);
  useEffect(() => {
    const focusSearch = (event: KeyboardEvent) => { if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") { event.preventDefault(); input.current?.focus(); } };
    window.addEventListener("keydown", focusSearch);
    return () => window.removeEventListener("keydown", focusSearch);
  }, []);
  useEffect(() => {
    const text = query.trim();
    if (!text) { const timer = window.setTimeout(() => setItems([]), 0); return () => window.clearTimeout(timer); }
    const timer = window.setTimeout(() => {
      const promise = pathname.startsWith("/enrollment/")
        ? enrollmentApi.get(resource as EnrollmentResource, text, departmentId, year)
        : (managementApis[resource] as { get: (search?: string, departmentId?: string) => Promise<ManagementItem[]> }).get(text, departmentId);
      promise.then(result => setItems((result as ManagementItem[]).filter(item => matchesYear(item, year)))).catch(() => setItems([]));
    }, 160);
    return () => window.clearTimeout(timer);
  }, [departmentId, pathname, query, resource, year]);

  const suggestions = useMemo(() => items.map(item => suggestion(item, resource)).filter(item => startsWithWord(item.label, query) || startsWithWord(item.detail, query)).slice(0, 9), [items, query, resource]);

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
    <select aria-label="Search feature" value={resource} onChange={event => { setResource(event.target.value as ManagementResource); setItems([]); setOpen(true); }}>
      {availableResources.map(option => <option value={option.id} key={option.id}>{option.label}</option>)}
    </select>
    <span className="global-search-input"><Icon name="search" size={17}/><input ref={input} aria-label={`Search ${resource}`} aria-autocomplete="list" aria-controls={resultsId} aria-expanded={open} role="combobox" placeholder={`Type a first or last name in ${resource}…`} value={query} onFocus={() => setOpen(true)} onBlur={() => window.setTimeout(() => setOpen(false), 120)} onChange={event => { setQuery(event.target.value); setOpen(true); }}/><kbd>Ctrl K</kbd></span>
    {open && query.trim() && <div className="global-search-results" role="listbox" id={resultsId}>
      <header><strong>{availableResources.find(option => option.id === resource)?.label}</strong><span>Matches the beginning of any name or word</span></header>
      {suggestions.length ? suggestions.map(item => <button type="button" role="option" aria-selected="false" onMouseDown={event => event.preventDefault()} onClick={() => navigate(item.label)} key={item.id}><span><strong>{item.label}</strong><small>{item.detail}</small></span><b>Open</b></button>) : <p>No {resource} begin with “{query.trim()}”.</p>}
    </div>}
  </form>;
}

function resourceFromPath(pathname: string): ManagementResource {
  const segment = pathname.split("/")[2] as ManagementResource;
  if (pathname.startsWith("/management/") && (segment === "attendance" || segment === "grades")) return "students";
  return resources.some(option => option.id === segment) ? segment : "students";
}
function startsWithWord(value: string, query: string) { const text = query.trim().toLowerCase(); return !text || value.toLowerCase().split(/\s+/).some(word => word.startsWith(text)); }
function matchesYear(item: ManagementItem, year: string) { return !year || !item.values.year && !item.values.yearLevel || item.values.year === year || item.values.yearLevel === year; }
function suggestion(item: ManagementItem, resource: ManagementResource) {
  const values = item.values;
  const uiId = managementCode(resource, values);
  const label = values.name ?? values.student ?? values.course ?? (resource === "classrooms" ? `Room ${uiId}` : uiId) ?? "Institute record";
  const detail = [uiId, values.department, values.email, values.dayOfWeek && values.startsAt ? `${values.dayOfWeek} ${values.startsAt}` : ""].filter(Boolean).join(" · ");
  return { id: item.id, label, detail };
}
