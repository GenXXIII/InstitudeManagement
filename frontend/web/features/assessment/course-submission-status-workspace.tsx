"use client";

import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { DataTableEmptyState, DataTableToolbar, PaginatedDataRegion } from "@/components/data-table";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import type { EnrollmentItem } from "@/features/enrollment/common/enrollment-types";
import { studentEnrollmentApi } from "@/features/enrollment/students/student-enrollment-api";
import { timetableEnrollmentApi } from "@/features/enrollment/timetable/timetable-enrollment-api";
import { workflowSourceSearch } from "@/lib/workflow-code";
import { assessmentApi } from "./assessment-api";
import { groupCourseAssessments, type CourseAssessmentGroup } from "./assessment-groups";
import { statusLabel, statusTone } from "./assessment-calculation";

type SubmissionState = "Pending" | "Submitted" | "Resubmitted";
type CourseSubmissionStatus = {
  key: string;
  courseCode: string;
  course: string;
  teacher: string;
  department: string;
  year: string;
  shift: string;
  cohort: string;
  academicYear: string;
  term: string;
  rosterCount: number;
  recordedCount: number;
  state: SubmissionState;
  workflow: string;
  workflowTone: string;
  version: number;
  submittedAtUtc: string;
};

export function CourseSubmissionStatusWorkspace() {
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [grades, setGrades] = useState<Awaited<ReturnType<typeof assessmentApi.get>>>([]);
  const [enrollments, setEnrollments] = useState<EnrollmentItem[]>([]);
  const [timetable, setTimetable] = useState<EnrollmentItem[]>([]);
  const [query, setQuery] = useState("");
  const [state, setState] = useState("All");
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);
  const load = useCallback(() => Promise.all([
    assessmentApi.get(departmentId),
    studentEnrollmentApi.get("", departmentId, year),
    timetableEnrollmentApi.get("", departmentId, year),
  ]).then(([gradeRows, studentRows, timetableRows]) => {
    setGrades(gradeRows);
    setEnrollments(studentRows);
    setTimetable(timetableRows);
    setReady(true);
    setError(false);
  }).catch(() => setError(true)), [departmentId, year]);
  useEffect(() => { void load(); }, [load]);

  const rows = useMemo(() => buildCourseStatuses(timetable, enrollments, groupCourseAssessments(grades, enrollments)), [enrollments, grades, timetable]);
  const visible = useMemo(() => {
    const text = workflowSourceSearch(query).toLowerCase();
    return rows.filter(item => (state === "All" || item.state === state) && (!text || [item.courseCode, item.course, item.teacher, item.department, item.year, item.shift, item.workflow].some(value => value.toLowerCase().includes(text))))
      .toSorted((left, right) => dateValue(right.submittedAtUtc) - dateValue(left.submittedAtUtc) || left.course.localeCompare(right.course, undefined, { numeric: true, sensitivity: "base" }));
  }, [query, rows, state]);

  if (error) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;

  return <div className="viewport-data-page assessment-viewport-page course-submission-page">
    <PageHeading eyebrow="Assessment · Current academic period" title="Course Submission Status" description="Latest institutional status for every current Teacher-assigned course roster. Pending, Submitted, and Resubmitted are derived from the complete course workflow."/>
    <DataTableToolbar query={query} onQueryChange={setQuery} searchPlaceholder="Search course, Teacher, department, or cohort..." searchAriaLabel="Search course submission status" resultLabel={`${visible.length} assigned courses`} className="record-toolbar panel assessment-toolbar" searchClassName="record-search management-search module-search-field">
      <select value={state} onChange={event => setState(event.target.value)} aria-label="Course submission status"><option>All</option><option>Pending</option><option>Submitted</option><option>Resubmitted</option></select>
    </DataTableToolbar>
    <PaginatedDataRegion items={visible} resetKey={`${query}-${state}`} className="assessment-paginated-region" empty={<DataTableEmptyState icon={<Icon name="book" size={28}/>} title="No assigned courses" description="Current Timetable Enrollment courses matching these filters will appear here."/>}>{pageItems => <section className="course-submission-grid" aria-label="Current course submission status">{pageItems.map(item => <CourseStatusCard item={item} key={item.key}/>)}</section>}</PaginatedDataRegion>
  </div>;
}

