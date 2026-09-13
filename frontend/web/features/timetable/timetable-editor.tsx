"use client";

import { useState } from "react";
import { CodeRecommendation } from "@/components/code-recommendation";
import { Icon } from "@/components/icon";
import { EditorField } from "@/features/management/components/editor-field";
import type { Field } from "@/features/management/management-types";
import { validateManagementFields, validationMessages, type FieldErrors } from "@/features/management/management-validation";
import { formatAssignedCode, workflowCodeExample } from "@/lib/workflow-code";
import { recommendedCodeFromError } from "@/lib/code-recommendation";
import type { TimetableItem } from "./timetable-types";
import { timetableDefaults, timetableFields } from "./timetable-config";
import { timetableApi } from "./timetable-api";
import { ScheduleSelectField } from "./schedule-select-field";
import { ClockTimeField } from "./clock-time-field";

export function TimetableEditor({ item, scopeDepartmentId, onClose, onSaved }: {
  item: TimetableItem | null;
  scopeDepartmentId: string;
  onClose: () => void;
  onSaved: () => void;
}) {
  const defaults = timetableDefaults(scopeDepartmentId);
  const [values, setValues] = useState<Record<string, string>>(() => item ? { ...defaults, ...item.values } : defaults);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  function optionsFor(field: Field) {
    return field.options?.map(value => ({ id: value, label: value })) ?? [];
  }

  function change(field: Field, value: string) {
    setFieldErrors(current => { const next = { ...current }; delete next[field.key]; return next; });
    setError("");
    setValues(current => ({ ...current, [field.key]: value }));
  }

  async function save(event: React.FormEvent) {
    event.preventDefault();
    const submittedValues: Record<string, string> = {
      ...values,
      ...(values.timetableCode?.trim() ? { timetableCode: formatAssignedCode(values.timetableCode, "timetable", "management") } : {}),
    };
    const optionSets = Object.fromEntries(timetableFields.filter(field => field.type === "select").map(field => [field.key, new Set(optionsFor(field).map(option => option.id))]));
    const nextErrors = validateManagementFields(timetableFields, submittedValues, optionSets);
    if (submittedValues.startsAt && submittedValues.endsAt && submittedValues.endsAt <= submittedValues.startsAt)
      nextErrors.endsAt = "Ending time must be after starting time.";
    setFieldErrors(nextErrors);
    setError("");
    if (Object.keys(nextErrors).length) return;

    setSaving(true);
    setValues(submittedValues);
    const payload: Record<string, string> = { ...submittedValues };
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
  return <div className="modal-backdrop schedule-modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}><form noValidate className="modal management-modal schedule-management-modal" onSubmit={save}><div className="modal-head"><div><span className="eyebrow">Schedule management</span><h2>{item ? "Edit schedule" : "Add schedule"}</h2><p>{item ? "Edit this permanent shift and day/time slot. Its permanent code cannot change." : "Create a permanent schedule code, shift, and reusable day/time slot. Course, teacher, classroom, and student year are added later in Timetable Enrollment."}</p></div><button type="button" className="icon-button" onClick={onClose}><Icon name="close" /></button></div><div className="management-form-grid">{timetableFields.map(field => {
    const fieldValue = values[field.key] ?? "";
    if (field.type === "select") return <ScheduleSelectField key={field.key} label={field.label} value={fieldValue} options={optionsFor(field)} required={field.required} error={fieldErrors[field.key]} onChange={value => change(field, value)}/>;
    if (field.type === "time") return <ClockTimeField key={field.key} label={field.label} value={fieldValue} required={field.required} error={fieldErrors[field.key]} onChange={value => change(field, value)}/>;
    return <EditorField key={field.key} field={field.key === "timetableCode" && item ? { ...field, readOnly: true } : field} value={fieldValue} options={optionsFor(field)} error={fieldErrors[field.key]} hint={field.key === "timetableCode" ? item ? "Permanent code" : `Final code: ${values.timetableCode?.trim() ? formatAssignedCode(values.timetableCode, "timetable", "management") : workflowCodeExample("timetable", "management")}` : undefined} onChange={value => change(field, value)} onBlur={field.key === "timetableCode" ? formatTimetableCode : undefined} />;
  })}</div>{recommendation && !item && <CodeRecommendation code={recommendation} onUse={() => change(timetableFields[0], recommendation)}/>} {problems.length > 0 && <div className="form-error validation-summary" role="alert"><strong>Fix these problems:</strong><ul>{problems.map(problem => <li key={problem}>{problem}</li>)}</ul></div>}<div className="timetable-period-note"><strong>Free schedule time</strong><span>Choose the shift, then use the visual clock to set any starting and ending time. Times are not limited by configured teaching periods.</span></div><div className="modal-actions"><button type="button" className="button secondary" onClick={onClose}>Cancel</button><button className="button primary" disabled={saving}>{saving ? "Saving schedule..." : item ? "Save changes" : "Add schedule"}</button></div></form></div>;
}
