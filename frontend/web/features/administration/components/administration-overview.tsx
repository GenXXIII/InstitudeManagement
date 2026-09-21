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
import { useAdministrationMode } from "../administration-mode-context";

const advancedOnlySections = new Set<SettingSection>(["code-formats", "users-access", "security"]);

export function AdministrationOverview() {
  const { advanced } = useAdministrationMode();
  const [rows, setRows] = useState<Settings[]>();
  const [error, setError] = useState(false);
  const load = useCallback(async () => {
    try { setRows(await administrationApi.list()); setError(false); }
    catch { setError(true); }
  }, []);
  useEffect(() => { const controller = new AbortController(); void administrationApi.list(controller.signal).then(result => { setRows(result); setError(false); }).catch(() => { if (!controller.signal.aborted) setError(true); }); return () => controller.abort(); }, []);
  const bySection = useMemo(() => new Map(rows?.map(row => [row.section, row]) ?? []), [rows]);
  const visibleSections = useMemo(() => administrationSections.filter(item => advanced || !advancedOnlySections.has(item.section)), [advanced]);

  if (error) return <ErrorPage retry={() => void load()}/>;
  if (!rows) return <LoadingPage/>;

  const configured = visibleSections.filter(item => bySection.get(item.section)?.isConfigured).length;
  const percentage = Math.round((configured / visibleSections.length) * 100);
  const remaining = visibleSections.length - configured;
  const statusTone = percentage <= 25 ? "red" : percentage <= 50 ? "yellow" : percentage <= 75 ? "light-green" : "green";

  return <div className="viewport-data-page administration-page administration-overview-page">
    <PageHeading eyebrow="Institute administration" title="Settings" description="Configure the institute in priority order. Each group contains one clear policy function." actions={<AdministrationModeToggle compact/>}/>
    <section className={`administration-status-summary panel is-${statusTone}`} aria-label={`${percentage}% configured, ${remaining} remaining`}>
      <strong>{percentage}%</strong>
      <div className="administration-progress-track" role="progressbar" aria-label="Settings configuration progress" aria-valuemin={0} aria-valuemax={100} aria-valuenow={percentage}>
        <i style={{ clipPath: `inset(0 ${100 - percentage}% 0 0)` }}/>
      </div>
      <p><b>{configured}/{visibleSections.length}</b> configured <span aria-hidden="true">·</span> {remaining} remaining</p>
    </section>
    <section className="administration-overview-scroll">
      <div className="administration-category-catalog">{administrationCategories.map(category => ({ category, sections: visibleSections.filter(item => item.category === category.id) })).filter(item => item.sections.length).map(({ category, sections }) => <section key={category.id}>
        <header><div><h2>{category.title}</h2><p>{category.description}</p></div><span>{sections.length} sections</span></header>
        <div>
          {sections.map(item => <SectionCard definition={item} row={bySection.get(item.section)} key={item.section}/>)}
          {advanced && sections.some(item => item.section === "system") && <MaintenanceSectionCard row={bySection.get("system")}/>}
        </div>
      </section>)}</div>
    </section>
  </div>;
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
