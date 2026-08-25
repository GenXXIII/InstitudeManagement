"use client";

import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { DataPagination, useDataPagination } from "@/components/data-pagination";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { ManagementDataCell } from "@/features/management/components/management-data-cell";
import { ManagementEditor } from "@/features/management/components/management-editor";
import { departmentApi } from "@/features/management/departments/department-api";
import { emptyReferences } from "@/features/management/management-config";
import { studentApi } from "@/features/management/students/student-api";
import type { DepartmentItem } from "@/features/management/types/department";
import type { StudentItem } from "@/features/management/types/student";

export function StudentEnrollmentWorkspace() {
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [students, setStudents] = useState<StudentItem[]>([]);
  const [departments, setDepartments] = useState<DepartmentItem[]>([]);
  const [editing, setEditing] = useState<StudentItem>();
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);

  const load = useCallback(() => Promise.all([studentApi.get(query, departmentId), departmentApi.get()])
    .then(([studentRows, departmentRows]) => { setStudents(studentRows); setDepartments(departmentRows); setReady(true); setError(false); })
    .catch(() => setError(true)), [departmentId, query]);
  useEffect(() => { const timer = window.setTimeout(() => { void load(); }, 180); return () => window.clearTimeout(timer); }, [load]);

  const visible = useMemo(() => students
    .filter(student => !year || student.values.year === year)
    .toSorted((left, right) => Number(left.values.year) - Number(right.values.year) || left.values.studentCode.localeCompare(right.values.studentCode, undefined, { numeric: true })), [students, year]);
  const pagination = useDataPagination(visible, `enrollment-${departmentId}-${year}-${query}`);
  const selectedDepartment = departments.find(department => department.id === departmentId)?.values.name ?? "All departments";

  if (error) return <ErrorPage retry={() => { setError(false); void load(); }}/>;
  if (!ready) return <LoadingPage/>;

  return <div className="viewport-data-page management-viewport-page enrollment-viewport-page">
    <PageHeading eyebrow="Academic enrollment" title="Student enrollment" description="Manage each student's academic placement separately from their personal profile: department, year level, shift, and enrollment state."/>
    <section className="management-toolbar panel management-toolbar-global">
      <label className="management-search"><Icon name="search" size={16}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder="Search enrolled students…"/></label>
      <div className="management-scope"><span>Enrollment scope</span><strong>{selectedDepartment}{year ? ` · Year ${year}` : " · All years"}</strong></div>
    </section>
    <section className="management-paginated-region">
      <section className="panel horizontal-management-table enrollment-horizontal">
        <div className="horizontal-management-head"><span>StudentCode</span><span>Student Name</span><span>Department / Program</span><span>Year Level</span><span>Shift</span><span>Enrollment</span><span>Actions</span></div>
        {pagination.pageItems.map(student => <EnrollmentRow student={student} onEdit={() => setEditing(student)} key={student.id}/>) }
      </section>
      <DataPagination page={pagination.page} pageCount={pagination.pageCount} total={visible.length} onPage={pagination.setPage}/>
    </section>
    {editing && <ManagementEditor module="students" item={editing} references={{ ...emptyReferences, departments, students }} scopeDepartmentId={departmentId} scopeYear={year} studentMode="enrollment" onClose={() => setEditing(undefined)} onSaved={() => { setEditing(undefined); void load(); }}/>} 
  </div>;
}

function EnrollmentRow({ student, onEdit }: { student: StudentItem; onEdit: () => void }) {
  const foundationYear = student.values.year === "1";
  return <article className="horizontal-management-row">
    <ManagementDataCell label="StudentCode"><strong className="management-code-value">{student.values.studentCode}</strong></ManagementDataCell>
    <ManagementDataCell label="Student Name" className="horizontal-primary"><strong>{student.values.name}</strong><span>{student.values.email}</span></ManagementDataCell>
    <ManagementDataCell label="Department / Program" className="horizontal-detail"><strong>{foundationYear ? "General foundation" : student.values.department}</strong>{foundationYear && <small className="enrollment-foundation-note">Department selected from Year 2</small>}</ManagementDataCell>
    <ManagementDataCell label="Year Level" className="horizontal-detail"><strong>Year {student.values.year}</strong></ManagementDataCell>
    <ManagementDataCell label="Shift" className="horizontal-detail"><strong>{student.values.shift}</strong></ManagementDataCell>
    <ManagementDataCell label="Enrollment"><span className={`table-status ${student.values.status.toLowerCase().replaceAll(" ", "-")}`}>{student.values.status}</span></ManagementDataCell>
    <ManagementDataCell label="Actions" className="management-action-cell"><div className="management-actions"><button type="button" onClick={onEdit}>Edit enrollment</button></div></ManagementDataCell>
  </article>;
}
