"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/icon";
import { SearchableSelect } from "@/components/searchable-select";
import { NotificationCenter } from "@/features/notifications/notifications/notification-center";
import type { ShellScopeKey } from "./use-shell-scopes";
import { TopbarSearch } from "./topbar-search";
import { useInstituteClock } from "./use-institute-clock";

type AppTopbarProps = {
  academicYear: Record<string, string>;
  avatar: string;
  departmentOptions: { id: string; label: string }[];
  departmentScope: string;
  events: number;
  institute: Record<string, string>;
  onOpenMenu: () => void;
  onScopeChange: (key: ShellScopeKey, value: string) => void;
  semester: Record<string, string>;
  settingsRoute: boolean;
  showYearScope: boolean;
  system: Record<string, string>;
  yearScope: string;
};

export function AppTopbar({ academicYear, avatar, departmentOptions, departmentScope, events, institute, onOpenMenu, onScopeChange, semester, settingsRoute, showYearScope, system, yearScope }: AppTopbarProps) {
  const router = useRouter();
  const [notificationOpen, setNotificationOpen] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);
  const currentTime = useInstituteClock(system.language ?? "", system.timeZone ?? "", system.dateFormat ?? "");

  return <header className="topbar">
    <button className="icon-button menu-button" onClick={onOpenMenu} aria-label="Open menu"><Icon name="menu"/></button>
    <TopbarSearch departmentId={departmentScope} year={yearScope}/>
    {!settingsRoute && <div className="topbar-scopes">
      <label><SearchableSelect value={departmentScope} options={departmentOptions} placeholder="Find department…" ariaLabel="Filter by department" onChange={(value) => onScopeChange("departmentId", value)}/></label>
      {showYearScope && <label><select aria-label="Filter by student year" value={yearScope} onChange={(event) => onScopeChange("year", event.target.value)}><option value="">All years</option><option value="1">Year 1</option><option value="2">Year 2</option><option value="3">Year 3</option><option value="4">Year 4</option></select></label>}
    </div>}
    <div className="top-actions">
      <button className="term-chip topbar-term-button" onClick={() => router.push("/settings/academic-year")}><span>{semester.currentTerm || "Current term"} · {currentTime}</span><strong>{academicYear.currentYear || "2026–2027"}</strong></button>
      <NotificationCenter open={notificationOpen} events={events} onToggle={() => { setNotificationOpen((value) => !value); setProfileOpen(false); }} onClose={() => setNotificationOpen(false)}/>
      <div className="topbar-popover-anchor">
        <button className="avatar avatar-button" aria-label="Open profile menu" aria-expanded={profileOpen} onClick={() => { setProfileOpen((value) => !value); setNotificationOpen(false); }}>{avatar}</button>
        {profileOpen && <aside className="topbar-popover profile-popover">
          <header><div><strong>{institute.name || "Institute administrator"}</strong><span>{[institute.email, institute.phone, institute.address].filter(Boolean).join(" · ")}</span></div></header>
          <button onClick={() => { setProfileOpen(false); router.push("/settings/institute"); }}>Institute profile</button>
          <button onClick={() => { setProfileOpen(false); router.push("/settings/system"); }}>System preferences</button>
          <button onClick={() => { setProfileOpen(false); router.push("/records/students"); }}>Audit history</button>
        </aside>}
      </div>
    </div>
  </header>;
}
