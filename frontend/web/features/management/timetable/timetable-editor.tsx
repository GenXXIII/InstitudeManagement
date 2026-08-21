"use client";

import { useEffect, useMemo, useState } from "react";
import { Icon } from "@/components/icon";
import { useInstituteSettings } from "@/features/administration/institute-settings-context";
import type { Field, ManagementItem, References } from "../management-types";
import { EditorField } from "../components/editor-field";
import type { TimetableItem, TimetablePeriod } from "../types/timetable";
import { timetableDefaults, timetableFields } from "./timetable-config";
import { timetableApi } from "./timetable-api";

const weekendDays = new Set(["Saturday", "Sunday"]);

export function TimetableEditor({ item, references, scopeDepartmentId, onClose, onSaved }: { item: TimetableItem | null; references: References; scopeDepartmentId: string; onClose: () => void; onSaved: () => void }) {
  const { settings } = useInstituteSettings();
  const defaults = timetableDefaults(scopeDepartmentId);
  const [values, setValues] = useState<Record<string, string>>(() => item
    ? { ...defaults, ...item.values, period: `${item.values.startsAt}|${item.values.endsAt}` }
    : defaults);
  const [periods, setPeriods] = useState<TimetablePeriod[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const availablePeriods = useMemo(() => periods.filter(period => period.dayGroup === (weekendDays.has(values.dayOfWeek) ? "Weekend" : "Weekday")), [periods, values.dayOfWeek]);

  useEffect(() => {
    timetableApi.getPeriods().then(result => {
      setPeriods(result);
      setValues(current => {
        const group = weekendDays.has(current.dayOfWeek) ? "Weekend" : "Weekday";
        const valid = result.some(period => period.dayGroup === group && `${period.startsAt}|${period.endsAt}` === current.period);
        const first = result.find(period => period.dayGroup === group);
        return valid || !first ? current : { ...current, period: `${first.startsAt}|${first.endsAt}` };
      });
    }).catch(reason => setError(reason instanceof Error ? reason.message : "Could not load teaching periods."));
  }, []);

  function optionsFor(field: Field) {
    if (field.key === "period") return availablePeriods.map(period => ({ id: `${period.startsAt}|${period.endsAt}`, label: `${period.session} · ${period.startsAt}–${period.endsAt}` }));
    if (field.options) return field.options.map(value => ({ id: value, label: value }));
    if (!field.source) return [];
    const source: ManagementItem[] = references[field.source];
    const allowCrossDepartmentTeacher = field.source === "teachers" && settings.departments.allowCrossDepartmentTeaching === "true";
    const allowSharedRoom = field.source === "classrooms" && settings.classrooms.allowSharedRooms === "true";
    const scoped = ["teachers", "students", "classrooms", "courses"].includes(field.source) && values.departmentId && !allowCrossDepartmentTeacher && !allowSharedRoom
      ? source.filter(option => option.values.departmentId === values.departmentId)
      : source;
    return scoped.filter(option => option.values.status !== "Inactive").map(option => ({
      id: option.id,
      label: field.source === "classrooms"
        ? `${option.values.roomType ?? "Classroom"} ${option.values.code}`
        : option.values.name ?? option.values.code ?? option.values.student ?? option.values.course,
    }));
  }

  function change(field: Field, value: string) {
    if (field.key !== "dayOfWeek") { setValues(current => ({ ...current, [field.key]: value })); return; }
    const dayGroup = weekendDays.has(value) ? "Weekend" : "Weekday";
    const first = periods.find(period => period.dayGroup === dayGroup);
    setValues(current => ({ ...current, dayOfWeek: value, period: first ? `${first.startsAt}|${first.endsAt}` : "" }));
  }

  async function save(event: React.FormEvent) {
    event.preventDefault();
    const [startsAt, endsAt] = values.period.split("|");
    if (!startsAt || !endsAt) { setError("Select a teaching period."); return; }
    setSaving(true); setError("");
    const payload: Record<string, string> = { ...values, startsAt, endsAt };
    delete payload.period;
    try { if (item) await timetableApi.update(item.id, payload); else await timetableApi.create(payload); onSaved(); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "Could not save this timetable entry."); setSaving(false); }
  }

  return <div className="modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}><form className="modal management-modal" onSubmit={save}><div className="modal-head"><div><span className="eyebrow">Timetable management</span><h2>{item ? "Edit class" : "Add class"}</h2><p>Choose Year 1–4, an institute teaching period, and any available classroom or meeting room.</p></div><button type="button" className="icon-button" onClick={onClose}><Icon name="close"/></button></div><div className="management-form-grid">{timetableFields.map(field => <EditorField key={field.key} field={field} value={values[field.key] ?? ""} options={optionsFor(field)} onChange={value => change(field, value)}/>)}</div>{error && <p className="form-error">{error}</p>}<div className="timetable-period-note"><strong>Concurrency rules</strong><span>Different rooms may run at the same time. A teacher or learning space cannot be double-booked.</span></div><div className="modal-actions"><button type="button" className="button secondary" onClick={onClose}>Cancel</button><button className="button primary" disabled={saving || !periods.length}>{saving ? "Saving timetable…" : item ? "Save changes" : "Add class"}</button></div></form></div>;
}
