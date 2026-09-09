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
import { workflowSourceSearch } from "@/lib/workflow-code";
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
    const candidateRequest: Promise<EnrollmentItem[]> = isSelectableEnrollment(resource)
      ? Promise.all([getCatalogCandidates(resource, departmentId, year), enrollmentApiFor(resource).get()]).then(([catalogItems, enrollmentItems]) => {
          const assignedIds = new Set(enrollmentItems.filter(item => item.values.status !== "Unassigned").map(item => item.id));
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
      enrollmentApiFor(resource).get(workflowSourceSearch(query), departmentId, year),
      departmentApi.get(),
      resource === "timetable" ? teacherApi.get() : Promise.resolve([]),
      resource === "timetable" ? courseApi.get() : Promise.resolve([]),
      resource === "timetable" ? classroomApi.get() : Promise.resolve([]),
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
    items,
    load,
    query,
    ready,
    remove,
    setActionError,
    setEditing,
    setError,
    setQuery,
    teachers,
    year,
  };
}

function getCatalogCandidates(resource: SelectableEnrollmentResource, departmentId: string, year: string): Promise<EnrollmentItem[]> {
  if (resource === "students") return studentApi.get();
  void year;
  return timetableApi.get("", departmentId);
}
