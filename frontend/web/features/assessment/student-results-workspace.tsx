"use client";

import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { DataTable, DataTableEmptyState, DataTableToolbar, PaginatedDataRegion, type DataTableColumn } from "@/components/data-table";
import { Icon } from "@/components/icon";
import { ManagementDataCell } from "@/components/management-data-cell";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { administrationApi } from "@/features/administration/administration-api";
import { defaultSettings } from "@/features/administration/administration-defaults";
import type { EnrollmentItem } from "@/features/enrollment/common/enrollment-types";
import { studentEnrollmentApi } from "@/features/enrollment/students/student-enrollment-api";
import { timetableEnrollmentApi } from "@/features/enrollment/timetable/timetable-enrollment-api";
import { workflowSourceSearch } from "@/lib/workflow-code";
import { assessmentApi } from "./assessment-api";
import { assessmentScore, gradeLetter, statusLabel } from "./assessment-calculation";
import type { GradeAssessment } from "./assessment-types";

export type AssessmentStudent = {
  key: string;
  studentId: string;
  student: string;
  departmentId: string;
  department: string;
  year: string;
  shift: string;
  academicYear: string;
  term: string;
  courses: GradeAssessment[];
  courseResults: AssessmentCourseResult[];
  resultCode: string;
  latestSubmission: string;
  finalizedAtUtc: string;
};

type AssessmentCourseResult = { courseId: string; courseCode: string; course: string; result: GradeAssessment | null };

