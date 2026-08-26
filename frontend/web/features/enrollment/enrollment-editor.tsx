"use client";

import { useState } from "react";
import { Icon } from "@/components/icon";
import { SearchableSelect, type SearchableOption } from "@/components/searchable-select";
import type { DepartmentItem } from "@/features/management/types/department";
import { enrollmentApi, type EnrollmentItem, type EnrollmentResource } from "./enrollment-api";

type Option = { id: string; label: string };
type Field = { key: string; label: string; type?: "select" | "number" | "text" | "time"; options?: Option[]; required?: boolean };
type AssignableEnrollmentResource = "students" | "teachers" | "courses" | "classrooms";

export function EnrollmentEditor({ resource, item, candidates, departments, teachers, courses, classrooms, scopeDepartmentId, scopeYear, onClose, onSaved }: {
  resource: EnrollmentResource;
  item: EnrollmentItem | null;
  candidates: EnrollmentItem[];
  departments: DepartmentItem[];
  teachers: EnrollmentItem[];
  courses: EnrollmentItem[];
  classrooms: EnrollmentItem[];
  scopeDepartmentId: string;
  scopeYear: string;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [resourceId, setResourceId] = useState(item?.id ?? "");
  const [values, setValues] = useState<Record<string, string>>(() => item ? { ...item.values } : enrollmentDefaults(resource, scopeDepartmentId, scopeYear));
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
  const creating = item === null;
  const candidateOptions = creating && isAssignableEnrollment(resource) ? candidates.map(candidateOption) : [];

  async function save(event: React.FormEvent) {
    event.preventDefault();
    setError("");
    if (creating && !resourceId) { setError(`${candidateName(resource)} is required.`); return; }
    const missing = fields.find(field => field.required && !values[field.key]?.trim());
    if (missing) { setError(`${missing.label} is required.`); return; }
    setSaving(true);
    try {
      await enrollmentApi.update(resource, item?.id ?? resourceId, values);
      onSaved();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Could not save this enrollment assignment.");
      setSaving(false);
    }
  }

  function selectCandidate(id: string) {
    setResourceId(id);
    setError("");
    const candidate = candidates.find(option => option.id === id);
    if (resource === "classrooms" && candidate?.values.capacity) {
      setValues(current => ({ ...current, capacity: candidate.values.capacity }));
    }
  }

  const subject = resource === "timetable" ? "timetable" : resource.slice(0, -1);
  return <div className="modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}><form className="modal management-modal" onSubmit={save} noValidate>
    <div className="modal-head"><div><span className="eyebrow">Academic enrollment service</span><h2>{creating ? "Add enrollment" : `Edit ${subject} assignment`}</h2><p>{creating ? `Select an existing ${candidateName(resource).toLowerCase()} from Academic Management, then define its current academic assignment.` : "This changes enrollment data only. Personal and master details remain in Academic Management."}</p></div><button type="button" className="icon-button" onClick={onClose}><Icon name="close"/></button></div>
    <div className="management-form-grid">
      {creating && <div className="editor-field relationship-editor-field enrollment-candidate-field"><span>{candidateName(resource)}</span><SearchableSelect value={resourceId} options={candidateOptions} placeholder={`Type to find ${candidateName(resource).toLowerCase()}...`} ariaLabel={candidateName(resource)} ariaInvalid={Boolean(error && !resourceId)} required onChange={selectCandidate}/>{candidates.length === 0 && <small className="enrollment-candidate-note">No unassigned {candidateName(resource).toLowerCase()} records are available. Add one in Academic Management first.</small>}</div>}
      {fields.map(field => <label className="editor-field" key={field.key}><span>{field.label}</span>{field.type === "select" ? <select value={values[field.key] ?? ""} onChange={event => setValues(current => ({ ...current, [field.key]: event.target.value }))}>{!field.options?.some(option => option.id === "") && <option value="">Select {field.label.toLowerCase()}</option>}{field.options?.map(option => <option value={option.id} key={option.id || "none"}>{option.label}</option>)}</select> : <input type={field.type === "time" ? "time" : field.type === "number" ? "number" : "text"} min={field.type === "number" ? "1" : undefined} value={values[field.key] ?? ""} onChange={event => setValues(current => ({ ...current, [field.key]: event.target.value }))}/>}</label>)}
    </div>
    {error && <div className="form-error" role="alert">{error}</div>}
    <div className="modal-actions"><button type="button" className="button secondary" onClick={onClose}>Cancel</button><button className="button primary" disabled={saving || (creating && candidates.length === 0)}>{saving ? "Saving assignment..." : creating ? "Add enrollment" : "Save enrollment"}</button></div>
  </form></div>;
}

function isAssignableEnrollment(resource: EnrollmentResource): resource is AssignableEnrollmentResource {
  return resource === "students" || resource === "teachers" || resource === "courses" || resource === "classrooms";
}

function enrollmentDefaults(resource: EnrollmentResource, departmentId: string, year: string): Record<string, string> {
  if (resource === "students") return { departmentId, year: year || "1", shift: "Morning", status: "Active" };
  if (resource === "teachers") return { departmentId, status: departmentId ? "Assigned" : "Unassigned" };
  if (resource === "courses") return { departmentId, teacherId: "", year: year || "1", capacity: "", status: "Active" };
  if (resource === "classrooms") return { departmentId, access: departmentId ? "Department only" : "Shared institute", capacity: "", status: "Available" };
  return {};
}

function candidateName(resource: EnrollmentResource) {
  if (resource === "students") return "Student profile";
  if (resource === "teachers") return "Teacher profile";
  if (resource === "courses") return "Course record";
  if (resource === "classrooms") return "Learning space";
  return "Enrollment record";
}

function candidateOption(item: EnrollmentItem): SearchableOption {
  const values = item.values;
  const code = values.studentCode || values.teacherCode || values.courseCode || values.classroomCode;
  const name = values.name || [values.building, values.roomType].filter(Boolean).join(" - ");
  return { id: item.id, label: [code, name].filter(Boolean).join(" - "), detail: values.email || undefined };
}
