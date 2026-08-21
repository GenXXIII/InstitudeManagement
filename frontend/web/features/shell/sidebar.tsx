"use client";

import Link from "next/link";
import Image from "next/image";
import { usePathname } from "next/navigation";
import { Icon } from "@/components/icon";
import type { CatalogItem } from "@/features/management/management-types";
import { historyNavigation, managementNavigation, operationNavigation, recordNavigation, settingsNavigation } from "./navigation-config";
import { NavGroup } from "./nav-group";

export function Sidebar({ open, live, departments, departmentScope, onScope, onClose }: { open: boolean; live: boolean; departments: CatalogItem[]; departmentScope: string; onScope: (value: string) => void; onClose: () => void }) {
  const pathname = usePathname();
  return <aside className={`sidebar ${open ? "open" : ""}`}><div className="brand"><Image className="brand-logo" src="/branding/ink-logo.png" width={44} height={52} alt="INK logo" preload/><div><strong>INK</strong><span>Management System</span></div></div><nav><Link className={`nav-item nav-home ${pathname === "/" ? "active" : ""}`} href="/" onClick={onClose}><Icon name="dashboard" size={17}/><span>Institute overview</span></Link><label className="sidebar-department-select sidebar-global-scope"><select aria-label="Department" value={departmentScope} onChange={event => onScope(event.target.value)}><option value="">All</option>{departments.map(department => <option value={department.id} key={department.id}>{department.values.name}</option>)}</select></label><NavGroup label="Institute operations" base="operation" items={operationNavigation} scope={departmentScope} onNavigate={onClose}/><NavGroup label="Record" base="record" items={recordNavigation} scope={departmentScope} onNavigate={onClose}/><NavGroup label="Academic management" base="management" items={managementNavigation} scope={departmentScope} onNavigate={onClose}/><NavGroup label="History" base="records" items={historyNavigation} onNavigate={onClose}/><NavGroup label="Administration" base="settings" items={settingsNavigation} onNavigate={onClose}/></nav><div className="sidebar-foot"><span className={`status-dot ${live ? "" : "offline"}`}/><div><strong>{live ? "Systems online" : "API disconnected"}</strong><span>{live ? "Live updates connected" : "Start the backend API"}</span></div></div></aside>;
}
