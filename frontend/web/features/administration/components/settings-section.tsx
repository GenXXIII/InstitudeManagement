"use client";

import Link from "next/link";
import { useEffect, useMemo, useRef, useState } from "react";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { administrationApi } from "../administration-api";
import { administrationSections, configurationSummary, editableSectionFields, sectionDefinition, simpleSectionFieldKeys } from "../administration-config";
import { defaultSettings } from "../administration-defaults";
import type { ConfigurationGroup, SettingSection } from "../administration-types";
import { formatUpdatedAt } from "../settings-codec";
import { validateSettings } from "../settings-validation";
import { useAdministrationMode } from "../administration-mode-context";
import { useInstituteSettings } from "../institute-settings-context";
import { AdministrationModeToggle } from "./administration-mode-toggle";
import { SettingField } from "./setting-field";

const maintenanceKeys = new Set(["maintenanceEnabled", "maintenanceMessage", "allowAdministratorsDuringMaintenance"]);

export function SettingsSection({ section }: { section: SettingSection }) {
  const definition = sectionDefinition(section);
  const { refresh } = useInstituteSettings();
  const { advanced } = useAdministrationMode();
  const [values, setValues] = useState<Record<string, string>>();
  const [baseline, setBaseline] = useState<Record<string, string>>({});
  const [configured, setConfigured] = useState(false);
  const [updatedAtUtc, setUpdatedAtUtc] = useState<string | null>(null);
  const [loadError, setLoadError] = useState(false);
  const [saveError, setSaveError] = useState("");
  const [validationErrors, setValidationErrors] = useState<string[]>([]);
  const [saved, setSaved] = useState(false);
  const [saving, setSaving] = useState(false);
  const [maintenanceSaved, setMaintenanceSaved] = useState(false);
  const [maintenanceSaving, setMaintenanceSaving] = useState(false);
  const [maintenanceError, setMaintenanceError] = useState("");
  const [reloadKey, setReloadKey] = useState(0);
  const saveController = useRef<AbortController | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      void administrationApi.get(section, controller.signal).then(result => {
        const merged = normalizeSection(section, { ...defaultSettings[section], ...result.values });
        setValues(merged);
        setBaseline(merged);
        setConfigured(result.isConfigured);
        setUpdatedAtUtc(result.updatedAtUtc);
        setLoadError(false);
        setSaveError("");
        setMaintenanceError("");
        setValidationErrors([]);
      }).catch(() => { if (!controller.signal.aborted) setLoadError(true); });
    }, 0);
    return () => { window.clearTimeout(timer); controller.abort(); };
  }, [reloadKey, section]);

  const editableFields = useMemo(() => editableSectionFields(section), [section]);
  const sectionEditableFields = useMemo(() => section === "system" ? editableFields.filter(field => !maintenanceKeys.has(field.key)) : editableFields, [editableFields, section]);
  const visibleGroups = useMemo(() => {
    const sectionGroups = section === "system"
      ? definition.groups.map(group => ({ ...group, fields: group.fields.filter(field => !maintenanceKeys.has(field.key)) })).filter(group => group.fields.length > 0)
      : definition.groups;
    if (advanced) return sectionGroups;
    const visibleKeys = new Set(simpleSectionFieldKeys(section, values ?? {}));
    return sectionGroups
      .map(group => ({ ...group, fields: group.fields.filter(field => visibleKeys.has(field.key)) }))
      .filter(group => group.fields.length > 0);
  }, [advanced, definition.groups, section, values]);
  const visibleEditableCount = useMemo(() => visibleGroups.flatMap(group => group.fields).filter(field => field.type !== "derived" && !field.readOnly).length, [visibleGroups]);
  const hiddenFieldCount = Math.max(0, sectionEditableFields.length - visibleEditableCount);
  const changedCount = useMemo(() => values ? sectionEditableFields.filter(item => values[item.key] !== baseline[item.key]).length : 0, [baseline, sectionEditableFields, values]);
  const maintenanceChangedCount = useMemo(() => section === "system" && values ? [...maintenanceKeys].filter(key => values[key] !== baseline[key]).length : 0, [baseline, section, values]);
  const dirty = changedCount + maintenanceChangedCount > 0;
  const currentIndex = administrationSections.findIndex(item => item.section === section);
  const previous = administrationSections[currentIndex - 1];
  const next = administrationSections[currentIndex + 1];

  useEffect(() => {
    const warn = (event: BeforeUnloadEvent) => { if (dirty) event.preventDefault(); };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirty]);

  if (loadError) return <ErrorPage retry={() => { setLoadError(false); setReloadKey(current => current + 1); }}/>;
  if (!values) return <LoadingPage/>;

  async function save() {
    const maintenanceDraft = section === "system" ? pickValues(values!, maintenanceKeys) : {};
    const pendingValues = normalizeSection(section, section === "system" ? { ...values!, ...pickValues(baseline, maintenanceKeys) } : values!);
    const problems = validateSettings(section, pendingValues);
    setValidationErrors(problems);
    setSaveError("");
    if (problems.length) return;
    setSaving(true);
    const controller = new AbortController();
    saveController.current = controller;
    try {
      const result = await administrationApi.save(section, pendingValues, controller.signal);
      const applied = normalizeSection(section, { ...defaultSettings[section], ...result.values });
      setValues(section === "system" ? { ...applied, ...maintenanceDraft } : applied);
      setBaseline(applied);
      setConfigured(result.isConfigured);
      setUpdatedAtUtc(result.updatedAtUtc);
      setSaveError("");
      setValidationErrors([]);
      await refresh();
      setSaved(true);
      window.setTimeout(() => setSaved(false), 2800);
    } catch (reason) {
      if (!controller.signal.aborted) setSaveError(reason instanceof Error ? reason.message : "Could not save this Settings section.");
    } finally {
      if (saveController.current === controller) saveController.current = null;
      setSaving(false);
    }
  }

  async function saveMaintenance() {
    const enabling = values!.maintenanceEnabled === "true" && baseline.maintenanceEnabled !== "true";
    const disabling = values!.maintenanceEnabled !== "true" && baseline.maintenanceEnabled === "true";
    if (enabling && !window.confirm("Activate maintenance mode now? Business pages and APIs will immediately show the maintenance experience.")) return;
    if (disabling && !window.confirm("End maintenance mode now and restore normal business access?")) return;
    if (!values!.maintenanceMessage?.trim()) { setMaintenanceError("Enter the message people will see during maintenance."); return; }

    const standardDraft = Object.fromEntries(Object.entries(values!).filter(([key]) => !maintenanceKeys.has(key)));
    const pendingValues = normalizeSection(section, { ...baseline, ...pickValues(values!, maintenanceKeys) });
    const problems = validateSettings(section, pendingValues);
    if (problems.length) { setMaintenanceError(problems.join(" ")); return; }

    setMaintenanceSaving(true);
    setMaintenanceError("");
    const controller = new AbortController();
    saveController.current = controller;
    try {
      const result = await administrationApi.save(section, pendingValues, controller.signal);
      const applied = normalizeSection(section, { ...defaultSettings[section], ...result.values });
      setValues({ ...applied, ...standardDraft });
      setBaseline(applied);
      setConfigured(result.isConfigured);
      setUpdatedAtUtc(result.updatedAtUtc);
      await refresh();
      setMaintenanceSaved(true);
      window.setTimeout(() => setMaintenanceSaved(false), 2800);
    } catch (reason) {
      if (!controller.signal.aborted) setMaintenanceError(reason instanceof Error ? reason.message : "Could not apply the maintenance controls.");
    } finally {
      if (saveController.current === controller) saveController.current = null;
      setMaintenanceSaving(false);
    }
  }

  function change(key: string, value: string) {
    setValues(current => normalizeSection(section, { ...current!, [key]: value }));
    setValidationErrors([]);
    setSaveError("");
    setSaved(false);
  }

  function changeMaintenance(key: string, value: string) {
    change(key, value);
    setMaintenanceError("");
    setMaintenanceSaved(false);
  }

  function restoreRecommended() {
    const knownKeys = new Set(Object.keys(defaultSettings[section]));
    const unknown = Object.fromEntries(Object.entries(baseline).filter(([key]) => !knownKeys.has(key)));
    const recommended = normalizeSection(section, { ...unknown, ...defaultSettings[section] });
    setValues(section === "system" ? { ...recommended, ...pickValues(values!, maintenanceKeys) } : recommended);
    setValidationErrors([]);
    setSaveError("");
    setSaved(false);
  }

  function protectNavigation(event: React.MouseEvent<HTMLAnchorElement>) {
    if (dirty && !window.confirm("Discard the unsaved Settings changes?")) event.preventDefault();
  }

  return <div className="viewport-data-page administration-page administration-section-page">
    <PageHeading eyebrow="Institute administration" title={definition.title} description={definition.description} actions={<><Link className="button secondary" href="/settings/overview" onClick={protectNavigation}><Icon name="dashboard" size={15}/>Settings overview</Link><AdministrationModeToggle compact/></>}/>
    <section className="administration-section-scroll">
        <div className="administration-current-summary">
          <span><Icon name={definition.icon} size={19}/></span>
          <div><small>{configured ? "Saved configuration" : "Recommended sample defaults"}</small><strong>{configurationSummary(section, values)}</strong><p>{policyCopy(section, configured, updatedAtUtc)}</p></div>
          <b className={`administration-section-status ${configured ? "saved" : "review"}`}><i/>{configured ? "Saved" : "Review"}</b>
        </div>

        {definition.managementLinks && <ManagementLinks links={definition.managementLinks}/>} 

        <article className="administration-editor">
          <header><div><span>{advanced ? "Advanced configuration" : "Simple configuration"}</span><h2>Review and apply</h2><p>{advanced ? `All ${sectionEditableFields.length} available settings are shown.` : `${visibleEditableCount} essential settings are shown. ${hiddenFieldCount} advanced settings remain safely unchanged.`}</p></div><button type="button" className="button secondary" onClick={restoreRecommended}>Recommended defaults</button></header>
          <div className="administration-setting-groups">{visibleGroups.map(group => <SettingGroup group={group} values={values} onChange={change} key={group.title}/>)}</div>
          {(validationErrors.length > 0 || saveError) && <div className="form-error validation-summary administration-validation" role="alert"><strong>Fix these problems:</strong><ul>{[...validationErrors, ...(saveError ? [saveError] : [])].map(problem => <li key={problem}>{problem}</li>)}</ul></div>}
          <footer className="administration-save-bar">
            <div><span className={saved ? "administration-save-message show" : "administration-save-message"}>Settings saved successfully.</span>{!saved && <span>{changedCount > 0 ? `${changedCount} unsaved ${changedCount === 1 ? "change" : "changes"}` : configured ? `Saved · ${formatUpdatedAt(updatedAtUtc)}` : "Review and apply the sample defaults to store this section."}</span>}</div>
            <button className="button secondary" type="button" onClick={() => setValues(current => section === "system" ? { ...baseline, ...pickValues(current!, maintenanceKeys) } : { ...baseline })} disabled={!changedCount || saving || maintenanceSaving}>Undo</button>
            <button className="button primary" type="button" onClick={() => void save()} disabled={saving || maintenanceSaving || (configured && !changedCount)}>{saving ? "Applying…" : "Apply settings"}</button>
          </footer>
        </article>

        {section === "system" && <MaintenanceModeCard
          values={values}
          baseline={baseline}
          dirty={maintenanceChangedCount > 0}
          saving={maintenanceSaving}
          saved={maintenanceSaved}
          error={maintenanceError}
          onChange={changeMaintenance}
          onApply={() => void saveMaintenance()}
          onUndo={() => setValues(current => ({ ...current!, ...pickValues(baseline, maintenanceKeys) }))}
        />}

    </section>
    <nav className="administration-section-pager" aria-label="Settings section sequence">
      {previous ? <Link href={`/settings/${previous.section}`} onClick={protectNavigation}><Icon name="arrow" size={13}/><span><small>Previous</small><strong>{previous.shortTitle}</strong></span></Link> : <span/>}
      {next ? <Link href={`/settings/${next.section}`} onClick={protectNavigation}><span><small>Next</small><strong>{next.shortTitle}</strong></span><Icon name="arrow" size={13}/></Link> : <Link href="/settings/overview" onClick={protectNavigation}><span><small>Finish</small><strong>Settings overview</strong></span><Icon name="check" size={14}/></Link>}
    </nav>
  </div>;
}

