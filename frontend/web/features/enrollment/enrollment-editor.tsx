"use client";

import { useState } from "react";
import { Icon } from "@/components/icon";
import { SearchableSelect, type SearchableOption } from "@/components/searchable-select";
import type { DepartmentItem } from "@/features/management/types/department";
import { enrollmentApi, type EnrollmentItem, type EnrollmentResource } from "./enrollment-api";

type Option = { id: string; label: string };
type Field = { key: string; label: string; type?: "select" | "number" | "text" | "time"; options?: Option[]; required?: boolean };
type SelectableEnrollmentResource = "students" | "timetable";

export function EnrollmentEditor({ resource, item, candidates, departments, teachers, scopeDepartmentId, scopeYear, onClose, onSaved }: {
  resource: EnrollmentResource;
  item: EnrollmentItem | null;
  candidates: EnrollmentItem[];
  departments: DepartmentItem[];
  teachers: EnrollmentItem[];
  scopeDepartmentId: string;
  scopeYear: string;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [resourceId, setResourceId] = useState(item?.id ?? "");
  const [values, setValues] = useState<Record<string, string>>(() => item ? { ...item.values } : enrollmentDefaults(resource, scopeDepartmentId, scopeYear));
  const [selectedCourseId, setSelectedCourseId] = useState(item?.values.courseId ?? "");
  const [selectedTeacherId, setSelectedTeacherId] = useState(item?.values.teacherId ?? "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const creating = item === null;
  const departmentOptions = departments.map(department => ({ id: department.id, label: department.values.name }));
  const fields: Field[] = resource === "students" ? [
    { key: "departmentId", label: "Department selected for Year 2-4", type: "select", options: departmentOptions, required: true },
    { key: "year", label: "Year level", type: "select", options: ["1", "2", "3", "4"].map(id => ({ id, label: `Year ${id}` })), required: true },
    { key: "shift", label: "Learning shift", type: "select", options: ["Morning", "Afternoon", "Evening", "Weekend"].map(id => ({ id, label: id })), required: true },
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
  ] : [];
  const timetableCandidates = resource === "timetable"
    ? candidates.filter(candidate => (!selectedCourseId || candidate.values.courseId === selectedCourseId) && (!selectedTeacherId || candidate.values.teacherId === selectedTeacherId))
    : [];
  const candidateOptions = creating && isSelectableEnrollment(resource)
    ? (resource === "timetable" ? timetableCandidates : candidates).map(candidateOption)
    : [];
  const courseCodeOptions = relationshipOptions(candidates, "courseId", "courseCode", "course");
  const teacherCodeOptions = relationshipOptions(candidates.filter(candidate => !selectedCourseId || candidate.values.courseId === selectedCourseId), "teacherId", "teacherCode", "teacher");

  async function save(event: React.FormEvent) {
    event.preventDefault();
    setError("");
    if (creating && isSelectableEnrollment(resource) && !resourceId) { setError(`${candidateName(resource)} is required.`); return; }
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
    if (resource === "students" && candidate) {
      setValues(current => ({ ...current, studentCode: candidate.values.studentCode, name: candidate.values.name, email: candidate.values.email, createAt: candidate.values.createAt }));
    }
    if (resource === "timetable" && candidate) {
      setSelectedCourseId(candidate.values.courseId);
      setSelectedTeacherId(candidate.values.teacherId);
      setValues({ ...candidate.values });
    }
    if (resource === "classrooms" && candidate?.values.capacity) {
      setValues(current => ({ ...current, capacity: candidate.values.capacity }));
    }
  }

  function selectCourse(id: string) {
    setSelectedCourseId(id);
    setError("");
    const selectedSchedule = candidates.find(candidate => candidate.id === resourceId);
    if (selectedSchedule?.values.courseId !== id) {
      setResourceId("");
      setValues(enrollmentDefaults(resource, scopeDepartmentId, scopeYear));
    }
    if (!candidates.some(candidate => candidate.values.courseId === id && candidate.values.teacherId === selectedTeacherId)) setSelectedTeacherId("");
  }

  function selectTeacher(id: string) {
    setSelectedTeacherId(id);
    setError("");
    const selectedSchedule = candidates.find(candidate => candidate.id === resourceId);
    if (selectedSchedule?.values.teacherId !== id) {
      setResourceId("");
      setValues(enrollmentDefaults(resource, scopeDepartmentId, scopeYear));
    }
  }

  function changeField(field: Field, value: string) {
    setError("");
    setValues(current => ({ ...current, [field.key]: value }));
  }

  const subject = resource === "timetable" ? "timetable" : resource.slice(0, -1);
  const createTitle = resource === "timetable" ? "Add timetable" : "Add enrollment";
  const createDescription = resource === "timetable"
    ? "Select linked course, timetable, and teacher codes from Management. Department, year, classroom, day, time, and creation date come from that schedule."
    : `Select an existing ${candidateName(resource).toLowerCase()} from Academic Management, then define its current academic assignment.`;
  return <div className="modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) onClose(); }}><form className="modal management-modal" onSubmit={save} noValidate>
    <div className="modal-head"><div><span className="eyebrow">Academic enrollment service</span><h2>{creating ? createTitle : `Edit ${subject} assignment`}</h2><p>{creating ? createDescription : "This changes enrollment data only. Personal and master details remain in Academic Management."}</p></div><button type="button" className="icon-button" onClick={onClose}><Icon name="close"/></button></div>
    <div className="management-form-grid">
      {creating && resource === "students" && <div className="editor-field relationship-editor-field enrollment-candidate-field"><span>{candidateName(resource)}</span><SearchableSelect value={resourceId} options={candidateOptions} placeholder={`Type to find ${candidateName(resource).toLowerCase()}...`} ariaLabel={candidateName(resource)} ariaInvalid={Boolean(error && !resourceId)} required onChange={selectCandidate}/>{candidates.length === 0 && <small className="enrollment-candidate-note">No available {candidateName(resource).toLowerCase()} records were found in Management.</small>}</div>}
      {creating && resource === "timetable" && <>
        <div className="editor-field relationship-editor-field"><span>Course code</span><SearchableSelect value={selectedCourseId} options={courseCodeOptions} placeholder="Select Management course code..." ariaLabel="Course code" required onChange={selectCourse}/></div>
        <div className="editor-field relationship-editor-field"><span>Timetable code</span><SearchableSelect value={resourceId} options={candidateOptions} placeholder="Select Management timetable code..." ariaLabel="Timetable code" ariaInvalid={Boolean(error && !resourceId)} required onChange={selectCandidate}/></div>
        <div className="editor-field relationship-editor-field"><span>Teacher code</span><SearchableSelect value={selectedTeacherId} options={teacherCodeOptions} placeholder="Select Management teacher code..." ariaLabel="Teacher code" required onChange={selectTeacher}/></div>
        {candidates.length === 0 && <small className="enrollment-candidate-note enrollment-candidate-field">No Management schedules are available. Add a schedule in Management first.</small>}
      </>}
      {resourceId && <ManagementSelectionPreview resource={resource} values={values}/>}
      {fields.map(field => <label className="editor-field" key={field.key}><span>{field.label}</span>{field.type === "select" ? <select value={values[field.key] ?? ""} onChange={event => changeField(field, event.target.value)}>{!field.options?.some(option => option.id === "") && <option value="">Select {field.label.toLowerCase()}</option>}{field.options?.map(option => <option value={option.id} key={option.id || "none"}>{option.label}</option>)}</select> : <input type={field.type === "time" ? "time" : field.type === "number" ? "number" : "text"} min={field.type === "number" ? "1" : undefined} value={values[field.key] ?? ""} onChange={event => changeField(field, event.target.value)}/>}</label>)}
    </div>
    {error && <div className="form-error" role="alert">{error}</div>}
    <div className="modal-actions"><button type="button" className="button secondary" onClick={onClose}>Cancel</button><button className="button primary" disabled={saving || (creating && isSelectableEnrollment(resource) && candidates.length === 0)}>{saving ? "Saving assignment..." : creating ? createTitle : "Save enrollment"}</button></div>
  </form></div>;
}

function isSelectableEnrollment(resource: EnrollmentResource): resource is SelectableEnrollmentResource {
  return resource === "students" || resource === "timetable";
}

function enrollmentDefaults(resource: EnrollmentResource, departmentId: string, year: string): Record<string, string> {
  if (resource === "students") return { departmentId, year: year || "1", shift: "Morning", status: "Active" };
  if (resource === "teachers") return { departmentId, status: departmentId ? "Assigned" : "Unassigned" };
  if (resource === "courses") return { departmentId, teacherId: "", year: year || "1", capacity: "", status: "Active" };
  if (resource === "classrooms") return { departmentId, access: departmentId ? "Department only" : "Shared institute", capacity: "", status: "Available" };
  if (resource === "timetable") return { yearLevel: year || "1", dayOfWeek: "Monday", startsAt: "07:30", endsAt: "09:00", status: "Upcoming" };
  return {};
}

function candidateName(resource: EnrollmentResource) {
  if (resource === "students") return "Student profile";
  if (resource === "timetable") return "Timetable code";
  if (resource === "teachers") return "Teacher profile";
  if (resource === "courses") return "Course record";
  if (resource === "classrooms") return "Learning space";
  return "Enrollment record";
}

function candidateOption(item: EnrollmentItem): SearchableOption {
  const values = item.values;
  if (values.timetableCode) {
    return {
      id: item.id,
      label: [values.timetableCode, values.courseCode, values.teacherCode].filter(Boolean).join(" - "),
      detail: [values.enrollmentStatus, values.course, values.teacher, values.dayOfWeek, `${values.startsAt}-${values.endsAt}`, values.classroom].filter(Boolean).join(" - "),
    };
  }
  const code = values.studentCode || values.timetableCode || values.teacherCode || values.courseCode || values.classroomCode;
  const name = values.name || values.course || [values.building, values.roomType].filter(Boolean).join(" - ");
  const detail = values.email;
  return { id: item.id, label: [code, name].filter(Boolean).join(" - "), detail: detail || undefined };
}

function relationshipOptions(items: EnrollmentItem[], idKey: string, codeKey: string, nameKey: string): SearchableOption[] {
  const options = new Map<string, SearchableOption>();
  for (const item of items) {
    const id = item.values[idKey];
    if (id && !options.has(id)) options.set(id, { id, label: [item.values[codeKey], item.values[nameKey]].filter(Boolean).join(" - ") });
  }
  return [...options.values()].toSorted((left, right) => left.label.localeCompare(right.label, undefined, { numeric: true, sensitivity: "base" }));
}

function ManagementSelectionPreview({ resource, values }: { resource: EnrollmentResource; values: Record<string, string> }) {
  const details = resource === "timetable" ? [
    ["Timetable code", values.timetableCode], ["Course code", values.courseCode], ["Course", values.course], ["Teacher code", values.teacherCode], ["Teacher", values.teacher],
    ["Department", values.department], ["Year", values.yearLevel ? `Year ${values.yearLevel}` : ""], ["Classroom", values.classroom], ["Day / time", [values.dayOfWeek, values.startsAt && values.endsAt ? `${values.startsAt}-${values.endsAt}` : ""].filter(Boolean).join(" ")], ["Create At", values.createAt],
  ] : [["Code", values.studentCode], ["Name", values.name]];
  return <section className="enrollment-selection-preview">{details.map(([label, value]) => <div key={label}><span>{label}</span><strong>{value || "Select a Management record"}</strong></div>)}</section>;
}
