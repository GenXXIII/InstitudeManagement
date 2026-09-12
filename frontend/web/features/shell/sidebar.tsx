"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState } from "react";
import { Icon } from "@/components/icon";
import { announceNavigation, enrollmentNavigation, historyNavigation, managementNavigation, operationNavigation, recordNavigation } from "./navigation-config";
import { NavGroup } from "./nav-group";

export function Sidebar({ open, live, instituteName, shortName, logoUrl, departmentScope, yearScope, onClose }: { open: boolean; live: boolean; instituteName: string; shortName: string; logoUrl: string; departmentScope: string; yearScope: string; onClose: () => void }) {
  const pathname = usePathname();
  const routeGroup = sidebarGroup(pathname);
  const [expansion, setExpansion] = useState<{ pathname: string; group: string | null }>(() => ({ pathname, group: routeGroup }));
  const expandedGroup = expansion.pathname === pathname ? expansion.group : routeGroup;
  const toggle = (group: string) => setExpansion({ pathname, group: expandedGroup === group ? null : group });

  return <aside className={`sidebar ${open ? "open" : ""}`}>
    <div className="brand"><BrandLogo source={logoUrl} instituteName={instituteName}/><div><strong>{instituteName}</strong><span>{shortName} · Management System</span></div></div>
    <nav>
      <Link className={`nav-item nav-direct ${pathname === "/" ? "active" : ""}`} href={scopedHref("/", departmentScope, yearScope)} onClick={onClose}><Icon name="dashboard" size={17}/><span>Institude Dashboard</span></Link>
      <NavGroup label="Operation" icon="bolt" base="operation" items={operationNavigation} expanded={expandedGroup === "operation"} onToggle={() => toggle("operation")} departmentScope={departmentScope} yearScope={yearScope} onNavigate={onClose}/>
      <NavGroup label="Enrollment" icon="users" base="enrollment" items={enrollmentNavigation} expanded={expandedGroup === "enrollment"} onToggle={() => toggle("enrollment")} departmentScope={departmentScope} yearScope={yearScope} onNavigate={onClose}/>
      <NavGroup label="Management" icon="building" base="management" items={managementNavigation} expanded={expandedGroup === "management"} onToggle={() => toggle("management")} departmentScope={departmentScope} yearScope={yearScope} onNavigate={onClose}/>
      <NavGroup label="Record" icon="folder" base="record" items={recordNavigation} expanded={expandedGroup === "record"} onToggle={() => toggle("record")} departmentScope={departmentScope} yearScope={yearScope} onNavigate={onClose}/>
      <NavGroup label="History" icon="archive" base="records" items={historyNavigation} expanded={expandedGroup === "records"} onToggle={() => toggle("records")} departmentScope={departmentScope} yearScope={yearScope} onNavigate={onClose}/>
      <NavGroup label="Announce" icon="bell" base="announce" items={announceNavigation} expanded={expandedGroup === "announce"} onToggle={() => toggle("announce")} departmentScope={departmentScope} yearScope={yearScope} onNavigate={onClose}/>
      <Link className={`nav-item nav-direct ${pathname.startsWith("/settings") ? "active" : ""}`} href="/settings" onClick={onClose}><Icon name="settings" size={17}/><span>Settings</span></Link>
    </nav>
    <div className="sidebar-foot"><span className={`status-dot ${live ? "" : "offline"}`}/><div><strong>{live ? "Systems online" : "API disconnected"}</strong><span>{live ? "Live updates connected" : "Start the backend API"}</span></div></div>
  </aside>;
}

function sidebarGroup(pathname: string) {
  if (pathname === "/") return "dashboard";
  const group = pathname.split("/")[1];
  return ["operation", "enrollment", "management", "record", "records", "announce"].includes(group) ? group : null;
}

function scopedHref(pathname: string, departmentId: string, year: string) {
  const params = new URLSearchParams();
  if (departmentId) params.set("departmentId", departmentId);
  if (year) params.set("year", year);
  return `${pathname}${params.size ? `?${params}` : ""}`;
}

function BrandLogo({ source, instituteName }: { source: string; instituteName: string }) {
  const fallback = "/branding/ink-logo.png";
  const [failedSource, setFailedSource] = useState("");
  const resolvedSource = !source || failedSource === source ? fallback : source;
  // Settings may reference the API or a user-managed CDN, so Next Image host allowlists are not appropriate here.
  // eslint-disable-next-line @next/next/no-img-element
  return <img className="brand-logo" src={resolvedSource} width={44} height={52} alt={`${instituteName} logo`} onError={() => {
    if (resolvedSource !== fallback) setFailedSource(source);
  }}/>;
}
