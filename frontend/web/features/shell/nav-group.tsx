"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Icon } from "@/components/icon";

export function NavGroup({ label, icon, base, items, expanded, onToggle, onNavigate, departmentScope = "", yearScope = "", preserveScope = true }: {
  label: string;
  icon: Parameters<typeof Icon>[0]["name"];
  base: string;
  items: readonly (readonly string[])[];
  expanded: boolean;
  onToggle: () => void;
  onNavigate: () => void;
  departmentScope?: string;
  yearScope?: string;
  preserveScope?: boolean;
}) {
  const pathname = usePathname();
  const root = base ? `/${base}` : "/";
  const current = base ? pathname === root || pathname.startsWith(`${root}/`) : pathname === "/";
  const controlsId = `${base || "dashboard"}-navigation`;

  return <div className={`nav-group ${expanded ? "expanded" : ""}`}>
    <button className={`nav-group-toggle ${current ? "current" : ""}`} type="button" aria-expanded={expanded} aria-controls={controlsId} onClick={onToggle}>
      <Icon name={icon} size={17}/>
      <span>{label}</span>
      <i className="nav-group-arrow"><Icon name="arrow" size={14}/></i>
    </button>
    {expanded && <div className="nav-group-items" id={controlsId}>
      {items.map(([slug, name, itemIcon = "settings"]) => {
        const params = new URLSearchParams();
        if (preserveScope && departmentScope) params.set("departmentId", departmentScope);
        if (preserveScope && yearScope) params.set("year", yearScope);
        const route = `/${base}${slug ? `/${slug}` : ""}`;
        const href = `${route}${params.size ? `?${params}` : ""}`;
        return <Link className={`nav-item ${pathname === route ? "active" : ""}`} href={href} key={slug || base || "dashboard"} onClick={onNavigate}>
          <Icon name={itemIcon as Parameters<typeof Icon>[0]["name"]} size={16}/><span>{name}</span>
        </Link>;
      })}
    </div>}
  </div>;
}
