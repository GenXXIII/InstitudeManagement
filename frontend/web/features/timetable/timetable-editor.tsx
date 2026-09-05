"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/icon";
import { useInstituteSettings } from "@/features/administration/institute-settings-context";
import { EditorField } from "@/features/management/components/editor-field";
import { relationshipCode } from "@/features/management/management-id";
import type { Field, ManagementItem, References } from "@/features/management/management-types";
import { validateManagementFields, validationMessages, type FieldErrors } from "@/features/management/management-validation";
import { relationshipCreateTarget } from "@/features/management/relationship-create";
import type { TimetableItem, TimetablePeriod } from "./timetable-types";
import { timetableDefaults, timetableFields } from "./timetable-config";
import { timetableApi } from "./timetable-api";
import { formatAssignedCode, workflowCodeExample } from "@/lib/workflow-code";

const weekendDays = new Set(["Saturday", "Sunday"]);

export function TimetableEditor({ item, references, scopeDepartmentId, scopeYear, saveItem, onClose, onSaved }: { item: TimetableItem | null; references: References; scopeDepartmentId: string; scopeYear: string; saveItem?: (id: string, values: Record<string, string>) => Promise<unknown>; onClose: () => void; onSaved: () => void }) {
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
    return scoped.filter(option => option.values.status !== "Inactive" && (field.source !== "classrooms" || ((option.values.status === "Available" || (saveItem && option.id === values.classroomId)) && classroomMatchesYear(option.values.classroomCode, values.yearLevel)))).map(option => ({
      id: option.id,
      label: `${relationshipCode(field.source!, option.values)} - ${option.values.name ?? option.values.building ?? option.values.student ?? option.values.course}`,
      detail: [option.values.roomType, option.values.department].filter(Boolean).join(" - "),
    }));
  }

  function change(field: Field, value: string) {
    setFieldErrors(current => { const next = { ...current }; delete next[field.key]; return next; });
    setError("");
    if (field.key !== "dayOfWeek") {
      setValues(current => {
        const selectedRoom = field.key === "yearLevel" ? references.classrooms.find(room => room.id === current.classroomId) : undefined;
        const clearRoom = selectedRoom && !classroomMatchesYear(selectedRoom.values.classroomCode, value);
        const nextRoom = field.key === "classroomId" ? references.classrooms.find(room => room.id === value) : undefined;
        return { ...current, [field.key]: value, ...(nextRoom && saveItem ? { classroomStatus: nextRoom.values.status } : {}), ...(clearRoom ? { classroomId: "" } : {}) };
      });
      return;
    }
    const dayGroup = weekendDays.has(value) ? "Weekend" : "Weekday";
    const first = periods.find(period => period.dayGroup === dayGroup);
    setValues(current => ({ ...current, dayOfWeek: value, period: first ? `${first.startsAt}|${first.endsAt}` : "" }));
  }

  async function save(event: React.FormEvent) {
    event.preventDefault();
    const submittedValues: Record<string, string> = {
      ...values,
      timetableCode: formatAssignedCode(
        values.timetableCode,
        "timetable",
        "management"
      ),
    };
    const optionSets = Object.fromEntries(timetableFields.filter(field => field.type === "select").map(field => [field.key, new Set(optionsFor(field).map(option => option.id))]));
    const nextErrors = validateManagementFields(timetableFields, submittedValues, optionSets);
    setFieldErrors(nextErrors);
    setError("");
    if (Object.keys(nextErrors).length) return;

    const [startsAt, endsAt] = submittedValues.period.split("|");
    setSaving(true);
    setValues(submittedValues);
    const payload: Record<string, string> = { ...submittedValues, startsAt, endsAt };
    delete payload.period;
    try {
      if (item && saveItem) await saveItem(item.id, payload);
      else if (item) await timetableApi.update(item.id, payload);
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
  return <div className="modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}><form noValidate className="modal management-modal" onSubmit={save}><div className="modal-head"><div><span className="eyebrow">{saveItem ? "Timetable enrollment" : "Schedule management"}</span><h2>{item ? "Edit schedule" : "Add schedule"}</h2><p>{saveItem ? "Edit the enrolled schedule and set whether its assigned classroom is Available or under Maintenance." : "Enter your own unique schedule code, then choose Year 1-4, a teaching period, and an available classroom or meeting room."}</p></div><button type="button" className="icon-button" onClick={onClose}><Icon name="close" /></button></div><div className="management-form-grid">{timetableFields.map(field => <EditorField key={field.key} field={field} value={values[field.key] ?? ""} options={optionsFor(field)} createOption={createOptionFor(field)} error={fieldErrors[field.key]} hint={field.key === "timetableCode" ? `Final code: ${values.timetableCode?.trim() ? formatAssignedCode(values.timetableCode, "timetable", "management") : workflowCodeExample("timetable", "management")}` : undefined} onChange={value => change(field, value)} />)}{saveItem && <label className="editor-field"><span>Status</span><select value={values.classroomStatus ?? "Available"} onChange={event => setValues(current => ({ ...current, classroomStatus: event.target.value }))}><option value="Available">Available</option><option value="Maintenance">Maintenance</option></select></label>}</div>{problems.length > 0 && <div className="form-error validation-summary" role="alert"><strong>Fix these problems:</strong><ul>{problems.map(problem => <li key={problem}>{problem}</li>)}</ul></div>}<div className="timetable-period-note"><strong>Room and concurrency rules</strong><span>Year 1 uses Classroom 501 only. Years 2-4 use the other classrooms. A teacher or learning space cannot be double-booked. Only Available classrooms can run.</span></div><div className="modal-actions"><button type="button" className="button secondary" onClick={onClose}>Cancel</button><button className="button primary" disabled={saving || !periods.length}>{saving ? "Saving schedule..." : item ? "Save changes" : "Add schedule"}</button></div></form></div>;
}

function classroomMatchesYear(classroomCode: string | undefined, yearLevel: string) {
  return yearLevel === "1" ? classroomCode === "501" : classroomCode !== "501";
}
