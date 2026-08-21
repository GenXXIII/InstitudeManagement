"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Icon } from "@/components/icon";

export function NavGroup({ label, base, items, onNavigate, scope }: { label: string; base: string; items: readonly (readonly string[])[]; onNavigate: () => void; scope?: string }) {
  const pathname = usePathname();
  return <div className="nav-group"><div className="nav-label">{label}</div>{items.map(([slug, name, icon = "settings"]) => { const acceptsScope = !(base === "operation" && slug === "timetable"); const href = `/${base}/${slug}${scope && acceptsScope ? `?departmentId=${encodeURIComponent(scope)}` : ""}`; return <Link className={`nav-item ${pathname === `/${base}/${slug}` ? "active" : ""}`} href={href} key={slug} onClick={onNavigate}><Icon name={icon as Parameters<typeof Icon>[0]["name"]} size={16}/><span>{name}</span></Link>; })}</div>;
}
