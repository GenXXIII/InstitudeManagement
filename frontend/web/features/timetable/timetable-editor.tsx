"use client";

import { useEffect, useMemo, useState } from "react";
import { CodeRecommendation } from "@/components/code-recommendation";
import { Icon } from "@/components/icon";
import { EditorField } from "@/features/management/components/editor-field";
import type { Field } from "@/features/management/management-types";
import { validateManagementFields, validationMessages, type FieldErrors } from "@/features/management/management-validation";
import { formatAssignedCode, workflowCodeExample } from "@/lib/workflow-code";
import { recommendedCodeFromError } from "@/lib/code-recommendation";
import type { TimetableItem, TimetablePeriod } from "./timetable-types";
import { timetableDefaults, timetableFields } from "./timetable-config";
import { timetableApi } from "./timetable-api";

const weekendDays = new Set(["Saturday", "Sunday"]);

export function TimetableEditor({ item, scopeDepartmentId, onClose, onSaved }: {
  item: TimetableItem | null;
  scopeDepartmentId: string;
  onClose: () => void;
  onSaved: () => void;
}) {
  const defaults = timetableDefaults(scopeDepartmentId);
  const [values, setValues] = useState<Record<string, string>>(() => item
    ? { ...defaults, ...item.values, period: `${item.values.startsAt}|${item.values.endsAt}` }
    : defaults);
  const [periods, setPeriods] = useState<TimetablePeriod[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const availablePeriods = useMemo(() => {
    const weekend = weekendDays.has(values.dayOfWeek);
    return periods.filter(period => period.dayGroup === (weekend ? "Weekend" : "Weekday") && (weekend || period.session === values.shift));
  }, [periods, values.dayOfWeek, values.shift]);

  useEffect(() => {
    timetableApi.getPeriods().then(result => {
      setPeriods(result);
      setValues(current => {
        const group = weekendDays.has(current.dayOfWeek) ? "Weekend" : "Weekday";
        const shift = group === "Weekend" ? "Weekend" : current.shift === "Weekend" ? "Morning" : current.shift;
        const matchingPeriods = result.filter(period => period.dayGroup === group && (group === "Weekend" || period.session === shift));
        const valid = matchingPeriods.some(period => `${period.startsAt}|${period.endsAt}` === current.period);
        const first = matchingPeriods[0];
        return valid || !first ? { ...current, shift } : { ...current, shift, period: `${first.startsAt}|${first.endsAt}` };
      });
    }).catch(reason => setError(reason instanceof Error ? reason.message : "Could not load teaching periods."));
  }, []);

  function optionsFor(field: Field) {
    if (field.key === "period") return availablePeriods.map(period => ({ id: `${period.startsAt}|${period.endsAt}`, label: `${period.session} - ${period.startsAt}-${period.endsAt}` }));
    if (field.key === "shift") {
      const shifts = weekendDays.has(values.dayOfWeek) ? ["Weekend"] : ["Morning", "Afternoon", "Evening"];
      return shifts.map(value => ({ id: value, label: value }));
    }
    return field.options?.map(value => ({ id: value, label: value })) ?? [];
  }

  function change(field: Field, value: string) {
    setFieldErrors(current => { const next = { ...current }; delete next[field.key]; return next; });
    setError("");
    if (field.key !== "dayOfWeek" && field.key !== "shift") {
      setValues(current => ({ ...current, [field.key]: value }));
      return;
    }
    setValues(current => {
      const dayOfWeek = field.key === "dayOfWeek" ? value : current.dayOfWeek;
      const weekend = weekendDays.has(dayOfWeek);
      const shift = weekend ? "Weekend" : field.key === "shift" ? value : current.shift === "Weekend" ? "Morning" : current.shift;
      const first = periods.find(period => period.dayGroup === (weekend ? "Weekend" : "Weekday") && (weekend || period.session === shift));
      return { ...current, dayOfWeek, shift, period: first ? `${first.startsAt}|${first.endsAt}` : "" };
    });
  }

  async function save(event: React.FormEvent) {
    event.preventDefault();
    const submittedValues: Record<string, string> = {
      ...values,
      ...(values.timetableCode?.trim() ? { timetableCode: formatAssignedCode(values.timetableCode, "timetable", "management") } : {}),
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
      if (item) await timetableApi.update(item.id, payload);
      else await timetableApi.create(payload);
      onSaved();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Could not save this schedule slot.");
      setSaving(false);
    }
  }

  function formatTimetableCode() {
    if (!values.timetableCode?.trim() || item) return;
    change(timetableFields[0], formatAssignedCode(values.timetableCode, "timetable", "management"));
  }

  const recommendation = recommendedCodeFromError(error);
  const problems = validationMessages(fieldErrors, recommendation ? "" : error);
  return <div className="modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}><form noValidate className="modal management-modal" onSubmit={save}><div className="modal-head"><div><span className="eyebrow">Schedule management</span><h2>{item ? "Edit schedule" : "Add schedule"}</h2><p>{item ? "Edit this permanent shift and day/time slot. Its permanent code cannot change." : "Create a permanent schedule code, shift, and reusable day/time slot. Course, teacher, classroom, and student year are added later in Timetable Enrollment."}</p></div><button type="button" className="icon-button" onClick={onClose}><Icon name="close" /></button></div><div className="management-form-grid">{timetableFields.map(field => <EditorField key={field.key} field={field.key === "timetableCode" && item ? { ...field, readOnly: true } : field} value={values[field.key] ?? ""} options={optionsFor(field)} error={fieldErrors[field.key]} hint={field.key === "timetableCode" ? item ? "Permanent code" : `Final code: ${values.timetableCode?.trim() ? formatAssignedCode(values.timetableCode, "timetable", "management") : workflowCodeExample("timetable", "management")}` : undefined} onChange={value => change(field, value)} onBlur={field.key === "timetableCode" ? formatTimetableCode : undefined} />)}</div>{recommendation && !item && <CodeRecommendation code={recommendation} onUse={() => change(timetableFields[0], recommendation)}/>} {problems.length > 0 && <div className="form-error validation-summary" role="alert"><strong>Fix these problems:</strong><ul>{problems.map(problem => <li key={problem}>{problem}</li>)}</ul></div>}<div className="timetable-period-note"><strong>Permanent schedule slot</strong><span>This Management record stores the code, shift, day, and time. Semester relationships are maintained in Enrollment.</span></div><div className="modal-actions"><button type="button" className="button secondary" onClick={onClose}>Cancel</button><button className="button primary" disabled={saving || !periods.length}>{saving ? "Saving schedule..." : item ? "Save changes" : "Add schedule"}</button></div></form></div>;
}
