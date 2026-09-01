"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState } from "react";
import { Icon } from "@/components/icon";
import { announceNavigation, enrollmentNavigation, historyNavigation, managementNavigation, operationNavigation, recordNavigation, settingsNavigation } from "./navigation-config";
import { NavGroup } from "./nav-group";

export function Sidebar({ open, live, instituteName, shortName, logoUrl, departmentScope, yearScope, onClose }: { open: boolean; live: boolean; instituteName: string; shortName: string; logoUrl: string; departmentScope: string; yearScope: string; onClose: () => void }) {
  const pathname = usePathname();
  return <aside className={`sidebar ${open ? "open" : ""}`}>
    <div className="brand"><BrandLogo source={logoUrl} instituteName={instituteName}/><div><strong>{instituteName}</strong><span>{shortName} · Management System</span></div></div>
    <nav>
      <Link className={`nav-item nav-home ${pathname === "/" ? "active" : ""}`} href={scopedHref("/", departmentScope, yearScope)} onClick={onClose}><Icon name="dashboard" size={17}/><span>Institude Dashboard</span></Link>
      <NavGroup label="Institute operations" base="operation" items={operationNavigation} departmentScope={departmentScope} yearScope={yearScope} onNavigate={onClose}/>
      <NavGroup label="Academic enrollment" base="enrollment" items={enrollmentNavigation} departmentScope={departmentScope} yearScope={yearScope} onNavigate={onClose}/>
      <NavGroup label="Academic management" base="management" items={managementNavigation} departmentScope={departmentScope} yearScope={yearScope} onNavigate={onClose}/>
      <NavGroup label="Record" base="record" items={recordNavigation} departmentScope={departmentScope} yearScope={yearScope} onNavigate={onClose}/>
      <NavGroup label="History" base="records" items={historyNavigation} departmentScope={departmentScope} yearScope={yearScope} onNavigate={onClose}/>
      <NavGroup label="Announce" base="announce" items={announceNavigation} departmentScope={departmentScope} yearScope={yearScope} onNavigate={onClose}/>
      <NavGroup label="Administration" base="settings" items={settingsNavigation} preserveScope={false} onNavigate={onClose}/>
    </nav>
    <div className="sidebar-foot"><span className={`status-dot ${live ? "" : "offline"}`}/><div><strong>{live ? "Systems online" : "API disconnected"}</strong><span>{live ? "Live updates connected" : "Start the backend API"}</span></div></div>
  </aside>;
}

function scopedHref(pathname: string, departmentId: string, year: string) { const params = new URLSearchParams(); if (departmentId) params.set("departmentId", departmentId); if (year) params.set("year", year); return `${pathname}${params.size ? `?${params}` : ""}`; }

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