function MaintenanceModeCard({ values, baseline, dirty, saving, saved, error, onChange, onApply, onUndo }: {
  values: Record<string, string>;
  baseline: Record<string, string>;
  dirty: boolean;
  saving: boolean;
  saved: boolean;
  error: string;
  onChange: (key: string, value: string) => void;
  onApply: () => void;
  onUndo: () => void;
}) {
  const plannedActive = values.maintenanceEnabled === "true";
  const active = baseline.maintenanceEnabled === "true";
  const message = values.maintenanceMessage ?? "";

  return <article className={`maintenance-control-card ${plannedActive ? "plans-maintenance" : "plans-normal"}`}>
    <header>
      <span className="maintenance-control-icon"><Icon name="settings" size={21}/></span>
      <div><small>Advanced service control</small><h2>Maintenance mode</h2><p>Control application availability separately from ordinary System settings.</p></div>
      <b className={`maintenance-current-state ${active ? "active" : "normal"}`}><i/>{active ? "Maintenance active" : "Normal service"}</b>
    </header>

    <div className="maintenance-control-layout">
      <section className="maintenance-command-panel">
        <div><span>Planned service state</span><h3>Choose how the platform should respond</h3><p>This is an explicit service command, not a simple preference switch.</p></div>
        <div className="maintenance-state-options" role="radiogroup" aria-label="Maintenance service state">
          <button type="button" role="radio" aria-checked={!plannedActive} className={!plannedActive ? "selected normal" : "normal"} onClick={() => onChange("maintenanceEnabled", "false")}>
            <span><Icon name="check" size={17}/></span><strong>Normal operation</strong><small>Business pages, live updates, and APIs remain available.</small>
          </button>
          <button type="button" role="radio" aria-checked={plannedActive} className={plannedActive ? "selected maintenance" : "maintenance"} onClick={() => onChange("maintenanceEnabled", "true")}>
            <span><Icon name="settings" size={17}/></span><strong>Maintenance lockdown</strong><small>Business access returns HTTP 503 and displays the maintenance page.</small>
          </button>
        </div>
        {plannedActive && <div className="maintenance-impact-warning"><Icon name="bell" size={16}/><div><strong>Immediate platform impact</strong><span>Applying this plan pauses live updates and replaces every business screen with the maintenance experience.</span></div></div>}
      </section>

      <section className="maintenance-message-panel">
        <div><span>Public maintenance message</span><b>{message.length}/1000</b></div>
        <textarea value={message} maxLength={1000} rows={5} onChange={event => onChange("maintenanceMessage", event.target.value)} placeholder="Explain why the platform is unavailable and when people should return."/>
        <div className="maintenance-message-preview"><small>Visitor preview</small><p>{message.trim() || "A maintenance message is required."}</p></div>
      </section>

      <aside className="maintenance-recovery-panel">
        <span>Recovery and enforcement</span>
        <ul>
          <li><Icon name="check" size={14}/><div><strong>Health endpoint</strong><small>Remains available for Docker and monitoring.</small></div></li>
          <li><Icon name="check" size={14}/><div><strong>System Settings</strong><small>Remains available so maintenance can be ended safely.</small></div></li>
          <li className="unavailable"><Icon name="archive" size={14}/><div><strong>Administrator bypass</strong><small>Unavailable until authentication and administrator roles are implemented.</small></div></li>
        </ul>
      </aside>
    </div>

    {error && <div className="maintenance-control-error" role="alert"><Icon name="bell" size={15}/><span>{error}</span></div>}
    <footer>
      <div>{saved ? <strong>Maintenance controls applied successfully.</strong> : <span>{dirty ? "Maintenance plan has unapplied changes." : `Saved service state: ${active ? "Maintenance" : "Normal"}.`}</span>}</div>
      <button type="button" className="button secondary" onClick={onUndo} disabled={!dirty || saving}>Undo</button>
      <button type="button" className={`button ${plannedActive ? "maintenance-apply" : "primary"}`} onClick={onApply} disabled={!dirty || saving}>{saving ? "Applying…" : plannedActive ? "Activate maintenance" : "Restore normal service"}</button>
    </footer>
  </article>;
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

function normalizeSection(section: SettingSection, values: Record<string, string>) {
  if (section !== "semester") return values;
  const prefix = values.currentTerm === "Semester 2" ? "semester2" : values.currentTerm === "Summer Term" ? "summer" : "semester1";
  return { ...values, startsOn: values[`${prefix}StartsOn`] ?? "", endsOn: values[`${prefix}EndsOn`] ?? "" };
}

function pickValues(values: Record<string, string>, keys: Set<string>) {
  return Object.fromEntries([...keys].map(key => [key, values[key] ?? ""]));
}

function policyCopy(section: SettingSection, configured: boolean, updatedAtUtc: string | null) {
  if (!configured) return "Sample defaults are displayed locally. Apply them to create the saved database configuration.";
  if (section === "users-access" || section === "security") return `Saved policy baseline · ${formatUpdatedAt(updatedAtUtc)}. Identity, authorization, sessions, lockout, and 2FA require a future authentication module before enforcement.`;
  if (section === "notifications") return `Saved delivery policy · ${formatUpdatedAt(updatedAtUtc)}. Existing event switches are live; external email and SMS delivery still require configured services and secrets.`;
  if (section === "student-rules" || section === "teacher-rules") return `Saved policy · ${formatUpdatedAt(updatedAtUtc)}. Identifier and lifecycle rules become enforceable as their corresponding creation workflows adopt them.`;
  return `Saved configuration · ${formatUpdatedAt(updatedAtUtc)}. Existing supported workflows consume these values; newly introduced policy fields are retained for their related services.`;
}
