"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { administrationApi } from "../administration-api";
import { administrationCategories, administrationSections, configurationSummary } from "../administration-config";
import { defaultSettings } from "../administration-defaults";
import type { SettingSection, Settings } from "../administration-types";
import { formatUpdatedAt } from "../settings-codec";
import { AdministrationModeToggle } from "./administration-mode-toggle";

export function AdministrationOverview() {
  const [rows, setRows] = useState<Settings[]>();
  const [error, setError] = useState(false);
  const load = useCallback(async () => {
    try { setRows(await administrationApi.list()); setError(false); }
    catch { setError(true); }
  }, []);
  useEffect(() => { const controller = new AbortController(); void administrationApi.list(controller.signal).then(result => { setRows(result); setError(false); }).catch(() => { if (!controller.signal.aborted) setError(true); }); return () => controller.abort(); }, []);
  const bySection = useMemo(() => new Map(rows?.map(row => [row.section, row]) ?? []), [rows]);

  if (error) return <ErrorPage retry={() => void load()}/>;
  if (!rows) return <LoadingPage/>;

  const configured = administrationSections.filter(item => bySection.get(item.section)?.isConfigured).length;
  const percentage = Math.round((configured / administrationSections.length) * 100);

  return <div className="viewport-data-page administration-page administration-overview-page">
    <PageHeading eyebrow="Institute administration" title="Settings" description="Configure institute identity, academic policy, people rules, communication, system behavior, and security readiness from one clear workspace." actions={<AdministrationModeToggle compact/>}/>
    <div className="administration-status-hero panel">
      <div><span>Configuration progress</span><strong>{percentage}%</strong><p>{configured} of {administrationSections.length} sections have saved database values.</p><div><i style={{ width: `${percentage}%` }}/></div></div>
      <dl><div><dt>Saved</dt><dd>{configured}</dd></div><div><dt>Review</dt><dd>{administrationSections.length - configured}</dd></div><div><dt>Categories</dt><dd>{administrationCategories.length}</dd></div></dl>
      <aside><Icon name="archive" size={18}/><span><strong>Settings are policy</strong><small>Record-backed departments, courses, classrooms, users, and roles remain separate from configuration.</small></span></aside>
    </div>
    <section className="administration-overview-scroll">
      <div className="administration-category-catalog">{administrationCategories.map(category => <section key={category.id}>
        <header><div><h2>{category.title}</h2><p>{category.description}</p></div><span>{administrationSections.filter(item => item.category === category.id).length} sections</span></header>
        <div>{administrationSections.filter(item => item.category === category.id).map(item => <MaintenanceCardPlacement definition={item} row={bySection.get(item.section)} systemRow={bySection.get("system")} key={item.section}/>)}</div>
      </section>)}</div>
    </section>
  </div>;
}

function MaintenanceCardPlacement({ definition, row, systemRow }: {
  definition: (typeof administrationSections)[number];
  row?: Settings;
  systemRow?: Settings;
}) {
  return <>
    <SectionCard definition={definition} row={row}/>
    {definition.section === "system" && <MaintenanceSectionCard row={systemRow}/>}
  </>;
}

function MaintenanceSectionCard({ row }: { row?: Settings }) {
  const values = { ...defaultSettings.system, ...(row?.values ?? {}) };
  const active = values.maintenanceEnabled === "true";
  return <Link className={`administration-section-card maintenance-section-card panel ${active ? "is-active" : ""}`} href="/settings/maintenance">
    <span className="administration-section-icon"><Icon name="settings" size={18}/></span>
    <div><span className={`administration-section-status ${active ? "" : "saved"}`}><i/>{active ? "Maintenance active" : "Normal service"}</span><h3>Maintenance mode</h3><p>Dedicated advanced control for platform availability, visitor messaging, impact, and recovery.</p><strong>Open full maintenance control</strong><small>{row?.updatedAtUtc ? `Updated ${formatUpdatedAt(row.updatedAtUtc)}` : "Uses the recommended normal-service default."}</small></div>
    <Icon name="arrow" size={15}/>
  </Link>;
}

function SectionCard({ definition, row }: { definition: (typeof administrationSections)[number]; row?: Settings }) {
  const values = { ...defaultSettings[definition.section], ...(row?.values ?? {}) };
  const configured = row?.isConfigured ?? false;
  return <Link className="administration-section-card panel" href={`/settings/${definition.section}`}>
    <span className="administration-section-icon"><Icon name={definition.icon} size={18}/></span>
    <div><span className={`administration-section-status ${configured ? "saved" : "review"}`}><i/>{configured ? "Saved" : "Review defaults"}</span><h3>{definition.title}</h3><p>{definition.description}</p><strong>{configurationSummary(definition.section as SettingSection, values)}</strong><small>{configured ? `Updated ${formatUpdatedAt(row?.updatedAtUtc ?? null)}` : "Sample defaults are shown until you apply this section."}</small></div>
    <Icon name="arrow" size={15}/>
  </Link>;
}
