"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/icon";
import { useInstituteSettings } from "@/features/administration/institute-settings-context";
import type { Field, ManagementItem, References } from "../management-types";
import { validateManagementFields, validationMessages, type FieldErrors } from "../management-validation";
import { relationshipCode } from "../management-id";
import { relationshipCreateTarget } from "../relationship-create";
import { EditorField } from "../components/editor-field";
import type { TimetableItem, TimetablePeriod } from "../types/timetable";
import { timetableDefaults, timetableFields } from "./timetable-config";
import { timetableApi } from "./timetable-api";

const weekendDays = new Set(["Saturday", "Sunday"]);

export function TimetableEditor({ item, references, scopeDepartmentId, scopeYear, onClose, onSaved }: { item: TimetableItem | null; references: References; scopeDepartmentId: string; scopeYear: string; onClose: () => void; onSaved: () => void }) {
  const router = useRouter();
  const { settings } = useInstituteSettings();
  const defaults = timetableDefaults(scopeDepartmentId);
  if (scopeYear) defaults.yearLevel = scopeYear;
  const [values, setValues] = useState<Record<string, string>>(() => item
    ? { ...defaults, ...item.values, period: `${item.values.startsAt}|${item.values.endsAt}` }
    : defaults);
  const [periods, setPeriods] = useState<TimetablePeriod[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

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
    if (field.key === "period") return availablePeriods.map(period => ({ id: `${period.startsAt}|${period.endsAt}`, label: `${period.session} - ${period.startsAt}-${period.endsAt}` }));
    if (field.options) return field.options.map(value => ({ id: value, label: value }));
    if (!field.source) return [];
    const source: ManagementItem[] = references[field.source];
    const allowCrossDepartmentTeacher = field.source === "teachers" && settings.departments.allowCrossDepartmentTeaching === "true";
    const sharedClassroom = field.source === "classrooms";
    const scoped = ["teachers", "students", "classrooms", "courses"].includes(field.source) && values.departmentId && !allowCrossDepartmentTeacher && !sharedClassroom
      ? source.filter(option => option.values.departmentId === values.departmentId || (field.source === "teachers" && !option.values.departmentId))
      : source;
    return scoped.filter(option => option.values.status !== "Inactive" && (field.source !== "classrooms" || values.yearLevel === "1" || option.values.classroomCode !== "501")).map(option => ({
      id: option.id,
      label: `${relationshipCode(field.source!, option.values)} - ${option.values.name ?? option.values.building ?? option.values.student ?? option.values.course}`,
      detail: [option.values.roomType, option.values.department].filter(Boolean).join(" - "),
    }));
  }

  function change(field: Field, value: string) {
    setFieldErrors(current => { const next = { ...current }; delete next[field.key]; return next; });
    setError("");
    if (field.key !== "dayOfWeek") {
      setValues(current => ({ ...current, [field.key]: value, ...(field.key === "yearLevel" && value !== "1" && references.classrooms.find(room => room.id === current.classroomId)?.values.classroomCode === "501" ? { classroomId: "" } : {}) }));
      return;
    }
    const dayGroup = weekendDays.has(value) ? "Weekend" : "Weekday";
    const first = periods.find(period => period.dayGroup === dayGroup);
    setValues(current => ({ ...current, dayOfWeek: value, period: first ? `${first.startsAt}|${first.endsAt}` : "" }));
  }

  async function save(event: React.FormEvent) {
    event.preventDefault();
    const optionSets = Object.fromEntries(timetableFields.filter(field => field.type === "select").map(field => [field.key, new Set(optionsFor(field).map(option => option.id))]));
    const nextErrors = validateManagementFields(timetableFields, values, optionSets);
    setFieldErrors(nextErrors);
    setError("");
    if (Object.keys(nextErrors).length) return;

    const [startsAt, endsAt] = values.period.split("|");
    setSaving(true);
    const payload: Record<string, string> = { ...values, startsAt, endsAt };
    delete payload.period;
    try {
      if (item) await timetableApi.update(item.id, payload);
      else await timetableApi.create(payload);
      onSaved();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Could not save this timetable entry.");
      setSaving(false);
    }
  }

  function createOptionFor(field: Field) {
    const target = relationshipCreateTarget(field.source);
    if (!target) return undefined;
    return { id: `create-${field.source}`, label: target.label, action: () => { onClose(); router.push(target.path); } };
  }

  const problems = validationMessages(fieldErrors, error);
  return <div className="modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}><form noValidate className="modal management-modal" onSubmit={save}><div className="modal-head"><div><span className="eyebrow">Timetable management</span><h2>{item ? "Edit class" : "Add class"}</h2><p>Choose Year 1-4, an institute teaching period, and any available classroom or meeting room.</p></div><button type="button" className="icon-button" onClick={onClose}><Icon name="close"/></button></div><div className="management-form-grid">{timetableFields.map(field => <EditorField key={field.key} field={field} value={values[field.key] ?? ""} options={optionsFor(field)} createOption={createOptionFor(field)} error={fieldErrors[field.key]} onChange={value => change(field, value)}/>)}</div>{problems.length > 0 && <div className="form-error validation-summary" role="alert"><strong>Fix these problems:</strong><ul>{problems.map(problem => <li key={problem}>{problem}</li>)}</ul></div>}<div className="timetable-period-note"><strong>Room and concurrency rules</strong><span>Room 501 is Year 1 only. A teacher or learning space cannot be double-booked.</span></div><div className="modal-actions"><button type="button" className="button secondary" onClick={onClose}>Cancel</button><button className="button primary" disabled={saving || !periods.length}>{saving ? "Saving timetable..." : item ? "Save changes" : "Add class"}</button></div></form></div>;
}
