"use client";

import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { administrationApi } from "./administration-api";
import type { Settings } from "./administration-types";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { Icon } from "@/components/icon";
import { useInstituteSettings } from "./institute-settings-context";

const titles: Record<string, string> = { institute: "Institute profile", "academic-year": "Academic year", semester: "Semester / term", departments: "Department settings", courses: "Course settings", classrooms: "Classroom settings", "attendance-rules": "Attendance rules", "grade-rules": "Grade rules", notifications: "Notification preferences", system: "System preferences" };
const descriptions: Record<string, string> = { institute: "Control the institute identity shown throughout the application.", "academic-year": "Set the active year displayed in the shell and academic workflows; expiry advances the cohort year automatically.", semester: "Define both semester date windows. Expiry activates the next term and starts fresh grade and attendance ledgers.", departments: "Control department-head and cross-department teaching rules.", courses: "Set new-course defaults and teacher assignment requirements.", classrooms: "Set learning-space defaults, device requirements, and room-sharing rules.", "attendance-rules": "Choose how attendance is captured and whether corrections and alerts are allowed.", "grade-rules": "Set the A, B, C, D, E, and F score boundaries used everywhere.", notifications: "Control which operational events create institute notifications.", system: "Control language metadata, time zone, date display, and live refresh timing." };

export default function SettingsPage() {
  const { refresh } = useInstituteSettings();
  const { section } = useParams<{ section: string }>(); const [data, setData] = useState<Settings>(); const [values, setValues] = useState<Record<string, string>>({}); const [error, setError] = useState(false); const [saveError, setSaveError] = useState(""); const [saved, setSaved] = useState(false); const [saving, setSaving] = useState(false);
  const load = useCallback(() => { administrationApi.get(section).then(result => { setData(result); setValues(result.values); }).catch(() => setError(true)); }, [section]); useEffect(load, [load]);
  if (error) return <ErrorPage retry={load}/>; if (!data) return <LoadingPage/>;
  async function save() { setSaving(true); setSaveError(""); try { const result = await administrationApi.save(section, values); setValues(result.values); await refresh(); setSaved(true); setTimeout(() => setSaved(false), 2500); } catch (reason) { setSaveError(reason instanceof Error ? reason.message : "Could not save these settings."); } finally { setSaving(false); } }
  const hasValues = Object.keys(values).length > 0;
  return <>
    <PageHeading eyebrow="System setting" title={titles[section] ?? "Configuration"} description={descriptions[section] ?? "Configure how this part of the institute management system should work."}/>
    <section className="settings-layout">
      <article className="panel settings-panel"><div className="settings-intro"><div><h3>{titles[section] ?? "Settings"}</h3><p>Changes are saved to the institute database and recorded in the audit history.</p></div><span className="settings-badge">Current configuration</span></div>
        {hasValues ? <div className="settings-form">{Object.entries(values).map(([key, value]) => <SettingField key={key} name={key} value={value} onChange={next => setValues({ ...values, [key]: next })}/>)}</div> : <div className="empty-state"><strong>No custom options yet</strong><span>This section uses the institute-wide defaults.</span></div>}
        {saveError && <p className="form-error">{saveError}</p>}<div className="settings-actions"><span className={saved ? "save-message show" : "save-message"}>✓ Changes saved</span><button className="button secondary" onClick={load}>Cancel</button><button className="button primary" onClick={save} disabled={!hasValues || saving}>{saving ? "Validating…" : "Save changes"}</button></div>
      </article>
      <aside className={`panel settings-preview settings-preview-${section}`}><SettingsPreview section={section} values={values}/><div className="settings-impact"><Icon name="archive" size={15}/><div><strong>Immediate and recorded</strong><span>Saving updates live behavior and writes an immutable audit entry.</span></div></div></aside>
    </section>
  </>;
}

