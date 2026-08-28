"use client";

import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { DataPagination, useDataPagination } from "@/components/data-pagination";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { ManagementDataCell } from "@/features/management/components/management-data-cell";
import { departmentApi } from "@/features/management/departments/department-api";
import { studentApi } from "@/features/management/students/student-api";
import { timetableApi } from "@/features/management/timetable/timetable-api";
import type { DepartmentItem } from "@/features/management/types/department";
import { enrollmentApi, type EnrollmentItem, type EnrollmentResource } from "./enrollment-api";
import { EnrollmentEditor } from "./enrollment-editor";

type AssignableEnrollmentResource = "students" | "teachers" | "courses" | "classrooms";
type SelectableEnrollmentResource = "students" | "timetable";
type EnrollmentDisplayItem = EnrollmentItem & { rowKey: string; assignedCourse?: string };

const copy: Record<EnrollmentResource, { title: string; description: string; columns: string[] }> = {
  students: { title: "Student Enrollment", description: "Select a student added in Management, then enroll their code and name into a department, year, and learning shift.", columns: ["Code", "Name", "Year", "Shift", "Department", "Actions"] },
  timetable: { title: "Timetable Enrollment", description: "Add and manage enrolled schedules by timetable, course, and teacher code with their department, year, classroom, day and time, and creation date.", columns: ["Code", "Course", "Teacher", "Department", "Year", "Classroom", "Day / time", "Create At", "Actions"] },
  "student-assignments": { title: "Student Assign", description: "View each enrolled student's department, year, shift, assigned courses, classrooms, and weekly classes.", columns: ["Code", "Student", "Department", "Year / shift", "Assigned courses", "Assigned classrooms", "Weekly classes", "Actions"] },
  teachers: { title: "Teacher Assign", description: "View and manage what each teacher is assigned to across departments, courses, year levels, and weekly classes.", columns: ["Code", "Teacher", "Department", "Assigned courses", "Year levels", "Weekly classes", "Actions"] },
  courses: { title: "Course Assign", description: "View and manage the department and year assigned to each course.", columns: ["Code", "Course", "Department", "Year", "Actions"] },
  classrooms: { title: "Classroom Assign", description: "View each classroom-course assignment as its own row with classroom access and seat capacity.", columns: ["Code", "Classroom", "Access", "Assigned course", "Capacity", "Actions"] },
  departments: { title: "Department Assign", description: "View what is assigned to each department for the selected year.", columns: ["Code", "Department", "Year", "Students", "Teachers", "Courses", "Classrooms", "Weekly classes"] },
};

