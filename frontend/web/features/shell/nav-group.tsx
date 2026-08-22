"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Icon } from "@/components/icon";

export function NavGroup({ label, base, items, onNavigate, departmentScope = "", yearScope = "" }: { label: string; base: string; items: readonly (readonly string[])[]; onNavigate: () => void; departmentScope?: string; yearScope?: string }) {
  const pathname = usePathname();
  return <div className="nav-group"><div className="nav-label">{label}</div>{items.map(([slug, name, icon = "settings"]) => { const params = new URLSearchParams(); if (departmentScope) params.set("departmentId", departmentScope); if (yearScope) params.set("year", yearScope); const href = `/${base}/${slug}${params.size ? `?${params}` : ""}`; return <Link className={`nav-item ${pathname === `/${base}/${slug}` ? "active" : ""}`} href={href} key={slug} onClick={onNavigate}><Icon name={icon as Parameters<typeof Icon>[0]["name"]} size={16}/><span>{name}</span></Link>; })}</div>;
}
