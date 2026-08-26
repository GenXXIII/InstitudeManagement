"use client";

import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { DataPagination, useDataPagination } from "@/components/data-pagination";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { classroomApi } from "@/features/management/classrooms/classroom-api";
import { ManagementDataCell } from "@/features/management/components/management-data-cell";
import { courseApi } from "@/features/management/courses/course-api";
import { departmentApi } from "@/features/management/departments/department-api";
import { studentApi } from "@/features/management/students/student-api";
import { teacherApi } from "@/features/management/teachers/teacher-api";
import type { DepartmentItem } from "@/features/management/types/department";
import { enrollmentApi, type EnrollmentItem, type EnrollmentResource } from "./enrollment-api";
import { EnrollmentEditor } from "./enrollment-editor";

type AssignableEnrollmentResource = "students" | "teachers" | "courses" | "classrooms";

const copy: Record<EnrollmentResource, { title: string; description: string; columns: string[] }> = {
  students: { title: "Student enrollment", description: "Place student profiles into a department, year level, learning shift, and current enrollment state.", columns: ["StudentCode", "Student", "Department / Program", "Year", "Shift", "Actions"] },
  teachers: { title: "Teacher assignment", description: "Assign teacher profiles to departments and view their linked teaching workload.", columns: ["TeacherCode", "Teacher", "Department", "Courses", "Year levels", "Weekly classes", "Actions"] },
  courses: { title: "Course assignment", description: "Place course master records into departments, years, teachers, and seat capacities.", columns: ["CourseCode", "Course", "Department", "Year", "Teacher", "Actions"] },
  classrooms: { title: "Classroom assignment", description: "Control which academic scope may use each learning space and view its scheduled relationships.", columns: ["ClassroomCode", "Classroom", "Access", "Courses", "Capacity", "Actions"] },
  timetable: { title: "Timetable assignment", description: "Schedule the assigned course, teacher, classroom, department, and Year 1-4 relationships.", columns: ["TimetableCode", "Day and time", "Course", "Department / year", "Teacher", "Actions"] },
  departments: { title: "Department enrollment", description: "View every department's students, teachers, courses, classrooms, and weekly classes for the selected year.", columns: ["DepartmentCode", "Department", "Year", "Students", "Teachers", "Courses", "Classrooms", "Weekly classes"] },
};

export function EnrollmentWorkspace({ resource }: { resource: EnrollmentResource }) {
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [items, setItems] = useState<EnrollmentItem[]>([]);
  const [candidates, setCandidates] = useState<EnrollmentItem[]>([]);
  const [teachers, setTeachers] = useState<EnrollmentItem[]>([]);
  const [courses, setCourses] = useState<EnrollmentItem[]>([]);
  const [classrooms, setClassrooms] = useState<EnrollmentItem[]>([]);
  const [departments, setDepartments] = useState<DepartmentItem[]>([]);
  const [editing, setEditing] = useState<EnrollmentItem | null | undefined>();
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);
  const [actionError, setActionError] = useState("");

  const load = useCallback(() => {
    const candidateRequest: Promise<EnrollmentItem[]> = canAddEnrollment(resource)
      ? Promise.all([getCatalogCandidates(resource), enrollmentApi.get(resource)]).then(([catalogItems, enrollmentItems]) => {
          const assignedIds = new Set(enrollmentItems.filter(item => item.values.status !== "Unassigned").map(item => item.id));
          return catalogItems.filter(item => !assignedIds.has(item.id));
        })
      : Promise.resolve([]);

    return Promise.all([
      enrollmentApi.get(resource, query, departmentId, year),
      departmentApi.get(),
      resource === "courses" || resource === "timetable" ? enrollmentApi.get("teachers", "", departmentId) : Promise.resolve([]),
      resource === "timetable" ? enrollmentApi.get("courses", "", departmentId, year) : Promise.resolve([]),
      resource === "timetable" ? enrollmentApi.get("classrooms", "", departmentId, year) : Promise.resolve([]),
      candidateRequest,
    ]).then(([rows, departmentRows, teacherRows, courseRows, classroomRows, candidateRows]) => {
      setItems(rows);
      setDepartments(departmentRows);
      setTeachers(teacherRows);
      setCourses(courseRows);
      setClassrooms(classroomRows);
      setCandidates(candidateRows);
      setReady(true);
      setError(false);
    }).catch(() => setError(true));
  }, [departmentId, query, resource, year]);

  useEffect(() => {
    const timer = window.setTimeout(() => { void load(); }, 180);
    return () => window.clearTimeout(timer);
  }, [load]);

  const assignedItems = useMemo(() => canAddEnrollment(resource) ? items.filter(item => item.values.status !== "Unassigned") : items, [items, resource]);
  const sortedItems = useMemo(() => sortEnrollmentItems(assignedItems, resource), [assignedItems, resource]);
  const pagination = useDataPagination(sortedItems, `${resource}-enrollment-${departmentId}-${year}-${query}`);
  const details = copy[resource];
  const selectedDepartment = departments.find(department => department.id === departmentId)?.values.name ?? "All departments";

  if (error) return <ErrorPage retry={() => { setError(false); void load(); }}/>;
  if (!ready) return <LoadingPage/>;

  async function remove(item: EnrollmentItem) {
    if (!confirm(`Remove this ${enrollmentSubject(resource)} assignment? The master record will remain in Academic Management.`)) return;
    setActionError("");
    try {
      await enrollmentApi.remove(resource, item.id);
      void load();
    } catch (reason) {
      setActionError(reason instanceof Error ? reason.message : "Could not remove this enrollment assignment.");
    }
  }

  return <div className="viewport-data-page management-viewport-page enrollment-viewport-page">
    <PageHeading
      eyebrow="Academic enrollment service"
      title={details.title}
      description={details.description}
      actions={canAddEnrollment(resource) ? <button type="button" className="button primary" onClick={() => setEditing(null)}><Icon name="plus" size={16}/>Add enrollment</button> : undefined}
    />
    <section className="management-toolbar panel management-toolbar-global">
      <label className="management-search"><Icon name="search" size={16}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder={`Search ${resource}...`}/></label>
      <div className="management-scope"><span>Enrollment scope</span><strong>{selectedDepartment}{year ? ` - Year ${year}` : " - All years"}</strong></div>
    </section>
    {actionError && <section className="management-rule-error"><Icon name="bell" size={16}/><div><strong>Enrollment relationship protected</strong><span>{actionError}</span></div><button type="button" onClick={() => setActionError("")}>Dismiss</button></section>}
    <section className="management-paginated-region">
      <section className={`panel horizontal-management-table enrollment-service-horizontal enrollment-${resource}`}>
        <div className="horizontal-management-head">{details.columns.map(column => <span key={column}>{column}</span>)}</div>
        {pagination.pageItems.map(item => <EnrollmentRow resource={resource} item={item} onEdit={() => setEditing(item)} onRemove={() => { void remove(item); }} key={item.id}/>)}
      </section>
      <DataPagination page={pagination.page} pageCount={pagination.pageCount} total={sortedItems.length} onPage={pagination.setPage}/>
    </section>
    {editing !== undefined && resource !== "departments" && <EnrollmentEditor
      resource={resource}
      item={editing}
      candidates={candidates}
      departments={departments}
      teachers={teachers}
      courses={courses}
      classrooms={classrooms}
      scopeDepartmentId={departmentId}
      scopeYear={year}
      onClose={() => setEditing(undefined)}
      onSaved={() => { setEditing(undefined); void load(); }}
    />}
  </div>;
}

