"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { ManagementEditor } from "./components/management-editor";
import { ManagementOverview } from "./components/management-overview";
import { ModuleLayout } from "./components/module-layout";
import { classroomApi } from "./classrooms/classroom-api";
import { attendanceApi } from "./attendance/attendance-api";
import { courseApi } from "./courses/course-api";
import { departmentApi } from "./departments/department-api";
import { managementApis } from "./management-apis";
import { emptyReferences, managementCopy } from "./management-config";
import { managementCode } from "./management-id";
import { studentApi } from "./students/student-api";
import { teacherApi } from "./teachers/teacher-api";
import { timetableApi } from "./timetable/timetable-api";
import type { ManagementItem, ManagementModule, References } from "./management-types";
import { TimetableEditor } from "./timetable/timetable-editor";
import type { TimetableItem } from "./types/timetable";

export function ManagementWorkspace({ module: rawModule }: { module: string }) {
  const currentModule = (rawModule in managementCopy ? rawModule : "overview") as ManagementModule;
  const resource = currentModule === "overview" ? "departments" : currentModule;
  const router = useRouter();
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [items, setItems] = useState<ManagementItem[]>([]);
  const [references, setReferences] = useState<References>(emptyReferences);
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [error, setError] = useState(false);
  const [actionError, setActionError] = useState("");
  const [ready, setReady] = useState(false);
  const [editing, setEditing] = useState<ManagementItem | null | undefined>();

  const loadReferences = useCallback(() => Promise.all([departmentApi.get(), teacherApi.get(), studentApi.get(), classroomApi.get(), courseApi.get(), timetableApi.get(), attendanceApi.get()]).then(([departments, teachers, students, classrooms, courses, timetable, attendance]) => setReferences({ departments, teachers, students, classrooms, courses, timetable, attendance })).catch(() => setError(true)), []);
  const load = useCallback(() => managementApis[resource].get(query, departmentId).then(result => { setItems(result); setReady(true); }).catch(() => setError(true)), [resource, query, departmentId]);
  useEffect(() => { void loadReferences(); }, [loadReferences]);
  useEffect(() => { const timer = window.setTimeout(load, 180); return () => window.clearTimeout(timer); }, [load]);
  useEffect(() => { const timer = window.setTimeout(() => setQuery(searchParams.get("q") ?? ""), 0); return () => window.clearTimeout(timer); }, [searchParams]);

  const selectedDepartment = references.departments.find(x => x.id === departmentId);
  const visibleItems = useMemo(() => sortItemsByYear(filterItemsByYear(items, currentModule, year, references), currentModule, references), [currentModule, items, references, year]);
  const visibleReferences = useMemo(() => sortReferencesByYear(filterReferencesByYear(references, year)), [references, year]);
  const activeItems = useMemo(() => visibleItems.filter(item => !["Inactive", "Cancelled"].includes(item.values.status)), [visibleItems]);
  if (error) return <ErrorPage retry={() => { setError(false); void loadReferences(); void load(); }}/>;
  if (!ready) return <LoadingPage/>;

  async function deactivate(item: ManagementItem) {
    if (!confirm(`Deactivate or remove this ${managementCopy[currentModule].singular}? Its history will remain read-only.`)) return;
    setActionError("");
    try { await managementApis[resource].remove(item.id); void load(); void loadReferences(); }
    catch (reason) { setActionError(reason instanceof Error ? reason.message : "This record is still used by another active record."); }
  }

  return <>
    <PageHeading eyebrow="Current data management" title={managementCopy[currentModule].title} description={managementCopy[currentModule].description} actions={currentModule !== "overview" ? <button className="button primary" onClick={() => setEditing(null)}><Icon name="plus" size={16}/>Add {managementCopy[currentModule].singular}</button> : undefined}/>
    <section className="management-toolbar panel management-toolbar-global">{currentModule !== "overview" && <label className="management-search"><Icon name="search" size={16}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder={`Search ${currentModule}…`}/></label>}<div className="management-scope"><span>Current scope</span><strong>{selectedDepartment?.values.name ?? "Whole institute"}{year ? ` · Year ${year}` : ""}</strong></div><div className="management-total"><span>Active records</span><strong>{activeItems.length}</strong></div></section>
    {actionError && <section className="management-rule-error"><Icon name="bell" size={16}/><div><strong>Relationship protected</strong><span>{actionError}</span></div><button onClick={() => setActionError("")}>Dismiss</button></section>}
    {currentModule === "overview" ? <ManagementOverview references={visibleReferences} onSelect={value => router.push(`/management/students?departmentId=${encodeURIComponent(value)}${year ? `&year=${year}` : ""}`)} selected={departmentId}/> : <ModuleLayout module={currentModule} items={visibleItems} references={visibleReferences} onEdit={setEditing} onDeactivate={deactivate}/>}
    {editing !== undefined && currentModule !== "overview" && (currentModule === "timetable"
      ? <TimetableEditor item={editing as TimetableItem | null} references={references} scopeDepartmentId={departmentId} scopeYear={year} onClose={() => setEditing(undefined)} onSaved={() => { setEditing(undefined); void load(); void loadReferences(); }}/>
      : <ManagementEditor module={currentModule} item={editing} references={references} scopeDepartmentId={departmentId} scopeYear={year} onClose={() => setEditing(undefined)} onSaved={() => { setEditing(undefined); void load(); void loadReferences(); }}/>)}
  </>;
}

