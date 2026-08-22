"use client";

import { useRef, useState } from "react";
import { Icon } from "@/components/icon";
import { useInstituteSettings } from "@/features/administration/institute-settings-context";
import { managementApis } from "../management-apis";
import { managementCopy, managementFields, moduleDefaults } from "../management-config";
import type { Field, ManagementItem, ManagementModule, References } from "../management-types";
import { EditorField } from "./editor-field";

export function ManagementEditor({ module, item, references, scopeDepartmentId, scopeYear, onClose, onSaved }: { module: Exclude<ManagementModule, "overview">; item: ManagementItem | null; references: References; scopeDepartmentId: string; scopeYear: string; onClose: () => void; onSaved: () => void }) {
  const { settings } = useInstituteSettings();
  const defaults = moduleDefaults(module, scopeDepartmentId);
  if (scopeYear && module === "students") defaults.year = scopeYear;
  if (module === "departments") defaults.status = settings.departments.defaultStatus || defaults.status;
  if (module === "courses") defaults.capacity = settings.courses.defaultCapacity || defaults.capacity;
  if (module === "classrooms") { defaults.capacity = settings.classrooms.defaultCapacity || defaults.capacity; defaults.deviceOnline = settings.classrooms.attendanceDeviceRequired === "true" ? "true" : defaults.deviceOnline; }
  if (module === "attendance") defaults.method = settings["attendance-rules"].method || defaults.method;
  if (module === "grades") defaults.term = settings.semester.currentTerm || defaults.term;
  const [values, setValues] = useState<Record<string, string>>(item ? { ...defaults, ...item.values } : defaults);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const saveController = useRef<AbortController | null>(null);
  const fields = managementFields[module].map(field => field.key === "teacherId" && module === "courses"
    ? { ...field, required: settings.courses.requireAssignedTeacher === "true" }
    : field.key === "headTeacherId" && module === "departments"
      ? { ...field, required: settings.departments.requireDepartmentHead === "true" }
      : field.key === "term" && module === "grades"
        ? { ...field, options: Array.from(new Set([settings.semester.currentTerm, ...(field.options ?? [])].filter(Boolean))) }
        : field);
  const canSave = fields.every(field => !field.required || Boolean(values[field.key]?.trim()));

  function optionsFor(field: Field) {
    if (field.options) return field.options.map(value => ({ id: value, label: value }));
    if (!field.source) return [];
    const source: ManagementItem[] = references[field.source];
    const allowCrossDepartmentTeacher = field.source === "teachers" && module === "courses" && settings.departments.allowCrossDepartmentTeaching === "true";
    const scoped = ["teachers", "students", "classrooms", "courses"].includes(field.source) && values.departmentId && !allowCrossDepartmentTeacher
      ? source.filter(option => option.values.departmentId === values.departmentId)
      : source;
    return scoped.filter(option => option.values.status !== "Inactive" && (field.source !== "students" || !scopeYear || option.values.year === scopeYear)).map(option => ({ id: option.id, label: option.values.name ?? option.values.code ?? option.values.student ?? option.values.course, detail: [option.values.number, option.values.department].filter(Boolean).join(" · ") }));
  }

  async function save(event: React.FormEvent) {
    event.preventDefault(); setSaving(true); setError("");
    const controller = new AbortController();
    saveController.current = controller;
    try { if (item) await managementApis[module].update(item.id, values, controller.signal); else await managementApis[module].create(values, controller.signal); onSaved(); }
    catch (reason) { if (!controller.signal.aborted) setError(reason instanceof Error ? reason.message : "Could not save this record."); setSaving(false); }
    finally { if (saveController.current === controller) saveController.current = null; }
  }

  function cancel() {
    saveController.current?.abort();
    onClose();
  }

  return <div className="modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) cancel(); }}><form className="modal management-modal" onSubmit={save}><div className="modal-head"><div><span className="eyebrow">{item ? "Edit current data" : "New current data"}</span><h2>{item ? `Edit ${managementCopy[module].singular}` : `Add ${managementCopy[module].singular}`}</h2><p>Required relationships and Administration rules are validated before saving.</p></div><button type="button" className="icon-button" onClick={cancel}><Icon name="close"/></button></div><div className="management-form-grid">{fields.map(field => <EditorField key={field.key} field={field} value={values[field.key] ?? ""} options={optionsFor(field)} onChange={value => setValues(current => ({ ...current, [field.key]: value }))}/>)}</div>{error && <p className="form-error">{error}</p>}<div className="modal-actions"><button type="button" className="button secondary" onClick={cancel}>{saving ? "Cancel request" : "Cancel"}</button><button className="button primary" disabled={saving || !canSave}>{saving ? "Saving relationships…" : item ? "Save changes" : `Add ${managementCopy[module].singular}`}</button></div></form></div>;
}
