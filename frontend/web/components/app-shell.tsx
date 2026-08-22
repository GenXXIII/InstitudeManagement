"use client";

import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { useInstituteSettings } from "@/features/administration/institute-settings-context";
import { departmentApi } from "@/features/management/departments/department-api";
import type { DepartmentItem } from "@/features/management/types/department";
import { NotificationCenter } from "@/features/notifications/notification-center";
import { Sidebar } from "@/features/shell/sidebar";
import { TopbarSearch } from "@/features/shell/topbar-search";
import { useLiveUpdates } from "@/features/shell/use-live-updates";
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
  useEffect(() => {
    document.documentElement.lang = system.language?.toLowerCase().startsWith("kh") ? "km" : "en";
    document.title = institute.name || "Institude of New Khmer";
  }, [institute.name, system.language]);

  function changeScope(key: "departmentId" | "year", value: string) {
    if (key === "departmentId") setDepartmentScope(value); else setYearScope(value);
    const params = new URLSearchParams(window.location.search);
    if (value) params.set(key, value); else params.delete(key);
    router.push(`${pathname}${params.size ? `?${params}` : ""}`, { scroll: false });
  }
  const clockLocale = system.language?.toLowerCase().startsWith("kh") ? "km-KH" : "en-GB";
  const currentDate = formatDate(now, clockLocale, system.timeZone || "Asia/Bangkok", system.dateFormat);
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
          <NotificationCenter open={notificationOpen} events={events} onToggle={() => { setNotificationOpen(value => !value); setProfileOpen(false); }} onClose={() => setNotificationOpen(false)}/>
          <div className="topbar-popover-anchor"><button className="avatar avatar-button" aria-label="Open profile menu" aria-expanded={profileOpen} onClick={() => { setProfileOpen(value => !value); setNotificationOpen(false); }}>{avatar}</button>{profileOpen && <aside className="topbar-popover profile-popover"><header><div><strong>{institute.name || "Institute administrator"}</strong><span>{[institute.email, institute.phone, institute.address].filter(Boolean).join(" · ")}</span></div></header><button onClick={() => { setProfileOpen(false); router.push("/settings/institute"); }}>Institute profile</button><button onClick={() => { setProfileOpen(false); router.push("/settings/system"); }}>System preferences</button><button onClick={() => { setProfileOpen(false); router.push("/records/students"); }}>Audit history</button></aside>}</div>
        </div>
      </header>
      <main className="content">{children}</main>
    </div>
  </div>;
}

function formatDate(date: Date, locale: string, timeZone: string, format = "DD MMM YYYY") {
  const normalized = format.trim().toUpperCase();
  if (normalized === "YYYY-MM-DD") return new Intl.DateTimeFormat("en-CA", { year: "numeric", month: "2-digit", day: "2-digit", timeZone }).format(date);
  if (normalized === "MM/DD/YYYY") return new Intl.DateTimeFormat("en-US", { year: "numeric", month: "2-digit", day: "2-digit", timeZone }).format(date);
  if (normalized === "DD/MM/YYYY") return new Intl.DateTimeFormat("en-GB", { year: "numeric", month: "2-digit", day: "2-digit", timeZone }).format(date);
  return new Intl.DateTimeFormat(locale, { day: "2-digit", month: "short", year: "numeric", timeZone }).format(date);
}