function EnrollmentRow({ resource, item, onEdit, onRemove }: { resource: EnrollmentResource; item: EnrollmentItem; onEdit: () => void; onRemove: () => void }) {
  const value = item.values;
  const cells = resource === "students" ? [value.studentCode, value.name, value.year === "1" ? "General foundation" : value.department, value.year ? `Year ${value.year}` : "Unassigned", value.shift || "Unassigned"]
    : resource === "teachers" ? [value.teacherCode, value.name, value.department, value.courses || `${value.courseCount || 0} assigned`, value.yearLevels || "Not scheduled", value.weeklyClasses || "0"]
    : resource === "courses" ? [value.courseCode, value.name, value.department, value.year ? `Year ${value.year}` : "Unassigned", value.teacher]
    : resource === "classrooms" ? [value.classroomCode, `${value.building} - ${value.roomType}`, value.access, value.courses || "Not scheduled", value.capacity ? `${value.capacity} seats` : "Unassigned"]
    : resource === "timetable" ? [value.timetableCode, `${value.dayOfWeek} ${value.startsAt}-${value.endsAt}`, value.course, `${value.department} - Year ${value.yearLevel}`, value.teacher]
    : [value.departmentCode, value.name, value.year === "All" ? "All years" : `Year ${value.year}`, value.students, value.teachers, value.courses, value.classrooms, value.weeklyClasses];

  return <article className="horizontal-management-row">
    {cells.map((cell, index) => {
      const relationship = (resource === "classrooms" && index === 3) || (resource === "teachers" && index === 3);
      const className = [index === 1 ? "horizontal-primary" : "horizontal-detail", relationship ? "enrollment-relationship-cell" : ""].filter(Boolean).join(" ");
      return <ManagementDataCell label={copy[resource].columns[index]} className={className} key={`${item.id}-${index}`}>
        <strong className={relationship ? "enrollment-relationship-value" : undefined} title={relationship ? cell : undefined}>{cell || "Unassigned"}</strong>
      </ManagementDataCell>;
    })}
    {resource !== "departments" && <ManagementDataCell label="Actions" className="management-action-cell"><div className="management-actions"><button type="button" onClick={onEdit}>Edit</button><button type="button" className="danger" onClick={onRemove}>Remove</button></div></ManagementDataCell>}
  </article>;
}

function canAddEnrollment(resource: EnrollmentResource): resource is AssignableEnrollmentResource {
  return resource === "students" || resource === "teachers" || resource === "courses" || resource === "classrooms";
}

function getCatalogCandidates(resource: AssignableEnrollmentResource): Promise<EnrollmentItem[]> {
  if (resource === "students") return studentApi.get();
  if (resource === "teachers") return teacherApi.get();
  if (resource === "courses") return courseApi.get();
  return classroomApi.get();
}

function sortEnrollmentItems(items: EnrollmentItem[], resource: EnrollmentResource) {
  return items.toSorted((left, right) => {
    const yearDifference = enrollmentYear(left, resource) - enrollmentYear(right, resource);
    if (yearDifference) return yearDifference;
    return enrollmentCode(left).localeCompare(enrollmentCode(right), undefined, { numeric: true, sensitivity: "base" });
  });
}

function enrollmentYear(item: EnrollmentItem, resource: EnrollmentResource) {
  const value = resource === "timetable" ? item.values.yearLevel : item.values.year || item.values.yearLevels;
  const match = value?.match(/\d+/);
  return match ? Number(match[0]) : 99;
}

function enrollmentCode(item: EnrollmentItem) {
  const values = item.values;
  return values.studentCode || values.teacherCode || values.courseCode || values.classroomCode || values.timetableCode || values.departmentCode || item.id;
}

function enrollmentSubject(resource: EnrollmentResource) {
  if (resource === "students") return "student enrollment";
  if (resource === "teachers") return "teacher";
  if (resource === "courses") return "course";
  if (resource === "classrooms") return "classroom";
  return "timetable";
}
