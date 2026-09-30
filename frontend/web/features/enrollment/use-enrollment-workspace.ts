"use client";

import { useCallback, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { classroomApi } from "@/features/management/classrooms/classroom-api";
import { courseApi } from "@/features/management/courses/course-api";
import { departmentApi } from "@/features/management/departments/department-api";
import { studentApi } from "@/features/management/students/student-api";
import { teacherApi } from "@/features/management/teachers/teacher-api";
import { timetableApi } from "@/features/timetable/timetable-api";
import type { DepartmentItem } from "@/features/management/departments/department-types";
import { emptyPage, type PagedResult } from "@/lib/pagination";
import { workflowSourceSearch } from "@/lib/workflow-code";
import { useServerPage } from "@/components/data-table";
import type { EnrollmentItem, EnrollmentResource } from "./common/enrollment-types";
import { enrollmentApiFor } from "./enrollment-apis";
import {
  enrollmentSubject,
  isSelectableEnrollment,
  type SelectableEnrollmentResource,
} from "./enrollment-workspace-model";

export function useEnrollmentWorkspace(resource: EnrollmentResource) {
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [result, setResult] = useState<PagedResult<EnrollmentItem>>(() => emptyPage());
  const [page, setPage] = useServerPage(`${resource}-${departmentId}-${year}-${query}`);
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
    const candidateRequest: Promise<EnrollmentItem[]> = editing !== undefined && isSelectableEnrollment(resource)
      ? Promise.all([getCatalogCandidates(resource, departmentId, year), enrollmentApiFor(resource).get()]).then(([catalogItems, enrollmentItems]) => {
          const assignedIds = new Set(enrollmentItems.filter(item => item.values.status !== "Unassigned" && item.values.periodState !== "Retained").map(item => item.id));
          if (resource === "timetable") {
            return catalogItems.filter(item => !assignedIds.has(item.id)).map(item => ({
              ...item,
              values: { ...item.values, enrollmentStatus: "Available to enroll" },
            }));
          }
          return catalogItems.filter(item => !assignedIds.has(item.id));
        })
      : Promise.resolve([]);

    return Promise.all([
      enrollmentApiFor(resource).getPage(workflowSourceSearch(query), departmentId, year, { page }),
      departmentApi.get(),
      resource === "timetable" ? teacherApi.get() : Promise.resolve([]),
      resource === "timetable" ? courseApi.get() : Promise.resolve([]),
      resource === "timetable" ? classroomApi.get() : Promise.resolve([]),
      candidateRequest,
    ]).then(([rows, departmentRows, teacherRows, courseRows, classroomRows, candidateRows]) => {
      setResult(rows);
      setDepartments(departmentRows);
      setTeachers(teacherRows);
      setCourses(courseRows);
      setClassrooms(classroomRows);
      setCandidates(candidateRows);
      setReady(true);
      setError(false);
    }).catch(() => setError(true));
  }, [departmentId, editing, page, query, resource, year]);

  useEffect(() => {
    const timer = window.setTimeout(() => { void load(); }, 180);
    return () => window.clearTimeout(timer);
  }, [load]);

  async function remove(item: EnrollmentItem) {
    if (!confirm(`Remove this ${enrollmentSubject(resource)} assignment? The master record will remain in Academic Management.`)) return;
    setActionError("");
    try {
      await enrollmentApiFor(resource).remove(item.id);
      void load();
    } catch (reason) {
      setActionError(reason instanceof Error ? reason.message : "Could not remove this enrollment assignment.");
    }
  }

  return {
    actionError,
    candidates,
    classrooms,
    courses,
    departmentId,
    departments,
    editing,
    error,
    items: result.items,
    page,
    result,
    load,
    query,
    ready,
    remove,
    setActionError,
    setEditing,
    setError,
    setQuery,
    setPage,
    teachers,
    year,
  };
}

function getCatalogCandidates(resource: SelectableEnrollmentResource, departmentId: string, year: string): Promise<EnrollmentItem[]> {
  if (resource === "students") return studentApi.get();
  void year;
  return timetableApi.get("", departmentId);
}
