"use client";

import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { DataPagination, useDataPagination } from "@/components/data-pagination";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { ManagementDataCell } from "@/features/management/components/management-data-cell";
import { departmentApi } from "@/features/management/departments/department-api";
import type { DepartmentItem } from "@/features/management/types/department";
import { enrollmentApi, type EnrollmentItem, type EnrollmentResource } from "./enrollment-api";
import { EnrollmentEditor } from "./enrollment-editor";

const copy: Record<EnrollmentResource, { title: string; description: string; columns: string[] }> = {
  students: { title: "Student enrollment", description: "Place student profiles into a department, year level, learning shift, and current enrollment state.", columns: ["StudentCode", "Student", "Department / Program", "Year", "Shift", "Status", "Actions"] },
  teachers: { title: "Teacher assignment", description: "Assign teacher profiles to departments and view their linked teaching workload.", columns: ["TeacherCode", "Teacher", "Department", "Courses", "Year levels", "Weekly classes", "Status", "Actions"] },
  courses: { title: "Course assignment", description: "Place course master records into departments, years, teachers, and seat capacities.", columns: ["CourseCode", "Course", "Department", "Year", "Teacher", "Capacity", "Status", "Actions"] },
  classrooms: { title: "Classroom assignment", description: "Control which academic scope may use each learning space and view its scheduled relationships.", columns: ["ClassroomCode", "Learning space", "Access", "Courses", "Teachers", "Year levels", "Status", "Actions"] },
  timetable: { title: "Timetable assignment", description: "Schedule the assigned course, teacher, classroom, department, and Year 1-4 relationships.", columns: ["TimetableCode", "Day and time", "Course", "Department / year", "Teacher", "Classroom", "Status", "Actions"] },
  departments: { title: "Department enrollment", description: "View every department's students, teachers, courses, classrooms, and weekly classes for the selected year.", columns: ["DepartmentCode", "Department", "Year", "Students", "Teachers", "Courses", "Classrooms", "Weekly classes", "Status"] },
};

export function EnrollmentWorkspace({ resource }: { resource: EnrollmentResource }) {
  const searchParams = useSearchParams(); const departmentId = searchParams.get("departmentId") ?? ""; const year = searchParams.get("year") ?? "";
  const [query, setQuery] = useState(searchParams.get("q") ?? ""); const [items, setItems] = useState<EnrollmentItem[]>([]); const [teachers, setTeachers] = useState<EnrollmentItem[]>([]); const [courses, setCourses] = useState<EnrollmentItem[]>([]); const [classrooms, setClassrooms] = useState<EnrollmentItem[]>([]); const [departments, setDepartments] = useState<DepartmentItem[]>([]); const [editing, setEditing] = useState<EnrollmentItem>(); const [ready, setReady] = useState(false); const [error, setError] = useState(false);
  const load = useCallback(() => Promise.all([
    enrollmentApi.get(resource, query, departmentId, year), departmentApi.get(),
    resource === "courses" || resource === "timetable" ? enrollmentApi.get("teachers", "", departmentId) : Promise.resolve([]),
    resource === "timetable" ? enrollmentApi.get("courses", "", departmentId, year) : Promise.resolve([]),
    resource === "timetable" ? enrollmentApi.get("classrooms", "", departmentId, year) : Promise.resolve([]),
  ]).then(([rows, departmentRows, teacherRows, courseRows, classroomRows]) => { setItems(rows); setDepartments(departmentRows); setTeachers(teacherRows); setCourses(courseRows); setClassrooms(classroomRows); setReady(true); setError(false); }).catch(() => setError(true)), [departmentId, query, resource, year]);
  useEffect(() => { const timer = window.setTimeout(() => { void load(); }, 180); return () => window.clearTimeout(timer); }, [load]);
  const pagination = useDataPagination(items, `${resource}-enrollment-${departmentId}-${year}-${query}`); const details = copy[resource]; const selectedDepartment = departments.find(department => department.id === departmentId)?.values.name ?? "All departments";
  if (error) return <ErrorPage retry={() => { setError(false); void load(); }}/>; if (!ready) return <LoadingPage/>;
  return <div className="viewport-data-page management-viewport-page enrollment-viewport-page"><PageHeading eyebrow="Academic enrollment service" title={details.title} description={details.description}/>
    <section className="management-toolbar panel management-toolbar-global"><label className="management-search"><Icon name="search" size={16}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder={`Search ${resource}...`}/></label><div className="management-scope"><span>Enrollment scope</span><strong>{selectedDepartment}{year ? ` - Year ${year}` : " - All years"}</strong></div></section>
    <section className="management-paginated-region"><section className={`panel horizontal-management-table enrollment-service-horizontal enrollment-${resource}`}><div className="horizontal-management-head">{details.columns.map(column => <span key={column}>{column}</span>)}</div>{pagination.pageItems.map(item => <EnrollmentRow resource={resource} item={item} onEdit={() => setEditing(item)} key={item.id}/>)}</section><DataPagination page={pagination.page} pageCount={pagination.pageCount} total={items.length} onPage={pagination.setPage}/></section>
    {editing && resource !== "departments" && <EnrollmentEditor resource={resource} item={editing} departments={departments} teachers={teachers} courses={courses} classrooms={classrooms} onClose={() => setEditing(undefined)} onSaved={() => { setEditing(undefined); void load(); }}/>}</div>;
}

function EnrollmentRow({ resource, item, onEdit }: { resource: EnrollmentResource; item: EnrollmentItem; onEdit: () => void }) {
  const value = item.values; const cells = resource === "students" ? [value.studentCode, value.name, value.year === "1" ? "General foundation" : value.department, value.year ? `Year ${value.year}` : "Unassigned", value.shift || "Unassigned", value.status]
    : resource === "teachers" ? [value.teacherCode, value.name, value.department, value.courses || `${value.courseCount || 0} assigned`, value.yearLevels || "Not scheduled", value.weeklyClasses || "0", value.status]
    : resource === "courses" ? [value.courseCode, value.name, value.department, value.year ? `Year ${value.year}` : "Unassigned", value.teacher, value.capacity ? `${value.capacity} seats` : "Unassigned", value.status]
    : resource === "classrooms" ? [value.classroomCode, `${value.building} - ${value.roomType}`, value.access, value.courses || "Not scheduled", value.teachers || "Not scheduled", value.yearLevels || "Not scheduled", value.status]
    : resource === "timetable" ? [value.timetableCode, `${value.dayOfWeek} ${value.startsAt}-${value.endsAt}`, value.course, `${value.department} - Year ${value.yearLevel}`, value.teacher, value.classroom, value.status]
    : [value.departmentCode, value.name, value.year === "All" ? "All years" : `Year ${value.year}`, value.students, value.teachers, value.courses, value.classrooms, value.weeklyClasses, value.status];
  return <article className="horizontal-management-row">{cells.map((cell, index) => <ManagementDataCell label={copy[resource].columns[index]} className={index === 1 ? "horizontal-primary" : "horizontal-detail"} key={`${item.id}-${index}`}><strong>{cell || "Unassigned"}</strong></ManagementDataCell>)}{resource !== "departments" && <ManagementDataCell label="Actions" className="management-action-cell"><div className="management-actions"><button type="button" onClick={onEdit}>Edit assignment</button></div></ManagementDataCell>}</article>;
}