export function EnrollmentWorkspace({ resource }: { resource: EnrollmentResource }) {
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [items, setItems] = useState<EnrollmentItem[]>([]);
  const [candidates, setCandidates] = useState<EnrollmentItem[]>([]);
  const [teachers, setTeachers] = useState<EnrollmentItem[]>([]);
  const [studentSchedules, setStudentSchedules] = useState<EnrollmentItem[]>([]);
  const [departments, setDepartments] = useState<DepartmentItem[]>([]);
  const [editing, setEditing] = useState<EnrollmentItem | null | undefined>();
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);
  const [actionError, setActionError] = useState("");

  const load = useCallback(() => {
    const candidateRequest: Promise<EnrollmentItem[]> = isSelectableEnrollment(resource)
      ? Promise.all([getCatalogCandidates(resource, departmentId, year), enrollmentApi.get(resource)]).then(([catalogItems, enrollmentItems]) => {
          const assignedIds = new Set(enrollmentItems.filter(item => item.values.status !== "Unassigned").map(item => item.id));
          if (resource === "timetable") {
            return catalogItems.map(item => ({
              ...item,
              values: { ...item.values, enrollmentStatus: assignedIds.has(item.id) ? "Already enrolled" : "Available to enroll" },
            }));
          }
          return catalogItems.filter(item => !assignedIds.has(item.id));
        })
      : Promise.resolve([]);

    return Promise.all([
      enrollmentApi.get(resource, query, departmentId, year),
      departmentApi.get(),
      resource === "courses" ? enrollmentApi.get("teachers", "", departmentId) : Promise.resolve([]),
      candidateRequest,
      resource === "student-assignments" ? enrollmentApi.get("timetable", "", departmentId, year) : Promise.resolve([]),
    ]).then(([rows, departmentRows, teacherRows, candidateRows, scheduleRows]) => {
      setItems(rows);
      setDepartments(departmentRows);
      setTeachers(teacherRows);
      setCandidates(candidateRows);
      setStudentSchedules(scheduleRows);
      setReady(true);
      setError(false);
    }).catch(() => setError(true));
  }, [departmentId, query, resource, year]);

  useEffect(() => {
    const timer = window.setTimeout(() => { void load(); }, 180);
    return () => window.clearTimeout(timer);
  }, [load]);

  const assignedItems = useMemo(() => isAssignableEnrollment(resource) || resource === "student-assignments" ? items.filter(item => item.values.status !== "Unassigned") : items, [items, resource]);
  const displayItems = useMemo<EnrollmentDisplayItem[]>(() => resource === "classrooms"
    ? assignedItems.flatMap(item => {
        const assignedCourses = item.values.courses?.split(",").map(course => course.trim()).filter(Boolean) ?? [];
        return (assignedCourses.length ? assignedCourses : ["Not scheduled"]).map((assignedCourse, index) => ({ ...item, assignedCourse, rowKey: `${item.id}-${index}-${assignedCourse}` }));
      })
    : assignedItems.map(item => ({ ...item, rowKey: item.id })), [assignedItems, resource]);
  const sortedItems = useMemo(() => sortEnrollmentItems(displayItems, resource), [displayItems, resource]);
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
      actions={resource === "students" || resource === "timetable" ? <button type="button" className="button primary" onClick={() => setEditing(null)}><Icon name="plus" size={16}/>{resource === "timetable" ? "Add timetable" : "Add student enrollment"}</button> : undefined}
    />
    <section className="management-toolbar panel management-toolbar-global">
      <label className="management-search"><Icon name="search" size={16}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder={`Search ${resource}...`}/></label>
      <div className="management-scope"><span>Enrollment scope</span><strong>{selectedDepartment}{year ? ` - Year ${year}` : " - All years"}</strong></div>
    </section>
    {actionError && <section className="management-rule-error"><Icon name="bell" size={16}/><div><strong>Enrollment relationship protected</strong><span>{actionError}</span></div><button type="button" onClick={() => setActionError("")}>Dismiss</button></section>}
    <section className="management-paginated-region">
      <section className={`panel horizontal-management-table enrollment-service-horizontal enrollment-${resource}`}>
        <div className="horizontal-management-head">{details.columns.map(column => <span key={column}>{column}</span>)}</div>
        {pagination.pageItems.map(item => <EnrollmentRow resource={resource} item={item} studentSchedules={studentSchedules} onEdit={() => setEditing(item)} onRemove={() => { void remove(item); }} key={item.rowKey}/>)}
      </section>
      <DataPagination page={pagination.page} pageCount={pagination.pageCount} total={sortedItems.length} onPage={pagination.setPage}/>
    </section>
    {editing !== undefined && resource !== "departments" && <EnrollmentEditor
      resource={resource === "student-assignments" ? "students" : resource}
      item={editing}
      candidates={candidates}
      departments={departments}
      teachers={teachers}
      scopeDepartmentId={departmentId}
      scopeYear={year}
      onClose={() => setEditing(undefined)}
      onSaved={() => { setEditing(undefined); void load(); }}
    />}
  </div>;
}

