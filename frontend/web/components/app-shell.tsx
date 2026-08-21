"use client";

import { usePathname, useRouter } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";
import { managementApi } from "@/features/management/management-api";
import type { CatalogItem } from "@/features/management/management-types";
import { Sidebar } from "@/features/shell/sidebar";
import { useLiveUpdates } from "@/features/shell/use-live-updates";
import { Icon } from "./icon";

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname(); const router = useRouter();
  const [open, setOpen] = useState(false); const [query, setQuery] = useState("");
  const [departments, setDepartments] = useState<CatalogItem[]>([]); const [departmentScope, setDepartmentScope] = useState("");
  const { live, events } = useLiveUpdates();

  useEffect(() => { managementApi.get("departments").then(items => { setDepartments(items); setDepartmentScope(new URLSearchParams(window.location.search).get("departmentId") ?? ""); }).catch(() => setDepartments([])); }, [pathname]);
  useEffect(() => { const sync = () => { if (isScoped(pathname)) setDepartmentScope(new URLSearchParams(window.location.search).get("departmentId") ?? ""); }; const timer = window.setTimeout(sync, 0); window.addEventListener("popstate", sync); return () => { window.clearTimeout(timer); window.removeEventListener("popstate", sync); }; }, [pathname]);

  function search(event: FormEvent) { event.preventDefault(); if (query.trim()) router.push(`/records/students?q=${encodeURIComponent(query)}`); }
  function changeScope(value: string) { setDepartmentScope(value); if (isScoped(pathname)) router.push(`${pathname}${value ? `?departmentId=${encodeURIComponent(value)}` : ""}`); }

  return <div className="app-frame"><div className="ambient ambient-one"/><div className="ambient ambient-two"/><Sidebar open={open} live={live} departments={departments} departmentScope={departmentScope} onScope={changeScope} onClose={() => setOpen(false)}/>{open && <button className="backdrop" onClick={() => setOpen(false)} aria-label="Close navigation"/>}<div className="workspace"><header className="topbar"><button className="icon-button menu-button" onClick={() => setOpen(true)} aria-label="Open menu"><Icon name="menu"/></button><form className="global-search" onSubmit={search}><Icon name="search" size={17}/><input aria-label="Search student history" placeholder="Search student history…" value={query} onChange={event => setQuery(event.target.value)}/><kbd>⌘ K</kbd></form><div className="top-actions"><div className="term-chip"><span>Academic year</span><strong>2026–2027</strong></div><button className="icon-button notification-button" aria-label="Notifications"><Icon name="bell"/><span>{events}</span></button><div className="avatar">AR</div></div></header><main className="content">{children}</main></div></div>;
}

function isScoped(pathname: string) {
  return (pathname.startsWith("/operation/") && pathname !== "/operation/timetable") || pathname.startsWith("/record/") || pathname.startsWith("/management/");
}
