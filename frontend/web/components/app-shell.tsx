"use client";

import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { useInstituteSettings } from "@/features/administration/institute-settings-context";
import { dashboardApi } from "@/features/dashboard/dashboard-api";
import { departmentApi } from "@/features/management/departments/department-api";
import type { DepartmentItem } from "@/features/management/types/department";
import { Sidebar } from "@/features/shell/sidebar";
import { TopbarSearch } from "@/features/shell/topbar-search";
import { useLiveUpdates } from "@/features/shell/use-live-updates";
import type { Activity } from "@/lib/types/presentation-types";
import { Icon } from "./icon";
import { SearchableSelect } from "./searchable-select";

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { settings } = useInstituteSettings();
  const [open, setOpen] = useState(false);
  const [departments, setDepartments] = useState<DepartmentItem[]>([]);
  const [departmentScope, setDepartmentScope] = useState("");
  const [yearScope, setYearScope] = useState("");
  const [notificationOpen, setNotificationOpen] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);
  const [notifications, setNotifications] = useState<Activity[]>([]);
  const [now, setNow] = useState(() => new Date());
  const { live, events } = useLiveUpdates();
  const institute = settings.institute;
  const academicYear = settings["academic-year"];
  const semester = settings.semester;
  const system = settings.system;

  useEffect(() => { departmentApi.get().then(setDepartments).catch(() => setDepartments([])); }, []);
  useEffect(() => {
    const sync = () => { const params = new URLSearchParams(window.location.search); setDepartmentScope(params.get("departmentId") ?? ""); setYearScope(params.get("year") ?? ""); };
    const timer = window.setTimeout(sync, 0);
    window.addEventListener("popstate", sync);
    return () => { window.clearTimeout(timer); window.removeEventListener("popstate", sync); };
  }, [pathname]);
  useEffect(() => { const timer = window.setInterval(() => setNow(new Date()), 1_000); return () => window.clearInterval(timer); }, []);
  useEffect(() => { document.documentElement.lang = system.language?.toLowerCase().startsWith("kh") ? "km" : "en"; }, [system.language]);

  function changeScope(key: "departmentId" | "year", value: string) {
    if (key === "departmentId") setDepartmentScope(value); else setYearScope(value);
    const params = new URLSearchParams(window.location.search);
    if (value) params.set(key, value); else params.delete(key);
    router.push(`${pathname}${params.size ? `?${params}` : ""}`, { scroll: false });
  }
  async function toggleNotifications() { const next = !notificationOpen; setNotificationOpen(next); setProfileOpen(false); if (next) dashboardApi.get().then(data => setNotifications(data.attention)).catch(() => setNotifications([])); }
  const clockLocale = system.language?.toLowerCase().startsWith("kh") ? "km-KH" : "en-GB";
  const currentDate = new Intl.DateTimeFormat(clockLocale, { day: "2-digit", month: "short", year: "numeric", timeZone: system.timeZone || "Asia/Bangkok" }).format(now);
  const clockTime = new Intl.DateTimeFormat(clockLocale, { hour: "2-digit", minute: "2-digit", second: "2-digit", hourCycle: "h23", timeZone: system.timeZone || "Asia/Bangkok" }).format(now);
  const currentTime = `${currentDate} · ${clockTime}`;
  const avatar = (institute.shortName || "INK").slice(0, 2).toUpperCase();
  const departmentOptions = [{ id: "", label: "All departments" }, ...departments.map(department => ({ id: department.id, label: department.values.name }))];

  return <div className="app-frame">
    <div className="ambient ambient-one"/><div className="ambient ambient-two"/>
    <Sidebar open={open} live={live} instituteName={institute.name || "Institude of New Khmer"} shortName={institute.shortName || "INK"} departmentScope={departmentScope} yearScope={yearScope} onClose={() => setOpen(false)}/>
    {open && <button className="backdrop" onClick={() => setOpen(false)} aria-label="Close navigation"/>}
    <div className="workspace">
      <header className="topbar">
        <button className="icon-button menu-button" onClick={() => setOpen(true)} aria-label="Open menu"><Icon name="menu"/></button>
        <TopbarSearch departmentId={departmentScope} year={yearScope}/>
        <div className="topbar-scopes">
          <label><SearchableSelect value={departmentScope} options={departmentOptions} placeholder="Find department…" ariaLabel="Filter by department" onChange={value => changeScope("departmentId", value)}/></label>
          <label><select aria-label="Filter by student year" value={yearScope} onChange={event => changeScope("year", event.target.value)}><option value="">All years</option><option value="1">Year 1</option><option value="2">Year 2</option><option value="3">Year 3</option><option value="4">Year 4</option></select></label>
        </div>
        <div className="top-actions">
          <button className="term-chip topbar-term-button" onClick={() => router.push("/settings/academic-year")}><span>{semester.currentTerm || "Current term"} · {currentTime}</span><strong>{academicYear.currentYear || "2026–2027"}</strong></button>
          <div className="topbar-popover-anchor"><button className="icon-button notification-button" aria-label="Open notifications" aria-expanded={notificationOpen} onClick={toggleNotifications}><Icon name="bell"/><span>{Math.max(events, notifications.length)}</span></button>{notificationOpen && <aside className="topbar-popover notification-popover"><header><div><strong>Notifications</strong><span>Controlled by Administration rules</span></div><button onClick={() => setNotificationOpen(false)}>Close</button></header><div>{notifications.length ? notifications.map((item, index) => <article key={`${item.title}-${index}`}><i className={`tone-${item.tone}`}/><div><strong>{item.title}</strong><span>{item.detail}</span><small>{item.time}</small></div></article>) : <p>No unread notifications.</p>}</div><button className="topbar-popover-link" onClick={() => { setNotificationOpen(false); router.push("/settings/notifications"); }}>Configure notifications</button></aside>}</div>
          <div className="topbar-popover-anchor"><button className="avatar avatar-button" aria-label="Open profile menu" aria-expanded={profileOpen} onClick={() => { setProfileOpen(value => !value); setNotificationOpen(false); }}>{avatar}</button>{profileOpen && <aside className="topbar-popover profile-popover"><header><div><strong>Institute administrator</strong><span>{institute.email}</span></div></header><button onClick={() => { setProfileOpen(false); router.push("/settings/institute"); }}>Institute profile</button><button onClick={() => { setProfileOpen(false); router.push("/settings/system"); }}>System preferences</button><button onClick={() => { setProfileOpen(false); router.push("/records/audit"); }}>Audit history</button></aside>}</div>
        </div>
      </header>
      <main className="content">{children}</main>
    </div>
  </div>;
}