export function StudentResultsWorkspace() {
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const [rows, setRows] = useState<GradeAssessment[]>([]);
  const [enrollments, setEnrollments] = useState<EnrollmentItem[]>([]);
  const [timetable, setTimetable] = useState<EnrollmentItem[]>([]);
  const [gradeRules, setGradeRules] = useState<Record<string, string>>(defaultSettings["grade-rules"]);
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [status, setStatus] = useState("All");
  const [confirming, setConfirming] = useState(false);
  const [notice, setNotice] = useState<{ message: string; error: boolean }>();
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);
  const load = useCallback(() => Promise.all([
    assessmentApi.get(departmentId),
    studentEnrollmentApi.get("", departmentId),
    timetableEnrollmentApi.get("", departmentId),
    administrationApi.get("grade-rules"),
  ]).then(([gradeRows, enrollmentRows, timetableRows, settings]) => {
    setRows(gradeRows);
    setEnrollments(enrollmentRows);
    setTimetable(timetableRows);
    setGradeRules({ ...defaultSettings["grade-rules"], ...settings.values });
    setReady(true);
    setError(false);
  }).catch(() => setError(true)), [departmentId]);
  useEffect(() => { void load(); }, [load]);

  const studentResults = useMemo(() => {
    const enrollmentByPeriod = new Map(enrollments.map(item => [enrollmentKey(item.id, item.values.academicYear, item.values.semester), item]));
    const expectedCourseCount = configuredCourseCount(gradeRules);
    return groupAssessments(rows, enrollmentByPeriod, timetable, expectedCourseCount);
  }, [enrollments, gradeRules, rows, timetable]);
  const visible = useMemo(() => {
    const text = workflowSourceSearch(query).toLowerCase();
    return studentResults.filter(student => {
      const matchesStatus = status === "All" || resultStatus(student) === status;
      const searchable = [student.resultCode, student.student, student.department, student.year, student.shift, student.academicYear, student.term, ...student.courseResults.flatMap(item => [item.courseCode, item.course]), ...student.courses.flatMap(item => [item.values.gradeCode, item.values.submittedByTeacher, statusLabel(item.values.reviewStatus)])];
      return matchesStatus && (!text || searchable.some(value => value.toLowerCase().includes(text)));
    }).toSorted((left, right) => Number(left.year || 999) - Number(right.year || 999) || semesterNumber(left.term) - semesterNumber(right.term) || shiftNumber(left.shift) - shiftNumber(right.shift) || left.student.localeCompare(right.student) || dateValue(right.latestSubmission) - dateValue(left.latestSubmission));
  }, [query, status, studentResults]);
  const readyCount = studentResults.filter(student => resultStatus(student) === "Ready").length;

  async function confirmFinalGrades() {
    if (!window.confirm(`Confirm ${readyCount} complete Student result${readyCount === 1 ? "" : "s"}? Every course grade will become read-only and move to Semester Result.`)) return;
    setConfirming(true);
    setNotice(undefined);
    try {
      const result = await assessmentApi.confirmFinalGrades(departmentId);
      setNotice({ message: result.confirmed > 0 ? `${result.confirmed} final Student result${result.confirmed === 1 ? "" : "s"} confirmed and moved to Semester Result.` : "No complete Student results are ready for confirmation.", error: false });
      await load();
    } catch (reason) {
      setNotice({ message: reason instanceof Error ? reason.message : "Could not confirm final Student results.", error: true });
    } finally {
      setConfirming(false);
    }
  }

  if (error) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;

  return <div className="viewport-data-page assessment-viewport-page">
    <PageHeading eyebrow="Assessment · Grade records" title="Student Results" description="Latest Teacher-submitted course results. Complete results can be confirmed once, become read-only, and then move to Semester Result." actions={<button type="button" className="button primary" disabled={readyCount === 0 || confirming} onClick={() => void confirmFinalGrades()}>{confirming ? "Confirming…" : `Confirm final grades (${readyCount})`}</button>}/>
    {notice && <section className={`result-action-error${notice.error ? "" : " is-success"}`} role={notice.error ? "alert" : "status"}>{notice.message}</section>}
    <DataTableToolbar query={query} onQueryChange={setQuery} searchPlaceholder="Search result code, Student, Teacher, or course..." searchAriaLabel="Search Student results" resultLabel={`${visible.length} Students`} className="record-toolbar panel assessment-toolbar" searchClassName="record-search management-search module-search-field">
      <select value={status} onChange={event => setStatus(event.target.value)} aria-label="Final grade status"><option>All</option><option>Draft</option><option>Ready</option><option>Confirmed</option></select>
    </DataTableToolbar>
    <PaginatedDataRegion items={visible} resetKey={`${query}-${status}`} className="assessment-paginated-region" empty={<DataTableEmptyState icon={<Icon name="grade" size={28}/>} title="No Student results" description="Latest Teacher-submitted course grades matching these filters will appear here."/>}>{pageItems => <DataTable as="section" className="panel horizontal-management-table semester-result-table student-latest-result-table" headerClassName="horizontal-management-head semester-result-head student-latest-result-head" rowSelector=":scope > .student-latest-result-row" columns={studentResultColumns} ariaLabel="Student assessment results">{pageItems.map(student => <StudentResultRow student={student} gradeRules={gradeRules} key={student.key}/>)}</DataTable>}</PaginatedDataRegion>
  </div>;
}

function StudentResultRow({ student, gradeRules }: { student: AssessmentStudent; gradeRules: Record<string, string> }) {
  const courseCount = student.courseResults.length;
  const accepted = student.courseResults.filter(item => item.result?.values.reviewStatus === "Approved").length;
  const gradeTotal = Math.round(student.courseResults.reduce((total, item) => total + (item.result ? assessmentScore(item.result) : 0), 0) * 100) / 100;
  const average = courseCount ? Math.round(gradeTotal / courseCount * 100) / 100 : 0;
  const overallGrade = gradeLetter(average, gradeRules);
  const publicationStatus = student.finalizedAtUtc ? "Confirmed" : courseCount > 0 && accepted === courseCount ? "Ready" : "Draft";
  return <article className="horizontal-management-row semester-result-row student-latest-result-row">
    <Cell label="Resultcode" className="horizontal-detail result-record-code"><strong className="management-code-value">{student.resultCode}</strong></Cell>
    <Cell label="Name"><strong>{student.student}</strong></Cell>
    <Cell label="Department"><strong>{student.department}</strong></Cell>
    <Cell label="Year"><strong>{student.year ? `Year ${student.year}` : "—"}</strong></Cell>
    <Cell label="Shift"><strong>{student.shift || "—"}</strong></Cell>
    <Cell label="Courses" className="result-courses-cell"><CourseCards student={student} gradeRules={gradeRules}/></Cell>
    <Cell label="Total" className="result-total-cell"><div className={`student-result-total-card grade-${overallGrade.toLowerCase()}`} data-preserve-table-font=""><strong>{formatScore(gradeTotal)}</strong><b>{overallGrade}</b><small>{courseCount} courses · Average {formatScore(average)}/100</small></div></Cell>
    <Cell label="Status" className="result-status-cell"><span className={`table-status result-publication-state state-${publicationStatus.toLowerCase()}`}>{publicationStatus}</span></Cell>
  </article>;
}