function CourseStatusCard({ item }: { item: CourseSubmissionStatus }) {
  return <article className="course-submission-card panel">
    <header><span className="assessment-student-icon"><Icon name="book" size={19}/></span><div><span className={`course-submission-state state-${item.state.toLowerCase()}`}><i/>{item.state}</span><h2>{item.course}</h2><p>{item.courseCode || "Course code not assigned"}</p></div><strong>v{item.version}</strong></header>
    <dl><div><dt>Assigned Teacher</dt><dd>{item.teacher}</dd></div><div><dt>Cohort</dt><dd>{item.cohort}</dd></div><div><dt>Department</dt><dd>{item.department}</dd></div><div><dt>Roster coverage</dt><dd>{item.recordedCount}/{item.rosterCount} Students</dd></div></dl>
    <footer><div><span>Latest Teacher submission</span><strong>{formatDateTime(item.submittedAtUtc)}</strong></div><span className={`assessment-course-state status-${item.workflowTone}`}>{item.workflow}</span></footer>
  </article>;
}

function buildCourseStatuses(timetable: EnrollmentItem[], enrollments: EnrollmentItem[], gradeGroups: CourseAssessmentGroup[]): CourseSubmissionStatus[] {
  const currentStudents = enrollments.filter(item => item.values.periodState === "Current" && item.values.status === "Active");
  const assignments = new Map<string, Map<string, EnrollmentItem>>();
  for (const item of timetable.filter(item => item.values.periodState === "Current" && item.values.status === "Active")) {
    const key = courseKey(item.values.courseId, item.values.departmentId, item.values.academicYear, item.values.semester);
    const courseAssignments = assignments.get(key) ?? new Map<string, EnrollmentItem>();
    courseAssignments.set(assignmentKey(item.values.teacherId, item.values.courseId, item.values.departmentId, item.values.yearLevel, item.values.shift, item.values.academicYear, item.values.semester), item);
    assignments.set(key, courseAssignments);
  }
  const groupsByCourse = new Map<string, CourseAssessmentGroup[]>();
  for (const group of gradeGroups) {
    const key = courseKey(group.courseId, group.departmentId, group.academicYear, group.term);
    groupsByCourse.set(key, [...(groupsByCourse.get(key) ?? []), group]);
  }
  return [...assignments.entries()].map(([key, courseAssignments]) => {
    const enrolledAssignments = [...courseAssignments.values()];
    const assignment = enrolledAssignments[0];
    const value = assignment.values;
    const roster = currentStudents.filter(student => enrolledAssignments.some(item => student.values.departmentId === item.values.departmentId && student.values.year === item.values.yearLevel && student.values.shift === item.values.shift && student.values.academicYear === item.values.academicYear && student.values.semester === item.values.semester));
    const groups = groupsByCourse.get(key) ?? [];
    const group = groups.toSorted((left, right) => dateValue(right.submittedAtUtc || right.reviewedAtUtc) - dateValue(left.submittedAtUtc || left.reviewedAtUtc))[0];
    const recordedStudents = new Set(groups.flatMap(item => item.grades.map(grade => grade.values.studentId)));
    const rosterStudents = new Set(roster.map(student => student.id));
    const rosterCount = rosterStudents.size;
    const recordedCount = [...recordedStudents].filter(studentId => rosterStudents.has(studentId)).length;
    const complete = Boolean(group) && recordedCount === rosterCount && rosterCount > 0;
    const version = Math.max(1, ...groups.map(item => item.version));
    const state: SubmissionState = !complete ? "Pending" : version > 1 && ["Submitted", "Pending", "Approved"].includes(group!.status) ? "Resubmitted" : group!.status === "Approved" ? "Submitted" : "Pending";
    const teachers = [...new Set(enrolledAssignments.map(item => item.values.teacher).filter(Boolean))];
    const cohorts = [...new Set(enrolledAssignments.map(item => `${item.values.yearLevel ? `Year ${item.values.yearLevel}` : "Year not assigned"} / ${item.values.shift || "Shift not assigned"}`))];
    return {
      key,
      courseCode: value.courseCode,
      course: value.course,
      teacher: teachers.join(", ") || "Teacher not assigned",
      department: value.department,
      year: value.yearLevel,
      shift: value.shift,
      cohort: cohorts.join(", "),
      academicYear: value.academicYear,
      term: value.semester,
      rosterCount,
      recordedCount,
      state,
      workflow: group ? statusLabel(group.status) : "Awaiting Teacher submission",
      workflowTone: group ? statusTone(group.status) : "pending",
      version,
      submittedAtUtc: group?.submittedAtUtc ?? "",
    };
  });
}

function assignmentKey(teacherId: string, courseId: string, departmentId: string, year: string, shift: string, academicYear: string, term: string) { return [teacherId, courseId, departmentId, year, shift, academicYear, term].join("|"); }
function courseKey(courseId: string, departmentId: string, academicYear: string, term: string) { return [courseId, departmentId, academicYear, term].join("|"); }
function formatDateTime(value: string) { if (!value) return "Not submitted"; const date = new Date(value); return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat("en-GB", { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" }).format(date); }
function dateValue(value: string) { const parsed = new Date(value).valueOf(); return Number.isNaN(parsed) ? 0 : parsed; }
