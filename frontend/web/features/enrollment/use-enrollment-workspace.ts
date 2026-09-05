"use client";

import { useCallback, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { useInstituteSettings } from "@/features/administration/institute-settings-context";
import { classroomApi } from "@/features/management/classrooms/classroom-api";
import { courseApi } from "@/features/management/courses/course-api";
import { departmentApi } from "@/features/management/departments/department-api";
import { emptyReferences } from "@/features/management/management-config";
import type { References } from "@/features/management/management-types";
import { studentApi } from "@/features/management/students/student-api";
import { teacherApi } from "@/features/management/teachers/teacher-api";
import { timetableApi } from "@/features/timetable/timetable-api";
import type { ClassroomItem } from "@/features/management/classrooms/classroom-types";
import type { CourseItem } from "@/features/management/courses/course-types";
import type { DepartmentItem } from "@/features/management/departments/department-types";
import type { TeacherItem } from "@/features/management/teachers/teacher-types";
import { workflowSourceSearch } from "@/lib/workflow-code";
import type { EnrollmentItem, EnrollmentResource } from "./common/enrollment-types";
import { enrollmentApiFor } from "./enrollment-apis";
import { teacherAssignmentApi } from "./teachers/teacher-assignment-api";
import { timetableEnrollmentApi } from "./timetable/timetable-enrollment-api";
import {
  enrollmentSubject,
  isSelectableEnrollment,
  type SelectableEnrollmentResource,
} from "./enrollment-workspace-model";

export function useEnrollmentWorkspace(resource: EnrollmentResource) {
  const { settings } = useInstituteSettings();
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [items, setItems] = useState<EnrollmentItem[]>([]);
  const [candidates, setCandidates] = useState<EnrollmentItem[]>([]);
  const [teachers, setTeachers] = useState<EnrollmentItem[]>([]);
  const [studentSchedules, setStudentSchedules] = useState<EnrollmentItem[]>([]);
  const [departments, setDepartments] = useState<DepartmentItem[]>([]);
  const [timetableReferences, setTimetableReferences] = useState<References>(emptyReferences);
  const [editing, setEditing] = useState<EnrollmentItem | null | undefined>();
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);
  const [actionError, setActionError] = useState("");

  const load = useCallback(() => {
    const candidateRequest: Promise<EnrollmentItem[]> = isSelectableEnrollment(resource)
      ? Promise.all([getCatalogCandidates(resource, departmentId, year), enrollmentApiFor(resource).get()]).then(([catalogItems, enrollmentItems]) => {
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
      enrollmentApiFor(resource).get(workflowSourceSearch(query), departmentId, year),
      departmentApi.get(),
      resource === "courses" ? teacherAssignmentApi.get("", settings.departments.allowCrossDepartmentTeaching === "true" ? "" : departmentId) : Promise.resolve([]),
      candidateRequest,
      resource === "student-assignments" ? timetableEnrollmentApi.get("", departmentId, year) : Promise.resolve([]),
      resource === "timetable"
        ? Promise.all([teacherApi.get(), courseApi.get(), classroomApi.get()])
        : Promise.resolve([[], [], []] as [TeacherItem[], CourseItem[], ClassroomItem[]]),
    ]).then(([rows, departmentRows, teacherRows, candidateRows, scheduleRows, [managementTeachers, managementCourses, managementClassrooms]]) => {
      setItems(rows);
      setDepartments(departmentRows);
      setTeachers(teacherRows);
      setCandidates(candidateRows);
      setStudentSchedules(scheduleRows);
      setTimetableReferences({ ...emptyReferences, departments: departmentRows, teachers: managementTeachers, courses: managementCourses, classrooms: managementClassrooms });
      setReady(true);
      setError(false);
    }).catch(() => setError(true));
  }, [departmentId, query, resource, settings.departments.allowCrossDepartmentTeaching, year]);

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

  function saveTimetable(id: string, values: Record<string, string>) {
    return timetableEnrollmentApi.update(id, values);
  }

  return {
    actionError,
    candidates,
    departmentId,
    departments,
    editing,
    error,
    items,
    load,
    query,
    ready,
    remove,
    saveTimetable,
    setActionError,
    setEditing,
    setError,
    setQuery,
    studentSchedules,
    teachers,
    timetableReferences,
    year,
  };
}

function getCatalogCandidates(resource: SelectableEnrollmentResource, departmentId: string, year: string): Promise<EnrollmentItem[]> {
  if (resource === "students") return studentApi.get();
  return timetableApi.get("", departmentId).then(items => items.filter(item => !year || item.values.yearLevel === year));
}
