"use client";

import { FormEvent, useEffect, useId, useMemo, useRef, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { Icon } from "@/components/icon";
import { managementApis } from "@/features/management/management-apis";
import { managementCode } from "@/features/management/management-id";
import type { ManagementItem, ManagementResource } from "@/features/management/management-types";

const resources: { id: ManagementResource; label: string }[] = [
  { id: "students", label: "Students" }, { id: "teachers", label: "Teachers" },
  { id: "courses", label: "Courses" }, { id: "classrooms", label: "Learning rooms" },
  { id: "timetable", label: "Timetable" }, { id: "attendance", label: "Attendance" },
  { id: "grades", label: "Grades" }, { id: "departments", label: "Departments" },
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
      const api = managementApis[resource] as { get: (search?: string, departmentId?: string) => Promise<ManagementItem[]> };
      api.get(text, departmentId).then(result => setItems(result.filter(item => matchesYear(item, year)))).catch(() => setItems([]));
    }, 160);
    return () => window.clearTimeout(timer);
  }, [departmentId, query, resource, year]);

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
    const target = section === "records"
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
      {resources.map(option => <option value={option.id} key={option.id}>{option.label}</option>)}
    </select>
    <span className="global-search-input"><Icon name="search" size={17}/><input ref={input} aria-label={`Search ${resource}`} aria-autocomplete="list" aria-controls={resultsId} aria-expanded={open} role="combobox" placeholder={`Type a first or last name in ${resource}…`} value={query} onFocus={() => setOpen(true)} onBlur={() => window.setTimeout(() => setOpen(false), 120)} onChange={event => { setQuery(event.target.value); setOpen(true); }}/><kbd>Ctrl K</kbd></span>
    {open && query.trim() && <div className="global-search-results" role="listbox" id={resultsId}>
      <header><strong>{resources.find(option => option.id === resource)?.label}</strong><span>Matches the beginning of any name or word</span></header>
      {suggestions.length ? suggestions.map(item => <button type="button" role="option" aria-selected="false" onMouseDown={event => event.preventDefault()} onClick={() => navigate(item.label)} key={item.id}><span><strong>{item.label}</strong><small>{item.detail}</small></span><b>Open</b></button>) : <p>No {resource} begin with “{query.trim()}”.</p>}
    </div>}
  </form>;
}

function resourceFromPath(pathname: string): ManagementResource {
  const segment = pathname.split("/")[2] as ManagementResource;
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
