"use client";

import { useState } from "react";
import { Icon } from "@/components/icon";
import { managementApi } from "../management-api";
import { managementCopy, managementFields, moduleDefaults } from "../management-config";
import type { CatalogItem, Field, ManagementModule, References } from "../management-types";
import { EditorField } from "./editor-field";

export function ManagementEditor({ module, item, references, scopeDepartmentId, onClose, onSaved }: { module: Exclude<ManagementModule, "overview">; item: CatalogItem | null; references: References; scopeDepartmentId: string; onClose: () => void; onSaved: () => void }) {
  const defaults = moduleDefaults(module, scopeDepartmentId);
  const [values, setValues] = useState<Record<string, string>>(item ? { ...defaults, ...item.values } : defaults);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  function optionsFor(field: Field) {
    if (field.options) return field.options.map(value => ({ id: value, label: value }));
    if (!field.source) return [];
    let source = references[field.source];
    if (["teachers", "students", "classrooms", "courses"].includes(field.source) && values.departmentId) source = source.filter(option => option.values.departmentId === values.departmentId);
    return source.filter(option => option.values.status !== "Inactive").map(option => ({ id: option.id, label: option.values.name ?? option.values.code ?? option.values.student ?? option.values.course }));
  }

  async function save(event: React.FormEvent) {
    event.preventDefault(); setSaving(true); setError("");
    try { if (item) await managementApi.update(module, item.id, values); else await managementApi.create(module, values); onSaved(); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "Could not save this record."); setSaving(false); }
  }

  return <div className="modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}><form className="modal management-modal" onSubmit={save}><div className="modal-head"><div><span className="eyebrow">{item ? "Edit current data" : "New current data"}</span><h2>{item ? `Edit ${managementCopy[module].singular}` : `Add ${managementCopy[module].singular}`}</h2><p>Required relationships are validated before saving.</p></div><button type="button" className="icon-button" onClick={onClose}><Icon name="close"/></button></div><div className="management-form-grid">{managementFields[module].map(field => <EditorField key={field.key} field={field} value={values[field.key] ?? ""} options={optionsFor(field)} onChange={value => setValues(current => ({ ...current, [field.key]: value }))}/>)}</div>{error && <p className="form-error">{error}</p>}<div className="modal-actions"><button type="button" className="button secondary" onClick={onClose}>Cancel</button><button className="button primary" disabled={saving}>{saving ? "Saving relationships…" : item ? "Save changes" : `Add ${managementCopy[module].singular}`}</button></div></form></div>;
}
