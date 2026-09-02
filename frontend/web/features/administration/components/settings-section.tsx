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
  const dirty = changedCount > 0;
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
      setValues(applied);
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

  function change(key: string, value: string) {
    setValues(current => normalizeSection(section, { ...current!, [key]: value }));
    setValidationErrors([]);
    setSaveError("");
    setSaved(false);
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
            <button className="button secondary" type="button" onClick={() => setValues({ ...baseline })} disabled={!changedCount || saving}>Undo</button>
            <button className="button primary" type="button" onClick={() => void save()} disabled={saving || (configured && !changedCount)}>{saving ? "Applying…" : "Apply settings"}</button>
          </footer>
        </article>
    </section>
    <nav className="administration-section-pager" aria-label="Settings section sequence">
      {previous ? <Link href={`/settings/${previous.section}`} onClick={protectNavigation}><Icon name="arrow" size={13}/><span><small>Previous</small><strong>{previous.shortTitle}</strong></span></Link> : <span/>}
      {next ? <Link href={`/settings/${next.section}`} onClick={protectNavigation}><span><small>Next</small><strong>{next.shortTitle}</strong></span><Icon name="arrow" size={13}/></Link> : <Link href="/settings/overview" onClick={protectNavigation}><span><small>Finish</small><strong>Settings overview</strong></span><Icon name="check" size={14}/></Link>}
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
