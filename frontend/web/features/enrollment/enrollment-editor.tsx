"use client";

import { useState } from "react";
import { Icon } from "@/components/icon";
import type { DepartmentItem } from "@/features/management/types/department";
import { enrollmentApi, type EnrollmentItem, type EnrollmentResource } from "./enrollment-api";

type Option = { id: string; label: string };
type Field = { key: string; label: string; type?: "select" | "number" | "text" | "time"; options?: Option[]; required?: boolean };

export function EnrollmentEditor({ resource, item, departments, teachers, courses, classrooms, onClose, onSaved }: { resource: EnrollmentResource; item: EnrollmentItem; departments: DepartmentItem[]; teachers: EnrollmentItem[]; courses: EnrollmentItem[]; classrooms: EnrollmentItem[]; onClose: () => void; onSaved: () => void }) {
  const [values, setValues] = useState({ ...item.values });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const departmentOptions = departments.map(department => ({ id: department.id, label: department.values.name }));
  const fields: Field[] = resource === "students" ? [
    { key: "departmentId", label: "Department selected for Year 2-4", type: "select", options: departmentOptions, required: true },
    { key: "year", label: "Year level", type: "select", options: ["1", "2", "3", "4"].map(id => ({ id, label: `Year ${id}` })), required: true },
    { key: "shift", label: "Learning shift", type: "select", options: ["Morning", "Afternoon", "Evening", "Weekend"].map(id => ({ id, label: id })), required: true },
    { key: "status", label: "Enrollment status", type: "select", options: ["Active", "Paused", "Completed"].map(id => ({ id, label: id })), required: true },
  ] : resource === "teachers" ? [
    { key: "departmentId", label: "Assigned department", type: "select", options: [{ id: "", label: "Unassigned" }, ...departmentOptions] },
    { key: "status", label: "Assignment status", type: "select", options: ["Assigned", "On leave", "Unassigned"].map(id => ({ id, label: id })), required: true },
  ] : resource === "courses" ? [
    { key: "departmentId", label: "Department", type: "select", options: departmentOptions, required: true },
    { key: "teacherId", label: "Assigned teacher", type: "select", options: teachers.filter(teacher => teacher.values.status === "Assigned").map(teacher => ({ id: teacher.id, label: `${teacher.values.teacherCode} - ${teacher.values.name}` })), required: true },
    { key: "year", label: "Year level", type: "select", options: ["1", "2", "3", "4"].map(id => ({ id, label: `Year ${id}` })), required: true },
    { key: "capacity", label: "Student capacity", type: "number", required: true },
    { key: "status", label: "Assignment status", type: "select", options: ["Active", "Paused"].map(id => ({ id, label: id })), required: true },
  ] : resource === "classrooms" ? [
    { key: "departmentId", label: "Department access", type: "select", options: [{ id: "", label: "Whole institute" }, ...departmentOptions] },
    { key: "access", label: "Access", type: "select", options: ["Shared institute", "Department only"].map(id => ({ id, label: id })), required: true },
    { key: "capacity", label: "Assigned seat capacity", type: "number", required: true },
    { key: "status", label: "Assignment status", type: "select", options: ["Available", "Reserved", "Unavailable"].map(id => ({ id, label: id })), required: true },
  ] : [
    { key: "timetableCode", label: "TimetableCode", type: "text", required: true },
    { key: "courseId", label: "Assigned course", type: "select", options: courses.map(course => ({ id: course.id, label: `${course.values.courseCode} - ${course.values.name}` })), required: true },
    { key: "teacherId", label: "Assigned teacher", type: "select", options: teachers.map(teacher => ({ id: teacher.id, label: `${teacher.values.teacherCode} - ${teacher.values.name}` })), required: true },
    { key: "classroomId", label: "Assigned classroom", type: "select", options: classrooms.map(room => ({ id: room.id, label: `${room.values.classroomCode} - ${room.values.building}` })), required: true },
    { key: "yearLevel", label: "Year level", type: "select", options: ["1", "2", "3", "4"].map(id => ({ id, label: `Year ${id}` })), required: true },
    { key: "dayOfWeek", label: "Day", type: "select", options: ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"].map(id => ({ id, label: id })), required: true },
    { key: "startsAt", label: "Starts at", type: "time", required: true },
    { key: "endsAt", label: "Ends at", type: "time", required: true },
    { key: "status", label: "Assignment status", type: "select", options: ["Upcoming", "Running", "Completed", "Cancelled"].map(id => ({ id, label: id })), required: true },
  ];

  async function save(event: React.FormEvent) {
    event.preventDefault(); setError("");
    const missing = fields.find(field => field.required && !values[field.key]?.trim());
    if (missing) { setError(`${missing.label} is required.`); return; }
    setSaving(true);
    try { await enrollmentApi.update(resource, item.id, values); onSaved(); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "Could not save this enrollment assignment."); setSaving(false); }
  }

  return <div className="modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}><form className="modal management-modal" onSubmit={save} noValidate>
    <div className="modal-head"><div><span className="eyebrow">Academic enrollment service</span><h2>Edit {resource === "timetable" ? "timetable" : resource.slice(0, -1)} assignment</h2><p>This changes enrollment data only. Personal and master details remain in Academic Management.</p></div><button type="button" className="icon-button" onClick={onClose}><Icon name="close"/></button></div>
    <div className="management-form-grid">{fields.map(field => <label className="editor-field" key={field.key}><span>{field.label}</span>{field.type === "select" ? <select value={values[field.key] ?? ""} onChange={event => setValues(current => ({ ...current, [field.key]: event.target.value }))}>{field.options?.map(option => <option value={option.id} key={option.id || "none"}>{option.label}</option>)}</select> : <input type={field.type === "time" ? "time" : field.type === "number" ? "number" : "text"} min={field.type === "number" ? "1" : undefined} value={values[field.key] ?? ""} onChange={event => setValues(current => ({ ...current, [field.key]: event.target.value }))}/>}</label>)}</div>
    {error && <div className="form-error" role="alert">{error}</div>}
    <div className="modal-actions"><button type="button" className="button secondary" onClick={onClose}>Cancel</button><button className="button primary" disabled={saving}>{saving ? "Saving assignment..." : "Save enrollment"}</button></div>
  </form></div>;
}