function filterItemsByYear(items: ManagementItem[], module: ManagementModule, year: string, references: References) {
  if (!year) return items;
  const students = references.students.filter(student => student.values.year === year);
  const studentIds = new Set(students.map(student => student.id));
  const departmentIds = new Set(students.map(student => student.values.departmentId));
  const timetable = references.timetable.filter(entry => entry.values.yearLevel === year && entry.values.status !== "Cancelled");
  const teacherIds = new Set(timetable.map(entry => entry.values.teacherId));
  const courseIds = new Set(timetable.map(entry => entry.values.courseId));
  const classroomIds = new Set(timetable.map(entry => entry.values.classroomId));
  if (module === "students") return items.filter(item => item.values.year === year);
  if (module === "timetable") return items.filter(item => item.values.yearLevel === year);
  if (module === "attendance" || module === "grades") return items.filter(item => studentIds.has(item.values.studentId));
  if (module === "teachers") return items.filter(item => teacherIds.has(item.id));
  if (module === "courses") return items.filter(item => courseIds.has(item.id));
  if (module === "classrooms") return items.filter(item => classroomIds.has(item.id));
  if (module === "departments" || module === "overview") return items.filter(item => departmentIds.has(item.id));
  return items;
}

function filterReferencesByYear(references: References, year: string): References {
  if (!year) return references;
  const students = references.students.filter(student => student.values.year === year);
  const studentIds = new Set(students.map(student => student.id));
  return {
    ...references,
    students,
    timetable: references.timetable.filter(entry => entry.values.yearLevel === year),
    attendance: references.attendance.filter(item => studentIds.has(item.values.studentId)),
  };
}

function sortItemsByYear(items: ManagementItem[], module: ManagementModule, references: References) {
  const students = new Map(references.students.map(student => [student.id, Number(student.values.year)]));
  const studentDepartments = new Map<string, number>();
  for (const student of references.students) studentDepartments.set(student.values.departmentId, Math.min(studentDepartments.get(student.values.departmentId) ?? 99, Number(student.values.year)));
  const timetableYear = (field: "teacherId" | "courseId" | "classroomId", id: string) => references.timetable.filter(entry => entry.values[field] === id).reduce((minimum, entry) => Math.min(minimum, Number(entry.values.yearLevel)), 99);
  const yearOf = (item: ManagementItem) => {
    const values = item.values as unknown as Record<string, string>;
    if (values.year || values.yearLevel) return Number(values.year ?? values.yearLevel);
    if (module === "attendance" || module === "grades") return students.get(values.studentId) ?? 99;
    if (module === "teachers") return timetableYear("teacherId", item.id);
    if (module === "courses") return timetableYear("courseId", item.id);
    if (module === "classrooms") return timetableYear("classroomId", item.id);
    if (module === "departments" || module === "overview") return studentDepartments.get(item.id) ?? 99;
    return 99;
  };
  const businessId = (item: ManagementItem) => {
    const values = item.values as unknown as Record<string, string>;
    return managementCode(module, values) || item.id;
  };
  return items.toSorted((left, right) => yearOf(left) - yearOf(right) || businessId(left).localeCompare(businessId(right), undefined, { numeric: true, sensitivity: "base" }));
}

function sortReferencesByYear(references: References): References {
  const studentYears = new Map(references.students.map(student => [student.id, Number(student.values.year)]));
  return {
    ...references,
    departments: references.departments.toSorted((left, right) => left.values.departmentCode.localeCompare(right.values.departmentCode, undefined, { numeric: true })),
    teachers: references.teachers.toSorted((left, right) => left.values.teacherCode.localeCompare(right.values.teacherCode, undefined, { numeric: true })),
    students: references.students.toSorted((left, right) => Number(left.values.year) - Number(right.values.year) || left.values.studentCode.localeCompare(right.values.studentCode, undefined, { numeric: true })),
    classrooms: references.classrooms.toSorted((left, right) => left.values.classroomCode.localeCompare(right.values.classroomCode, undefined, { numeric: true })),
    courses: references.courses.toSorted((left, right) => left.values.courseCode.localeCompare(right.values.courseCode, undefined, { numeric: true })),
    timetable: references.timetable.toSorted((left, right) => Number(left.values.yearLevel) - Number(right.values.yearLevel) || left.values.timetableCode.localeCompare(right.values.timetableCode, undefined, { numeric: true })),
    attendance: references.attendance.toSorted((left, right) => (studentYears.get(left.values.studentId) ?? 99) - (studentYears.get(right.values.studentId) ?? 99) || left.values.attendanceCode.localeCompare(right.values.attendanceCode, undefined, { numeric: true })),
  };
}
