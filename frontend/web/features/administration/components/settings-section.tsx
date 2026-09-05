"use client";

import Link from "next/link";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { configurationSummary, sectionDefinition } from "../administration-config";
import type { ConfigurationGroup, SettingSection } from "../administration-types";
import { formatUpdatedAt } from "../settings-codec";
import { useSettingsSection } from "../use-settings-section";
import { AdministrationModeToggle } from "./administration-mode-toggle";
import { SettingField } from "./setting-field";

export function SettingsSection({ section }: { section: SettingSection }) {
  const state = useSettingsSection(section);

  if (state.loadError) return <ErrorPage retry={state.retry}/>;
  if (!state.values) return <LoadingPage/>;

  const values = state.values;
  return <div className="viewport-data-page administration-page administration-section-page">
    <PageHeading eyebrow="Institute administration" title={state.definition.title} description={state.definition.description} actions={<><Link className="button secondary" href="/settings/overview" onClick={state.protectNavigation}><Icon name="dashboard" size={15}/>Settings overview</Link><AdministrationModeToggle compact/></>}/>
    <section className="administration-section-scroll">
        <div className="administration-current-summary">
          <span><Icon name={state.definition.icon} size={19}/></span>
          <div><small>{state.configured ? "Saved configuration" : "Recommended sample defaults"}</small><strong>{configurationSummary(section, values)}</strong><p>{policyCopy(section, state.configured, state.updatedAtUtc)}</p></div>
          <b className={`administration-section-status ${state.configured ? "saved" : "review"}`}><i/>{state.configured ? "Saved" : "Review"}</b>
        </div>

        {state.definition.managementLinks && <ManagementLinks links={state.definition.managementLinks}/>}

        <article className="administration-editor">
          <header><div><span>{state.advanced ? "Advanced configuration" : "Simple configuration"}</span><h2>Review and apply</h2><p>{state.advanced ? `All ${state.sectionEditableFields.length} available settings are shown.` : `${state.visibleEditableCount} essential settings are shown. ${state.hiddenFieldCount} advanced settings remain safely unchanged.`}</p></div><button type="button" className="button secondary" onClick={state.restoreRecommended}>Recommended defaults</button></header>
          <div className="administration-setting-groups">{state.visibleGroups.map(group => <SettingGroup group={group} values={values} onChange={state.change} key={group.title}/>)}</div>
          {(state.validationErrors.length > 0 || state.saveError) && <div className="form-error validation-summary administration-validation" role="alert"><strong>Fix these problems:</strong><ul>{[...state.validationErrors, ...(state.saveError ? [state.saveError] : [])].map(problem => <li key={problem}>{problem}</li>)}</ul></div>}
          <footer className="administration-save-bar">
            <div><span className={state.saved ? "administration-save-message show" : "administration-save-message"}>Settings saved successfully.</span>{!state.saved && <span>{state.changedCount > 0 ? `${state.changedCount} unsaved ${state.changedCount === 1 ? "change" : "changes"}` : state.configured ? `Saved · ${formatUpdatedAt(state.updatedAtUtc)}` : "Review and apply the sample defaults to store this section."}</span>}</div>
            <button className="button secondary" type="button" onClick={() => state.setValues({ ...state.baseline })} disabled={!state.changedCount || state.saving}>Undo</button>
            <button className="button primary" type="button" onClick={() => void state.save()} disabled={state.saving || (state.configured && !state.changedCount)}>{state.saving ? "Applying…" : "Apply settings"}</button>
          </footer>
        </article>
    </section>
    <nav className="administration-section-pager" aria-label="Settings section sequence">
      {state.previous ? <Link href={`/settings/${state.previous.section}`} onClick={state.protectNavigation}><Icon name="arrow" size={13}/><span><small>Previous</small><strong>{state.previous.shortTitle}</strong></span></Link> : <span/>}
      {state.next ? <Link href={`/settings/${state.next.section}`} onClick={state.protectNavigation}><span><small>Next</small><strong>{state.next.shortTitle}</strong></span><Icon name="arrow" size={13}/></Link> : <Link href="/settings/overview" onClick={state.protectNavigation}><span><small>Finish</small><strong>Settings overview</strong></span><Icon name="check" size={14}/></Link>}
    </nav>
  </div>;
}

function SettingGroup({ group, values, onChange }: { group: ConfigurationGroup; values: Record<string, string>; onChange: (key: string, value: string) => void }) {
  return <section className="administration-setting-group panel">
    <header><div><h3>{group.title}</h3><p>{group.description}</p></div><span>{group.fields.filter(item => item.type !== "derived").length} settings</span></header>
    <div>{group.fields.map(item => <SettingField definition={item} value={values[item.key] ?? ""} values={values} onChange={value => onChange(item.key, value)} key={item.key}/>)}</div>
  </section>;
}

function ManagementLinks({ links }: { links: NonNullable<ReturnType<typeof sectionDefinition>["managementLinks"]> }) {
  return <section className="administration-record-links panel"><header><div><span>Record-backed management</span><strong>Settings define defaults; records stay in their own workspaces.</strong></div></header><div>{links.map(link => link.href
    ? <Link href={link.href} key={link.title}><span><Icon name="archive" size={16}/><span><strong>{link.title}</strong><small>{link.description}</small></span></span><b>{link.label}<Icon name="arrow" size={12}/></b></Link>
    : <div className="deferred" key={link.title}><span><Icon name="archive" size={16}/><span><strong>{link.title}</strong><small>{link.description}</small></span></span><b>{link.label}</b></div>)}</div></section>;
}

function policyCopy(section: SettingSection, configured: boolean, updatedAtUtc: string | null) {
  if (!configured) return "Sample defaults are displayed locally. Apply them to create the saved database configuration.";
  if (section === "users-access" || section === "security") return `Saved policy baseline · ${formatUpdatedAt(updatedAtUtc)}. Identity, authorization, sessions, lockout, and 2FA require a future authentication module before enforcement.`;
  if (section === "notifications") return `Saved delivery policy · ${formatUpdatedAt(updatedAtUtc)}. Existing event switches are live; external email and SMS delivery still require configured services and secrets.`;
  if (section === "student-rules" || section === "teacher-rules") return `Saved policy · ${formatUpdatedAt(updatedAtUtc)}. Identifier and lifecycle rules become enforceable as their corresponding creation workflows adopt them.`;
  return `Saved configuration · ${formatUpdatedAt(updatedAtUtc)}. Existing supported workflows consume these values; newly introduced policy fields are retained for their related services.`;
}
