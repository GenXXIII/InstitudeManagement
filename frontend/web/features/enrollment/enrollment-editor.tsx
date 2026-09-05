"use client";

import { useState } from "react";
import { Icon } from "@/components/icon";
import { SearchableSelect } from "@/components/searchable-select";
import { useInstituteSettings } from "@/features/administration/institute-settings-context";
import type { DepartmentItem } from "@/features/management/departments/department-types";
import type { EnrollmentItem, EnrollmentResource } from "./common/enrollment-types";
import { enrollmentApiFor } from "./enrollment-apis";
import {
  buildEnrollmentFields,
  candidateName,
  candidateOption,
  enrollmentCodeResource,
  enrollmentDefaults,
  relationshipOptions,
  type EnrollmentField,
} from "./enrollment-editor-config";
import { isSelectableEnrollment } from "./enrollment-workspace-model";
import { formatAssignedCode, workflowCodeExample } from "@/lib/workflow-code";

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
  const { settings } = useInstituteSettings();
  const [resourceId, setResourceId] = useState(item?.id ?? "");
  const [values, setValues] = useState<Record<string, string>>(() => item ? { ...item.values } : enrollmentDefaults(resource, scopeDepartmentId, scopeYear, settings.courses.defaultCapacity));
  const [selectedCourseId, setSelectedCourseId] = useState(item?.values.courseId ?? "");
  const [selectedTeacherId, setSelectedTeacherId] = useState(item?.values.teacherId ?? "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const creating = item === null;
  const teacherRequired = settings.courses.requireAssignedTeacher === "true";
  const allowCrossDepartment = settings.departments.allowCrossDepartmentTeaching === "true";
  const availableTeachers = teachers.filter(teacher => teacher.values.status === "Assigned" && (allowCrossDepartment || !values.departmentId || !teacher.values.departmentId || teacher.values.departmentId === values.departmentId));
  const fields = buildEnrollmentFields({ resource, departments, availableTeachers, teacherRequired });
  const codeResource = enrollmentCodeResource(resource);
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
    const submittedValues = { ...values };
    if (codeResource && submittedValues.enrollmentCode?.trim()) submittedValues.enrollmentCode = formatAssignedCode(submittedValues.enrollmentCode, codeResource, "enrollment");
    const missing = fields.find(field => field.required && !submittedValues[field.key]?.trim());
    if (missing) { setError(`${missing.label} is required.`); return; }
    if (submittedValues.enrollmentCode && !/^[A-Za-z0-9][A-Za-z0-9._/-]{0,63}$/.test(submittedValues.enrollmentCode)) { setError("EnrollmentCode must be 1 to 64 characters using letters, numbers, dot, underscore, slash, or hyphen."); return; }
    setSaving(true);
    try {
      setValues(submittedValues);
      await enrollmentApiFor(resource).update(item?.id ?? resourceId, submittedValues);
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
      setValues(current => ({ ...candidate.values, enrollmentCode: current.enrollmentCode ?? "" }));
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
      setValues(current => ({ ...enrollmentDefaults(resource, scopeDepartmentId, scopeYear, settings.courses.defaultCapacity), enrollmentCode: current.enrollmentCode ?? "" }));
    }
    if (!candidates.some(candidate => candidate.values.courseId === id && candidate.values.teacherId === selectedTeacherId)) setSelectedTeacherId("");
  }

  function selectTeacher(id: string) {
    setSelectedTeacherId(id);
    setError("");
    const selectedSchedule = candidates.find(candidate => candidate.id === resourceId);
    if (selectedSchedule?.values.teacherId !== id) {
      setResourceId("");
      setValues(current => ({ ...enrollmentDefaults(resource, scopeDepartmentId, scopeYear, settings.courses.defaultCapacity), enrollmentCode: current.enrollmentCode ?? "" }));
    }
  }

  function changeField(field: EnrollmentField, value: string) {
    setError("");
    setValues(current => {
      const next = { ...current, [field.key]: value };
      if (resource === "courses" && field.key === "departmentId" && !allowCrossDepartment) {
        const selectedTeacher = teachers.find(teacher => teacher.id === current.teacherId);
        if (selectedTeacher?.values.departmentId && selectedTeacher.values.departmentId !== value) next.teacherId = "";
      }
      return next;
    });
  }

  const subject = resource === "timetable" ? "timetable" : resource.slice(0, -1);
  const createTitle = resource === "timetable" ? "Add timetable" : "Add enrollment";
  const createDescription = resource === "timetable"
    ? "Select linked course, timetable, and teacher codes from Management. Department, year, classroom, day, time, and creation date come from that schedule."
    : `Assign its own EnrollmentCode, select an existing ${candidateName(resource).toLowerCase()} from Academic Management, then define its current academic assignment.`;
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
      {fields.map(field => <label className="editor-field" key={field.key}><span>{field.label}</span>{field.type === "select" ? <select value={values[field.key] ?? ""} onChange={event => changeField(field, event.target.value)}>{!field.options?.some(option => option.id === "") && <option value="">Select {field.label.toLowerCase()}</option>}{field.options?.map(option => <option value={option.id} key={option.id || "none"}>{option.label}</option>)}</select> : <input type={field.type === "time" ? "time" : field.type === "number" ? "number" : "text"} min={field.type === "number" ? "1" : undefined} value={values[field.key] ?? ""} onChange={event => changeField(field, event.target.value)}/>} {field.key === "enrollmentCode" && codeResource && <small>Final code: {values.enrollmentCode?.trim() ? formatAssignedCode(values.enrollmentCode, codeResource, "enrollment") : workflowCodeExample(codeResource, "enrollment")}</small>}</label>)}
    </div>
    {error && <div className="form-error" role="alert">{error}</div>}
    <div className="modal-actions"><button type="button" className="button secondary" onClick={onClose}>Cancel</button><button className="button primary" disabled={saving || (creating && isSelectableEnrollment(resource) && candidates.length === 0)}>{saving ? "Saving assignment..." : creating ? createTitle : "Save enrollment"}</button></div>
  </form></div>;
}

function ManagementSelectionPreview({ resource, values }: { resource: EnrollmentResource; values: Record<string, string> }) {
  const details = resource === "timetable" ? [
    ["Timetable code", values.timetableCode], ["Course code", values.courseCode], ["Course", values.course], ["Teacher code", values.teacherCode], ["Teacher", values.teacher],
    ["Department", values.department], ["Year", values.yearLevel ? `Year ${values.yearLevel}` : ""], ["Classroom", values.classroom], ["Day / time", [values.dayOfWeek, values.startsAt && values.endsAt ? `${values.startsAt}-${values.endsAt}` : ""].filter(Boolean).join(" ")], ["Create At", values.createAt],
  ] : [["Code", values.studentCode], ["Name", values.name]];
  return <section className="enrollment-selection-preview">{details.map(([label, value]) => <div key={label}><span>{label}</span><strong>{value || "Select a Management record"}</strong></div>)}</section>;
}