function SettingsPreview({ section, values }: { section: string; values: Record<string, string> }) {
  const enabled = (key: string) => values[key] === "true";
  if (section === "institute") return <><span className="preview-kicker">Public institute identity</span><div className="institute-preview-mark">{values.shortName || "INK"}</div><h3>{values.name}</h3><p>{values.address}</p><div className="identity-contact"><span>{values.email}</span><span>{values.phone}</span></div></>;
  if (section === "academic-year" || section === "semester") return <><span className="preview-kicker">Active academic window</span><div className="term-preview"><Icon name="calendar" size={22}/><strong>{values.currentYear ?? values.currentTerm}</strong><span>Current {section === "semester" ? "term · automatic rollover" : "academic year · automatic promotion"}</span></div><div className="term-track"><i/><i/><i/></div><div className="term-dates"><span><small>Starts</small>{values.startsOn}</span><span><small>Ends</small>{values.endsOn}</span></div>{section === "semester" && <div className="term-dates"><span><small>Semester 1</small>{values.semester1StartsOn} – {values.semester1EndsOn}</span><span><small>Semester 2</small>{values.semester2StartsOn} – {values.semester2EndsOn}</span></div>}</>;
  if (section === "departments") return <><span className="preview-kicker">Organization rules</span><div className="department-rule-preview"><div><Icon name="building" size={21}/><strong>Department</strong></div><i/><div><Icon name="teacher" size={21}/><strong>Head teacher</strong></div></div><RulePill active={enabled("requireDepartmentHead")} label="Head required"/><RulePill active={enabled("allowCrossDepartmentTeaching")} label="Cross-department teaching"/></>;
  if (section === "courses") return <><span className="preview-kicker">New course defaults</span><div className="course-rule-preview"><div>NEW</div><h3>Course template</h3><p>Teacher relationship {enabled("requireAssignedTeacher") ? "required" : "optional"}</p><span><b>{values.defaultCredits}</b> credits</span><span><b>{values.defaultCapacity}</b> seats</span></div></>;
  if (section === "classrooms") return <><span className="preview-kicker">New classroom defaults</span><div className="classroom-rule-preview"><Icon name="room" size={24}/><strong>Room template</strong><b>{values.defaultCapacity}</b><span>default seats</span></div><RulePill active={enabled("attendanceDeviceRequired")} label="Device required"/><RulePill active={enabled("allowSharedRooms")} label="Shared rooms"/></>;
  if (section === "attendance-rules") return <><span className="preview-kicker">Attendance workflow</span><div className="attendance-flow-preview"><div><Icon name="check" size={17}/><span>{values.method}</span></div><i/><div><strong>{values.lateThresholdMinutes}</strong><span>minute late limit</span></div><i/><div><Icon name="bell" size={17}/><span>Notify</span></div></div><div className="preview-rule-count">{Object.entries(values).filter(([, value]) => value === "true").length} automation rules enabled</div></>;
  if (section === "grade-rules") return <><span className="preview-kicker">Letter grade boundaries</span><div className="grade-rule-preview">{["a", "b", "c", "d", "e"].map(letter => <div key={letter} className={`grade-${letter}`}><strong>{letter.toUpperCase()}</strong><span>{values[`${letter}Minimum`]}+</span></div>)}<div className="grade-f"><strong>F</strong><span>Below {values.eMinimum}</span></div></div><p>Saving recalculates existing grade letters and dashboard distribution.</p></>;
  if (section === "notifications") return <><span className="preview-kicker">Notification event routing</span><div className="notification-rule-preview">{Object.entries(values).map(([key, value]) => <div key={key}><Icon name="bell" size={14}/><span>{pretty(key)}</span><i className={value === "true" ? "on" : ""}/></div>)}</div></>;
  return <><span className="preview-kicker">Local runtime behavior</span><div className="system-rule-preview"><div><span>Time zone</span><strong>{values.timeZone}</strong></div><div><span>Language</span><strong>{values.language}</strong></div><div><span>Date format</span><strong>{values.dateFormat}</strong></div><div><span>Refresh</span><strong>{values.autoRefreshSeconds}s</strong></div></div></>;
}

function RulePill({ active, label }: { active: boolean; label: string }) { return <div className={`rule-pill ${active ? "active" : ""}`}><i/><span>{label}</span><strong>{active ? "On" : "Off"}</strong></div>; }
function pretty(value: string) { return value.replace(/([A-Z])/g, " $1").replace(/^./, first => first.toUpperCase()); }

function SettingField({ name, value, onChange }: { name: string; value: string; onChange: (value: string) => void }) {
  const label = name.replace(/([A-Z])/g, " $1").replace(/^./, value => value.toUpperCase());
  if (value === "true" || value === "false") return <label className="toggle-field"><div><strong>{label}</strong><span>Enable or disable this rule.</span></div><input type="checkbox" checked={value === "true"} onChange={event => onChange(String(event.target.checked))}/><i/></label>;
  if (name === "method") return <label className="setting-field"><span>{label}</span><select value={value} onChange={e => onChange(e.target.value)}><option>Manual</option><option>ID Card</option><option>QR Code</option><option>Biometric</option></select></label>;
  if (name === "currentTerm") return <label className="setting-field"><span>{label}</span><select value={value} onChange={e => onChange(e.target.value)}><option>Semester 1</option><option>Semester 2</option></select></label>;
  return <label className="setting-field"><span>{label}</span><input type={name.toLowerCase().includes("date") || name.endsWith("On") ? "date" : name.toLowerCase().includes("email") ? "email" : name.toLowerCase().includes("minimum") || name.toLowerCase().includes("minutes") || name.toLowerCase().includes("seconds") ? "number" : "text"} value={value} onChange={e => onChange(e.target.value)}/></label>;
}
