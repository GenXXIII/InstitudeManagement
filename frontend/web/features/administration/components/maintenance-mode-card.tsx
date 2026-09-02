"use client";

import { useEffect, useMemo, useState } from "react";
import { Icon } from "@/components/icon";
import { administrationApi } from "../administration-api";
import { defaultSettings } from "../administration-defaults";
import type { Settings } from "../administration-types";
import { validateSettings } from "../settings-validation";
import { useInstituteSettings } from "../institute-settings-context";

const maintenanceKeys = ["maintenanceEnabled", "maintenanceMessage", "allowAdministratorsDuringMaintenance"] as const;

export function MaintenanceModeCard({ row, onSaved }: { row?: Settings; onSaved: () => Promise<void> }) {
  const { refresh } = useInstituteSettings();
  const initialValues = { ...defaultSettings.system, ...(row?.values ?? {}) };
  const [baseline, setBaseline] = useState<Record<string, string>>(initialValues);
  const [values, setValues] = useState<Record<string, string>>(() => pickMaintenanceValues(initialValues));
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState("");

  const dirty = useMemo(() => maintenanceKeys.some(key => values[key] !== baseline[key]), [baseline, values]);
  const plannedActive = values.maintenanceEnabled === "true";
  const active = baseline.maintenanceEnabled === "true";
  const message = values.maintenanceMessage ?? "";

  useEffect(() => {
    const warn = (event: BeforeUnloadEvent) => { if (dirty) event.preventDefault(); };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirty]);

  function change(key: string, value: string) {
    setValues(current => ({ ...current, [key]: value }));
    setError("");
    setSaved(false);
  }

  async function apply() {
    const enabling = plannedActive && !active;
    const disabling = !plannedActive && active;
    if (enabling && !window.confirm("Activate maintenance mode now? Business pages and APIs will immediately show the maintenance experience.")) return;
    if (disabling && !window.confirm("End maintenance mode now and restore normal business access?")) return;
    if (!message.trim()) { setError("Enter the message people will see during maintenance."); return; }

    const pendingValues = { ...baseline, ...pickMaintenanceValues(values) };
    const problems = validateSettings("system", pendingValues);
    if (problems.length) { setError(problems.join(" ")); return; }

    setSaving(true);
    setError("");
    try {
      const result = await administrationApi.save("system", pendingValues);
      const applied = { ...defaultSettings.system, ...result.values };
      setBaseline(applied);
      setValues(pickMaintenanceValues(applied));
      await Promise.all([refresh(), onSaved()]);
      setSaved(true);
      window.setTimeout(() => setSaved(false), 2800);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Could not apply the maintenance controls.");
    } finally {
      setSaving(false);
    }
  }

  return <article className={`maintenance-control-card maintenance-overview-card ${plannedActive ? "plans-maintenance" : "plans-normal"}`}>
    <header>
      <span className="maintenance-control-icon"><Icon name="settings" size={21}/></span>
      <div><small>Advanced service control</small><h2>Maintenance mode</h2><p>Control application availability outside ordinary System Settings.</p></div>
      <b className={`maintenance-current-state ${active ? "active" : "normal"}`}><i/>{active ? "Maintenance active" : "Normal service"}</b>
    </header>

    <div className="maintenance-control-layout">
      <section className="maintenance-command-panel">
        <div><span>Planned service state</span><h3>Choose how the platform should respond</h3><p>This is an explicit service command, not a simple preference switch.</p></div>
        <div className="maintenance-state-options" role="radiogroup" aria-label="Maintenance service state">
          <button type="button" role="radio" aria-checked={!plannedActive} className={!plannedActive ? "selected normal" : "normal"} onClick={() => change("maintenanceEnabled", "false")}>
            <span><Icon name="check" size={17}/></span><strong>Normal operation</strong><small>Business pages, live updates, and APIs remain available.</small>
          </button>
          <button type="button" role="radio" aria-checked={plannedActive} className={plannedActive ? "selected maintenance" : "maintenance"} onClick={() => change("maintenanceEnabled", "true")}>
            <span><Icon name="settings" size={17}/></span><strong>Maintenance lockdown</strong><small>Business access returns HTTP 503 and displays the maintenance page.</small>
          </button>
        </div>
        {plannedActive && <div className="maintenance-impact-warning"><Icon name="bell" size={16}/><div><strong>Immediate platform impact</strong><span>Applying this plan pauses live updates and replaces every business screen with the maintenance experience.</span></div></div>}
      </section>

      <section className="maintenance-message-panel">
        <div><span>Public maintenance message</span><b>{message.length}/1000</b></div>
        <textarea value={message} maxLength={1000} rows={5} onChange={event => change("maintenanceMessage", event.target.value)} placeholder="Explain why the platform is unavailable and when people should return."/>
        <div className="maintenance-message-preview"><small>Visitor preview</small><p>{message.trim() || "A maintenance message is required."}</p></div>
      </section>

      <aside className="maintenance-recovery-panel">
        <span>Recovery and enforcement</span>
        <ul>
          <li><Icon name="check" size={14}/><div><strong>Health endpoint</strong><small>Remains available for Docker and monitoring.</small></div></li>
          <li><Icon name="check" size={14}/><div><strong>Settings workspace</strong><small>Remains available so maintenance can be ended safely.</small></div></li>
          <li className="unavailable"><Icon name="archive" size={14}/><div><strong>Administrator bypass</strong><small>Unavailable until authentication and administrator roles are implemented.</small></div></li>
        </ul>
      </aside>
    </div>

    {error && <div className="maintenance-control-error" role="alert"><Icon name="bell" size={15}/><span>{error}</span></div>}
    <footer>
      <div>{saved ? <strong>Maintenance controls applied successfully.</strong> : <span>{dirty ? "Maintenance plan has unapplied changes." : `Saved service state: ${active ? "Maintenance" : "Normal"}.`}</span>}</div>
      <button type="button" className="button secondary" onClick={() => { setValues(pickMaintenanceValues(baseline)); setError(""); }} disabled={!dirty || saving}>Undo</button>
      <button type="button" className={`button ${plannedActive ? "maintenance-apply" : "primary"}`} onClick={() => void apply()} disabled={!dirty || saving}>{saving ? "Applying…" : plannedActive ? "Activate maintenance" : "Restore normal service"}</button>
    </footer>
  </article>;
}

function pickMaintenanceValues(values: Record<string, string>) {
  return Object.fromEntries(maintenanceKeys.map(key => [key, values[key] ?? ""]));
}