function EnrollmentRow({ resource, item, studentSchedules, onEdit, onRemove }: { resource: EnrollmentResource; item: EnrollmentDisplayItem; studentSchedules: EnrollmentItem[]; onEdit: () => void; onRemove: () => void }) {
  const value = item.values;
  const relatedSchedules = resource === "student-assignments" ? studentSchedules.filter(schedule => schedule.values.departmentId === value.departmentId && schedule.values.yearLevel === value.year && scheduleMatchesShift(schedule, value.shift)) : [];
  const cells = resource === "students" ? [value.studentCode, value.name, value.year ? `Year ${value.year}` : "Unassigned", value.shift || "Unassigned", value.year === "1" ? "General foundation" : value.department]
    : resource === "student-assignments" ? [value.studentCode, value.name, value.year === "1" ? "General foundation" : value.department, [value.year ? `Year ${value.year}` : "Unassigned", value.shift].filter(Boolean).join(" / "), uniqueValues(relatedSchedules, "course"), uniqueValues(relatedSchedules, "classroom"), relatedSchedules.length.toString()]
    : resource === "teachers" ? [value.teacherCode, value.name, value.department, value.courses || `${value.courseCount || 0} assigned`, value.yearLevels || "Not scheduled", value.weeklyClasses || "0"]
    : resource === "courses" ? [value.courseCode, value.name, value.department, value.year ? `Year ${value.year}` : "Unassigned"]
    : resource === "classrooms" ? [value.classroomCode, `${value.building} - ${value.roomType}`, value.access, item.assignedCourse || "Not scheduled", value.capacity ? `${value.capacity} seats` : "Unassigned"]
    : resource === "timetable" ? [value.timetableCode, [value.courseCode, value.course].filter(Boolean).join(" - "), [value.teacherCode, value.teacher].filter(Boolean).join(" - "), value.department, value.yearLevel ? `Year ${value.yearLevel}` : "Unassigned", value.classroom, `${value.dayOfWeek} ${value.startsAt}-${value.endsAt}`, value.createAt]
    : [value.departmentCode, value.name, value.year === "All" ? "All years" : `Year ${value.year}`, value.students, value.teachers, value.courses, value.classrooms, value.weeklyClasses];

  return <article className="horizontal-management-row">
    {cells.map((cell, index) => {
      const relationship = (resource === "classrooms" && index === 3) || (resource === "teachers" && index === 3) || (resource === "student-assignments" && (index === 4 || index === 5));
      const className = [index === 1 ? "horizontal-primary" : "horizontal-detail", relationship ? "enrollment-relationship-cell" : ""].filter(Boolean).join(" ");
      return <ManagementDataCell label={copy[resource].columns[index]} className={className} key={`${item.id}-${index}`}>
        <strong className={relationship ? "enrollment-relationship-value" : undefined} title={relationship ? cell : undefined}>{cell || "Unassigned"}</strong>
      </ManagementDataCell>;
    })}
    {resource !== "departments" && <ManagementDataCell label="Actions" className="management-action-cell"><div className="management-actions"><button type="button" onClick={onEdit}>Edit</button><button type="button" className="danger" onClick={onRemove}>Remove</button></div></ManagementDataCell>}
  </article>;
}

function isAssignableEnrollment(resource: EnrollmentResource): resource is AssignableEnrollmentResource {
  return resource === "students" || resource === "teachers" || resource === "courses" || resource === "classrooms";
}

function getCatalogCandidates(resource: SelectableEnrollmentResource, departmentId: string, year: string): Promise<EnrollmentItem[]> {
  if (resource === "students") return studentApi.get();
  return timetableApi.get("", departmentId).then(items => items.filter(item => !year || item.values.yearLevel === year));
}

function isSelectableEnrollment(resource: EnrollmentResource): resource is SelectableEnrollmentResource {
  return resource === "students" || resource === "timetable";
}

function sortEnrollmentItems<T extends EnrollmentItem>(items: T[], resource: EnrollmentResource) {
  return items.toSorted((left, right) => {
    const yearDifference = enrollmentYear(left, resource) - enrollmentYear(right, resource);
    if (yearDifference) return yearDifference;
    const codeDifference = enrollmentCode(left).localeCompare(enrollmentCode(right), undefined, { numeric: true, sensitivity: "base" });
    if (codeDifference) return codeDifference;
    return assignedCourseName(left).localeCompare(assignedCourseName(right), undefined, { numeric: true, sensitivity: "base" });
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
  if (resource === "students" || resource === "student-assignments") return "student enrollment";
  if (resource === "teachers") return "teacher";
  if (resource === "courses") return "course";
  if (resource === "classrooms") return "classroom";
  return "timetable";
}

function assignedCourseName(item: EnrollmentItem) {
  return "assignedCourse" in item && typeof item.assignedCourse === "string" ? item.assignedCourse : "";
}

function scheduleMatchesShift(schedule: EnrollmentItem, shift: string | undefined) {
  if (!shift) return true;
  if (shift === "Weekend") return schedule.values.dayOfWeek === "Saturday" || schedule.values.dayOfWeek === "Sunday";
  const hour = Number(schedule.values.startsAt?.slice(0, 2));
  return shift === "Morning" ? hour < 13 : shift === "Afternoon" ? hour >= 13 && hour < 17 : hour >= 17;
}

function uniqueValues(items: EnrollmentItem[], key: string) {
  return [...new Set(items.map(item => item.values[key]).filter(Boolean))].join(", ") || "Not scheduled";
}
