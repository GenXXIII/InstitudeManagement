"use client";

import { usePathname, useRouter } from "next/navigation";
import { FormEvent, useEffect, useRef, useState } from "react";
import { useInstituteSettings } from "@/features/administration/institute-settings-context";
import { dashboardApi } from "@/features/dashboard/dashboard-api";
import { departmentApi } from "@/features/management/departments/department-api";
import type { DepartmentItem } from "@/features/management/types/department";
import { Sidebar } from "@/features/shell/sidebar";
import { useLiveUpdates } from "@/features/shell/use-live-updates";
import type { Activity } from "@/lib/types/presentation-types";
import { Icon } from "./icon";

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname(); const router = useRouter(); const { settings } = useInstituteSettings();
  const [open, setOpen] = useState(false); const [query, setQuery] = useState("");
  const [departments, setDepartments] = useState<DepartmentItem[]>([]); const [departmentScope, setDepartmentScope] = useState("");
  const [notificationOpen, setNotificationOpen] = useState(false); const [profileOpen, setProfileOpen] = useState(false); const [notifications, setNotifications] = useState<Activity[]>([]); const [now, setNow] = useState(() => new Date());
  const searchInput = useRef<HTMLInputElement>(null); const { live, events } = useLiveUpdates();
  const institute = settings.institute; const academicYear = settings["academic-year"]; const semester = settings.semester; const system = settings.system;

  useEffect(() => { departmentApi.get().then(items => { setDepartments(items); setDepartmentScope(new URLSearchParams(window.location.search).get("departmentId") ?? ""); }).catch(() => setDepartments([])); }, [pathname]);
  useEffect(() => { const sync = () => { if (isScoped(pathname)) setDepartmentScope(new URLSearchParams(window.location.search).get("departmentId") ?? ""); }; const timer = window.setTimeout(sync, 0); window.addEventListener("popstate", sync); return () => { window.clearTimeout(timer); window.removeEventListener("popstate", sync); }; }, [pathname]);
  useEffect(() => { const timer = window.setInterval(() => setNow(new Date()), 30_000); return () => window.clearInterval(timer); }, []);
  useEffect(() => { document.documentElement.lang = system.language?.toLowerCase().startsWith("kh") ? "km" : "en"; }, [system.language]);
  useEffect(() => { const focusSearch = (event: KeyboardEvent) => { if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") { event.preventDefault(); searchInput.current?.focus(); } }; window.addEventListener("keydown", focusSearch); return () => window.removeEventListener("keydown", focusSearch); }, []);

  function search(event: FormEvent) {
    event.preventDefault(); const text = query.trim(); if (!text) return;
    const match = /^(student|teacher|course|classroom):\s*(.+)$/i.exec(text); const resource = match ? `${match[1].toLowerCase()}s` : "students"; const value = match?.[2] ?? text;
    router.push(`/records/${resource}?q=${encodeURIComponent(value)}`);
  }
  function changeScope(value: string) { setDepartmentScope(value); if (isScoped(pathname)) router.push(`${pathname}${value ? `?departmentId=${encodeURIComponent(value)}` : ""}`); }
  async function toggleNotifications() { const next = !notificationOpen; setNotificationOpen(next); setProfileOpen(false); if (next) dashboardApi.get().then(data => setNotifications(data.attention)).catch(() => setNotifications([])); }
  const currentTime = new Intl.DateTimeFormat(system.language?.toLowerCase().startsWith("kh") ? "km-KH" : "en-GB", { hour: "2-digit", minute: "2-digit", timeZone: system.timeZone || "Asia/Bangkok" }).format(now);
  const avatar = (institute.shortName || "INK").slice(0, 2).toUpperCase();

  return <div className="app-frame"><div className="ambient ambient-one"/><div className="ambient ambient-two"/><Sidebar open={open} live={live} instituteName={institute.name || "Institude of New Khmer"} shortName={institute.shortName || "INK"} departments={departments} departmentScope={departmentScope} onScope={changeScope} onClose={() => setOpen(false)}/>{open && <button className="backdrop" onClick={() => setOpen(false)} aria-label="Close navigation"/>}<div className="workspace"><header className="topbar"><button className="icon-button menu-button" onClick={() => setOpen(true)} aria-label="Open menu"><Icon name="menu"/></button><form className="global-search" onSubmit={search}><Icon name="search" size={17}/><input ref={searchInput} aria-label="Search institute records" placeholder="Search records or use teacher: name…" value={query} onChange={event => setQuery(event.target.value)}/><kbd>Ctrl K</kbd></form><div className="top-actions"><button className="term-chip topbar-term-button" onClick={() => router.push("/settings/academic-year")}><span>{semester.currentTerm || "Current term"} · {currentTime}</span><strong>{academicYear.currentYear || "2026–2027"}</strong></button><div className="topbar-popover-anchor"><button className="icon-button notification-button" aria-label="Open notifications" aria-expanded={notificationOpen} onClick={toggleNotifications}><Icon name="bell"/><span>{Math.max(events, notifications.length)}</span></button>{notificationOpen && <aside className="topbar-popover notification-popover"><header><div><strong>Notifications</strong><span>Controlled by Administration rules</span></div><button onClick={() => setNotificationOpen(false)}>Close</button></header><div>{notifications.length ? notifications.map((item, index) => <article key={`${item.title}-${index}`}><i className={`tone-${item.tone}`}/><div><strong>{item.title}</strong><span>{item.detail}</span><small>{item.time}</small></div></article>) : <p>No unread notifications.</p>}</div><button className="topbar-popover-link" onClick={() => { setNotificationOpen(false); router.push("/settings/notifications"); }}>Configure notifications</button></aside>}</div><div className="topbar-popover-anchor"><button className="avatar avatar-button" aria-label="Open profile menu" aria-expanded={profileOpen} onClick={() => { setProfileOpen(value => !value); setNotificationOpen(false); }}>{avatar}</button>{profileOpen && <aside className="topbar-popover profile-popover"><header><div><strong>Institute administrator</strong><span>{institute.email}</span></div></header><button onClick={() => { setProfileOpen(false); router.push("/settings/institute"); }}>Institute profile</button><button onClick={() => { setProfileOpen(false); router.push("/settings/system"); }}>System preferences</button><button onClick={() => { setProfileOpen(false); router.push("/records/audit"); }}>Audit history</button></aside>}</div></div></header><main className="content">{children}</main></div></div>;
}

function isScoped(pathname: string) { return pathname.startsWith("/operation/") || pathname.startsWith("/record/") || pathname.startsWith("/management/"); }