const studentResultColumns: DataTableColumn[] = [
  { key: "result-code", label: "Resultcode", align: "center", minimumWidth: 105 },
  { key: "name", label: "Name", minimumWidth: 115 },
  { key: "department", label: "Department", minimumWidth: 125 },
  { key: "year", label: "Year", align: "center", minimumWidth: 55 },
  { key: "shift", label: "Shift", align: "center", minimumWidth: 65 },
  { key: "courses", label: "Courses", minimumWidth: 480 },
  { key: "total", label: "Total", align: "center", minimumWidth: 115 },
  { key: "status", label: "Status", align: "center", minimumWidth: 85 },
];

function CourseCards({ student, gradeRules }: { student: AssessmentStudent; gradeRules: Record<string, string> }) {
  return <div className="result-course-cards">{student.courseResults.map(item => {
    const score = item.result ? assessmentScore(item.result) : null;
    const grade = score === null ? "Draft" : gradeLetter(score, gradeRules);
    return <span className={`student-result-course-card grade-${grade.toLowerCase()}`} data-preserve-table-font="" key={item.courseId}><small>{item.courseCode || item.result?.values.gradeCode || "Not assigned"}</small><strong>{item.course}</strong><b>{score === null ? "Draft" : `${formatScore(score)}/${grade}`}</b></span>;
  })}</div>;
}

function Cell({ label, children, className = "horizontal-detail" }: { label: string; children: React.ReactNode; className?: string }) {
  return <ManagementDataCell label={label} className={className}>{children}</ManagementDataCell>;
}

export function groupAssessments(rows: GradeAssessment[], enrollmentByStudent: Map<string, EnrollmentItem>, timetable: EnrollmentItem[] = [], expectedCourseCount = 5): AssessmentStudent[] {
  const latestCourses = new Map<string, GradeAssessment>();
  for (const item of rows.filter(hasTeacherSubmission)) {
    const value = item.values;
    const enrollment = enrollmentByStudent.get(enrollmentKey(value.studentId, value.academicYear, value.term));
    const key = [value.studentId, value.courseId, enrollment?.values.year ?? "", enrollment?.values.shift ?? "", value.academicYear, value.term].join("|");
    const current = latestCourses.get(key);
    if (!current || compareTeacherSubmissions(item, current) > 0) latestCourses.set(key, item);
  }
  const groups = new Map<string, AssessmentStudent>();
  for (const item of latestCourses.values()) {
    const value = item.values;
    const enrollment = enrollmentByStudent.get(enrollmentKey(value.studentId, value.academicYear, value.term));
    const year = enrollment?.values.year ?? "";
    const shift = enrollment?.values.shift ?? "";
    const key = [value.studentId, year, shift, value.academicYear, value.term].join("|");
    const existing = groups.get(key) ?? { key, studentId: value.studentId, student: value.student, departmentId: value.departmentId, department: value.department, year, shift, academicYear: value.academicYear, term: value.term, courses: [], courseResults: [], resultCode: value.gradeCode, latestSubmission: "", finalizedAtUtc: "" };
    existing.courses.push(item);
    if (dateValue(value.submittedAtUtc) > dateValue(existing.latestSubmission)) {
      existing.latestSubmission = value.submittedAtUtc;
      existing.resultCode = value.gradeCode;
    }
    groups.set(key, existing);
  }
  return [...groups.values()].map(student => {
    const courses = student.courses.toSorted((left, right) => left.values.course.localeCompare(right.values.course, undefined, { numeric: true, sensitivity: "base" }));
    const submittedByCourse = new Map(courses.map(item => [item.values.courseId, item]));
    const assignedCourses = cohortCourses(timetable, student);
    const assignedCourseIds = new Set(assignedCourses.map(item => item.courseId));
    const submittedOutsideRoster = courses.filter(item => !assignedCourseIds.has(item.values.courseId)).map(item => ({ courseId: item.values.courseId, courseCode: "", course: item.values.course, result: item }));
    const knownCourseResults = assignedCourses.length
      ? [...assignedCourses.map(course => ({ ...course, result: submittedByCourse.get(course.courseId) ?? null })), ...submittedOutsideRoster]
      : courses.map(item => ({ courseId: item.values.courseId, courseCode: "", course: item.values.course, result: item }));
    const courseResults = Array.from({ length: Math.max(expectedCourseCount, knownCourseResults.length) }, (_, index) => knownCourseResults[index] ?? { courseId: `draft-${student.key}-${index}`, courseCode: "", course: `Course ${index + 1}`, result: null });
    const finalizedAtUtc = courseResults.length > 0 && courseResults.every(item => item.result?.values.finalizedAtUtc)
      ? courseResults.map(item => item.result?.values.finalizedAtUtc ?? "").toSorted().at(-1) ?? ""
      : "";
    return { ...student, courses, courseResults, finalizedAtUtc };
  });
}

