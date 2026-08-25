"use client";

import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { DataPagination, useDataPagination } from "@/components/data-pagination";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { ManagementDataCell } from "@/features/management/components/management-data-cell";
import { ManagementEditor } from "@/features/management/components/management-editor";
import { courseApi } from "@/features/management/courses/course-api";
import { departmentApi } from "@/features/management/departments/department-api";
import { emptyReferences } from "@/features/management/management-config";
import { teacherApi } from "@/features/management/teachers/teacher-api";
import { timetableApi } from "@/features/management/timetable/timetable-api";
import type { CourseItem } from "@/features/management/types/course";
import type { DepartmentItem } from "@/features/management/types/department";
import type { TeacherItem } from "@/features/management/types/teacher";
import type { TimetableItem } from "@/features/management/types/timetable";

export function TeacherEnrollmentWorkspace() {
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [teachers, setTeachers] = useState<TeacherItem[]>([]);
  const [departments, setDepartments] = useState<DepartmentItem[]>([]);
  const [courses, setCourses] = useState<CourseItem[]>([]);
  const [timetable, setTimetable] = useState<TimetableItem[]>([]);
  const [editing, setEditing] = useState<TeacherItem>();
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);

  const load = useCallback(() => Promise.all([teacherApi.get(query, departmentId), departmentApi.get(), courseApi.get("", departmentId), timetableApi.get("", departmentId)])
    .then(([teacherRows, departmentRows, courseRows, timetableRows]) => { setTeachers(teacherRows); setDepartments(departmentRows); setCourses(courseRows); setTimetable(timetableRows); setReady(true); setError(false); })
    .catch(() => setError(true)), [departmentId, query]);
  useEffect(() => { const timer = window.setTimeout(() => { void load(); }, 180); return () => window.clearTimeout(timer); }, [load]);

  const visible = useMemo(() => {
    const yearTeacherIds = new Set(timetable.filter(entry => !year || entry.values.yearLevel === year).map(entry => entry.values.teacherId));
    return teachers.filter(teacher => !year || yearTeacherIds.has(teacher.id)).toSorted((left, right) => left.values.teacherCode.localeCompare(right.values.teacherCode, undefined, { numeric: true }));
  }, [teachers, timetable, year]);
  const pagination = useDataPagination(visible, `teacher-enrollment-${departmentId}-${year}-${query}`);
  const selectedDepartment = departments.find(department => department.id === departmentId)?.values.name ?? "All departments";

  if (error) return <ErrorPage retry={() => { setError(false); void load(); }}/>;
  if (!ready) return <LoadingPage/>;

  return <div className="viewport-data-page management-viewport-page enrollment-viewport-page">
    <PageHeading eyebrow="Academic enrollment" title="Teacher enrollment" description="Manage each teacher's department assignment and view the linked courses, year levels, weekly classes, and learning spaces."/>
    <section className="management-toolbar panel management-toolbar-global">
      <label className="management-search"><Icon name="search" size={16}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder="Search enrolled teachers…"/></label>
      <div className="management-scope"><span>Enrollment scope</span><strong>{selectedDepartment}{year ? ` · Year ${year}` : " · All years"}</strong></div>
    </section>
    <section className="management-paginated-region">
      <section className="panel horizontal-management-table teacher-enrollment-horizontal">
        <div className="horizontal-management-head"><span>TeacherCode</span><span>Teacher Name</span><span>Department</span><span>Assigned Courses</span><span>Year Levels</span><span>Weekly Classes</span><span>Learning Spaces</span><span>Actions</span></div>
        {pagination.pageItems.map(teacher => <TeacherEnrollmentRow teacher={teacher} courses={courses} timetable={timetable} onEdit={() => setEditing(teacher)} key={teacher.id}/>) }
      </section>
      <DataPagination page={pagination.page} pageCount={pagination.pageCount} total={visible.length} onPage={pagination.setPage}/>
    </section>
    {editing && <ManagementEditor module="teachers" item={editing} references={{ ...emptyReferences, departments, teachers, courses, timetable }} scopeDepartmentId={departmentId} scopeYear={year} teacherMode="enrollment" onClose={() => setEditing(undefined)} onSaved={() => { setEditing(undefined); void load(); }}/>} 
  </div>;
}

function TeacherEnrollmentRow({ teacher, courses, timetable, onEdit }: { teacher: TeacherItem; courses: CourseItem[]; timetable: TimetableItem[]; onEdit: () => void }) {
  const assignedCourses = courses.filter(course => course.values.teacherId === teacher.id && course.values.status !== "Inactive");
  const schedule = timetable.filter(entry => entry.values.teacherId === teacher.id && entry.values.status !== "Cancelled");
  const years = [...new Set(schedule.map(entry => entry.values.yearLevel))].sort().map(value => `Year ${value}`).join(", ") || "Not scheduled";
  const rooms = [...new Set(schedule.map(entry => entry.values.classroom))].sort((left, right) => left.localeCompare(right, undefined, { numeric: true })).join(", ") || "Not scheduled";
  return <article className="horizontal-management-row">
    <ManagementDataCell label="TeacherCode"><strong className="management-code-value">{teacher.values.teacherCode}</strong></ManagementDataCell>
    <ManagementDataCell label="Teacher Name" className="horizontal-primary"><strong>{teacher.values.name}</strong><span>{teacher.values.email}</span></ManagementDataCell>
    <ManagementDataCell label="Department" className="horizontal-detail"><strong>{teacher.values.department || "Unassigned"}</strong></ManagementDataCell>
    <ManagementDataCell label="Assigned Courses" className="horizontal-detail"><strong>{assignedCourses.length}</strong><span>{assignedCourses.map(course => course.values.name).join(", ") || "No courses assigned"}</span></ManagementDataCell>
    <ManagementDataCell label="Year Levels" className="horizontal-detail"><strong>{years}</strong></ManagementDataCell>
    <ManagementDataCell label="Weekly Classes" className="relationship-number"><strong>{schedule.length}</strong></ManagementDataCell>
    <ManagementDataCell label="Learning Spaces" className="horizontal-detail"><strong>{rooms}</strong></ManagementDataCell>
    <ManagementDataCell label="Actions" className="management-action-cell"><div className="management-actions"><button type="button" onClick={onEdit}>Edit enrollment</button></div></ManagementDataCell>
  </article>;
}
