"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";
import { Icon } from "./icon";
import { api, API_URL } from "@/lib/api";
import type { CatalogItem } from "@/lib/types";

const operation = [
  ["control-room", "Control room", "pulse"], ["students", "Students", "users"], ["teachers", "Teachers", "teacher"],
  ["classrooms", "Classrooms", "room"], ["courses", "Courses", "book"], ["timetable", "Timetable", "calendar"],
  ["attendance", "Attendance", "check"], ["departments", "Departments", "building"], ["grades", "Grades", "grade"],
] as const;
const management = operation.slice(1);
const records = [...operation.slice(1), ["audit-logs", "Audit logs", "archive"]] as const;
const settings = [
  ["institute", "Institute"], ["academic-year", "Academic year"], ["semester", "Semester / term"], ["departments", "Departments"],
  ["courses", "Courses"], ["classrooms", "Classrooms"], ["attendance-rules", "Attendance rules"], ["grade-rules", "Grade rules"],
  ["notifications", "Notifications"], ["system", "System"],
] as const;

function NavGroup({ label, base, items, onNavigate, scope }: { label: string; base: string; items: readonly (readonly string[])[]; onNavigate: () => void; scope?: string }) {
  const pathname = usePathname();
  return <div className="nav-group"><div className="nav-label">{label}</div>{items.map(([slug, name, icon = "settings"]) => {
    const href = `/${base}/${slug}${scope ? `?departmentId=${encodeURIComponent(scope)}` : ""}`; const active = pathname === `/${base}/${slug}`;
    return <Link className={`nav-item ${active ? "active" : ""}`} href={href} key={slug} onClick={onNavigate}><Icon name={icon as Parameters<typeof Icon>[0]["name"]} size={16}/><span>{name}</span></Link>;
  })}</div>;
}

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname(); const router = useRouter();
  const [open, setOpen] = useState(false); const [query, setQuery] = useState(""); const [live, setLive] = useState(false); const [events, setEvents] = useState(3); const [departments, setDepartments] = useState<CatalogItem[]>([]); const [departmentScope, setDepartmentScope] = useState("");
  useEffect(() => {
    api.catalog("departments").then(items => {
      setDepartments(items);
      const requestedDepartment = new URLSearchParams(window.location.search).get("departmentId") ?? "";
      setDepartmentScope(requestedDepartment);
    }).catch(() => setDepartments([]));
  }, [pathname, router]);
  useEffect(() => { const syncScope = () => { if (pathname.startsWith("/operation/") || pathname.startsWith("/management/")) setDepartmentScope(new URLSearchParams(window.location.search).get("departmentId") ?? ""); }; const timer = window.setTimeout(syncScope, 0); window.addEventListener("popstate", syncScope); return () => { window.clearTimeout(timer); window.removeEventListener("popstate", syncScope); }; }, [pathname]);
  useEffect(() => {
    let connection: import("@microsoft/signalr").HubConnection | undefined;
    import("@microsoft/signalr").then(({ HubConnectionBuilder, LogLevel }) => {
      connection = new HubConnectionBuilder().withUrl(`${API_URL}/hubs/institute`).withAutomaticReconnect().configureLogging(LogLevel.Warning).build();
      connection.on("InstituteEvent", () => setEvents(value => value + 1));
      connection.start().then(() => setLive(true)).catch(() => setLive(false));
    });
    return () => { connection?.stop(); };
  }, []);
  function search(event: FormEvent) { event.preventDefault(); if (query.trim()) router.push(`/records/audit-logs?q=${encodeURIComponent(query)}`); }
  function changeScope(value: string) { setDepartmentScope(value); if (pathname.startsWith("/operation/") || pathname.startsWith("/management/")) router.push(`${pathname}${value ? `?departmentId=${encodeURIComponent(value)}` : ""}`); }

  return <div className="app-frame">
    <div className="ambient ambient-one"/><div className="ambient ambient-two"/>
    <aside className={`sidebar ${open ? "open" : ""}`}>
      <div className="brand"><div className="brand-mark">N</div><div><strong>Northstar</strong><span>Institute OS</span></div></div>
      <nav>
        <Link className={`nav-item nav-home ${pathname === "/" ? "active" : ""}`} href="/" onClick={() => setOpen(false)}><Icon name="dashboard" size={17}/><span>Dashboard</span></Link>
        <label className="sidebar-department-select sidebar-global-scope"><select aria-label="Department" value={departmentScope} onChange={event => changeScope(event.target.value)}><option value="">All</option>{departments.map(department => <option value={department.id} key={department.id}>{department.values.name}</option>)}</select></label>
        <NavGroup label="Operation" base="operation" items={operation} scope={departmentScope} onNavigate={() => setOpen(false)}/>
        <NavGroup label="Management" base="management" items={management} scope={departmentScope} onNavigate={() => setOpen(false)}/>
        <NavGroup label="Record" base="records" items={records} onNavigate={() => setOpen(false)}/>
        <NavGroup label="Setting" base="settings" items={settings} onNavigate={() => setOpen(false)}/>
      </nav>
      <div className="sidebar-foot"><span className={`status-dot ${live ? "" : "offline"}`}/><div><strong>{live ? "Systems online" : "API disconnected"}</strong><span>{live ? "Live updates connected" : "Start the backend API"}</span></div></div>
    </aside>
    {open && <button className="backdrop" onClick={() => setOpen(false)} aria-label="Close navigation"/>}
    <div className="workspace">
      <header className="topbar">
        <button className="icon-button menu-button" onClick={() => setOpen(true)} aria-label="Open menu"><Icon name="menu"/></button>
        <form className="global-search" onSubmit={search}><Icon name="search" size={17}/><input aria-label="Search all records" placeholder="Search students, courses, records…" value={query} onChange={e => setQuery(e.target.value)}/><kbd>⌘ K</kbd></form>
        <div className="top-actions"><div className="term-chip"><span>Academic year</span><strong>2026–2027</strong></div><button className="icon-button notification-button" aria-label="Notifications"><Icon name="bell"/><span>{events}</span></button><div className="avatar">AR</div></div>
      </header>
      <main className="content">{children}</main>
    </div>
  </div>;
}