function cohortCourses(timetable: EnrollmentItem[], student: AssessmentStudent): Omit<AssessmentCourseResult, "result">[] {
  const courses = new Map<string, Omit<AssessmentCourseResult, "result">>();
  for (const item of timetable.filter(item => item.values.periodState === "Current" && item.values.status === "Active")) {
    const value = item.values;
    if (value.departmentId !== student.departmentId || value.yearLevel !== student.year || value.shift !== student.shift || value.academicYear !== student.academicYear || value.semester !== student.term) continue;
    const courseId = value.courseId || [value.courseCode, value.course].join("|");
    if (courseId && !courses.has(courseId)) courses.set(courseId, { courseId, courseCode: value.courseCode, course: value.course || "Course" });
  }
  return [...courses.values()].toSorted((left, right) => left.course.localeCompare(right.course, undefined, { numeric: true, sensitivity: "base" }));
}

function hasTeacherSubmission(item: GradeAssessment) { return Boolean(item.values.submittedByTeacherId && item.values.submittedAtUtc) && !["SubmissionRequested", "SubmissionAuthorized"].includes(item.values.reviewStatus); }
function compareTeacherSubmissions(left: GradeAssessment, right: GradeAssessment) { return dateValue(left.values.submittedAtUtc) - dateValue(right.values.submittedAtUtc) || Number(left.values.submissionVersion || 1) - Number(right.values.submissionVersion || 1) || dateValue(left.values.createAt) - dateValue(right.values.createAt); }
function dateValue(value: string) { const parsed = new Date(value).valueOf(); return Number.isNaN(parsed) ? 0 : parsed; }
function formatScore(value: number) { return Number.isInteger(value) ? value.toFixed(0) : value.toFixed(2).replace(/0+$/, "").replace(/\.$/, ""); }
function configuredCourseCount(settings: Record<string, string>) { const count = Number(settings.expectedCourseCount); return Number.isInteger(count) && count > 0 ? count : 5; }
function resultStatus(student: AssessmentStudent) { return student.finalizedAtUtc ? "Confirmed" : student.courseResults.length > 0 && student.courseResults.every(item => item.result?.values.reviewStatus === "Approved") ? "Ready" : "Draft"; }
function enrollmentKey(studentId: string, academicYear: string, term: string) { return `${studentId}|${academicYear}|${term}`; }
function semesterNumber(value: string) { return Number(value.match(/\d+/)?.[0] ?? 0); }
function shiftNumber(value: string) { const order = ["morning", "afternoon", "evening", "weekend"]; const index = order.indexOf(value.toLowerCase()); return index < 0 ? order.length : index; }
