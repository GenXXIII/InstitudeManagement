"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { DataTable, DataTableEmptyState, DataTableToolbar, PaginatedDataRegion } from "@/components/data-table";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { administrationApi } from "@/features/administration/administration-api";
import { defaultSettings } from "@/features/administration/administration-defaults";
import type { EnrollmentItem } from "@/features/enrollment/common/enrollment-types";
import { studentEnrollmentApi } from "@/features/enrollment/students/student-enrollment-api";
import { workflowSourceSearch } from "@/lib/workflow-code";
import { assessmentApi } from "./assessment-api";
import { assessmentScore, gradeLetter, statusLabel } from "./assessment-calculation";
import type { GradeAssessment } from "./assessment-types";

export type AssessmentStudent = {
  key: string;
  studentId: string;
  student: string;
  department: string;
  year: string;
  shift: string;
  academicYear: string;
  term: string;
  courses: GradeAssessment[];
  latestSubmission: string;
};

export function StudentResultsWorkspace() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const [rows, setRows] = useState<GradeAssessment[]>([]);
  const [enrollments, setEnrollments] = useState<EnrollmentItem[]>([]);
  const [gradeRules, setGradeRules] = useState<Record<string, string>>(defaultSettings["grade-rules"]);
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [status, setStatus] = useState("All");
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);
  const load = useCallback(() => Promise.all([
    assessmentApi.get(departmentId),
    studentEnrollmentApi.get("", departmentId),
    administrationApi.get("grade-rules"),
  ]).then(([gradeRows, enrollmentRows, settings]) => {
    setRows(gradeRows);
    setEnrollments(enrollmentRows);
    setGradeRules({ ...defaultSettings["grade-rules"], ...settings.values });
    setReady(true);
    setError(false);
  }).catch(() => setError(true)), [departmentId]);
  useEffect(() => { void load(); }, [load]);

  const visible = useMemo(() => {
    const currentEnrollments = new Map(enrollments.filter(item => item.values.periodState === "Current").map(item => [item.id, item]));
    const text = workflowSourceSearch(query).toLowerCase();
    return groupAssessments(rows, currentEnrollments).filter(student => {
      const matchesStatus = status === "All" || student.courses.some(item => item.values.reviewStatus === status);
      const searchable = [student.student, student.department, student.year, student.shift, student.academicYear, student.term, ...student.courses.flatMap(item => [item.values.gradeCode, item.values.course, item.values.submittedByTeacher, statusLabel(item.values.reviewStatus)])];
      return matchesStatus && (!text || searchable.some(value => value.toLowerCase().includes(text)));
    }).toSorted((left, right) => left.student.localeCompare(right.student) || right.latestSubmission.localeCompare(left.latestSubmission));
  }, [enrollments, query, rows, status]);

  if (error) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;

  return <div className="viewport-data-page assessment-viewport-page">
    <PageHeading eyebrow="Assessment · Grade records" title="Student Results" description="Select a Student summary to open the complete inside view of every Teacher-assigned course grade."/>
    <DataTableToolbar query={query} onQueryChange={setQuery} searchPlaceholder="Search Student, Teacher, or course..." searchAriaLabel="Search Student results" resultLabel={`${visible.length} Students`} className="record-toolbar panel assessment-toolbar" searchClassName="record-search management-search module-search-field">
      <select value={status} onChange={event => setStatus(event.target.value)} aria-label="Grade workflow status"><option>All</option><option value="SubmissionRequested">Pending approval</option><option value="SubmissionAuthorized">Submission authorized</option><option value="Submitted">Pending review</option><option value="ResubmitRequested">Resubmission requested</option><option value="Approved">Accepted</option><option value="Rejected">Rejected</option></select>
    </DataTableToolbar>
    <PaginatedDataRegion items={visible} resetKey={`${query}-${status}`} className="assessment-paginated-region" empty={<DataTableEmptyState icon={<Icon name="grade" size={28}/>} title="No Student results" description="Teacher-assigned course grades matching these filters will appear here."/>}>{pageItems => <DataTable as="section" className="panel horizontal-management-table assessment-student-register" headerClassName="horizontal-management-head assessment-student-register-head" rowSelector=":scope > .assessment-student-register-row" columns={[{ key: "student", label: "Student", minimumWidth: 190 }, { key: "cohort", label: "Cohort", minimumWidth: 155 }, { key: "courses", label: "Assigned Courses", minimumWidth: 125, align: "center" }, { key: "accepted", label: "Accepted", minimumWidth: 100, align: "center" }, { key: "grade", label: "Overall Grade", minimumWidth: 110, align: "center" }, { key: "submission", label: "Latest Submission", minimumWidth: 145 }, { key: "action", label: "Action", minimumWidth: 90, align: "center" }]} ariaLabel="Student assessment results">{pageItems.map(student => <StudentResultRow student={student} gradeRules={gradeRules} onOpen={() => router.push(`/assessment/student-results/${encodeURIComponent(student.studentId)}${departmentId ? `?departmentId=${encodeURIComponent(departmentId)}` : ""}`)} key={student.key}/>)}</DataTable>}</PaginatedDataRegion>
  </div>;
}

function StudentResultRow({ student, gradeRules, onOpen }: { student: AssessmentStudent; gradeRules: Record<string, string>; onOpen: () => void }) {
  const accepted = student.courses.filter(item => item.values.reviewStatus === "Approved").length;
  const action = student.courses.filter(item => ["SubmissionRequested", "Submitted", "Pending", "ResubmitRequested"].includes(item.values.reviewStatus)).length;
  const average = student.courses.length ? Math.round(student.courses.reduce((total, item) => total + assessmentScore(item), 0) / student.courses.length * 100) / 100 : 0;
  const state = action ? { label: "Workflow active", tone: "pending" } : accepted === student.courses.length ? { label: "All accepted", tone: "approved" } : { label: "Waiting", tone: "authorized" };
  return <article className="horizontal-management-row assessment-student-register-row" role="link" tabIndex={0} onClick={onOpen} onKeyDown={event => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); onOpen(); } }}>
    <span className="assessment-register-person"><span className="assessment-student-icon"><Icon name="grade" size={17}/></span><span><strong>{student.student}</strong><small>{student.department}</small></span></span>
    <span className="assessment-register-copy"><strong>{student.year ? `Year ${student.year}` : "Year not assigned"}</strong><small>{student.shift || "Shift not assigned"}</small></span>
    <span className="assessment-register-center"><strong>{student.courses.length}</strong><small>{student.academicYear} · {student.term}</small></span>
    <span className="assessment-register-center"><strong>{accepted}/{student.courses.length}</strong><small className={`assessment-summary-state status-${state.tone}`}><i/>{state.label}</small></span>
    <span className="assessment-register-grade"><strong>{gradeLetter(average, gradeRules)}</strong><small>{average.toFixed(2)}/100</small></span>
    <span className="assessment-register-copy"><strong>{formatDate(student.latestSubmission)}</strong><small>{student.latestSubmission ? "Teacher submitted" : "Not submitted"}</small></span>
    <span className="assessment-register-action"><button type="button" className="button secondary" onClick={event => { event.stopPropagation(); onOpen(); }}>Open</button></span>
  </article>;
}

export function groupAssessments(rows: GradeAssessment[], enrollmentByStudent: Map<string, EnrollmentItem>): AssessmentStudent[] {
  const groups = new Map<string, AssessmentStudent>();
  for (const item of rows) {
    const value = item.values;
    const key = `${value.studentId}-${value.academicYear}-${value.term}`;
    const enrollment = enrollmentByStudent.get(value.studentId);
    const existing = groups.get(key) ?? { key, studentId: value.studentId, student: value.student, department: value.department, year: enrollment?.values.year ?? "", shift: enrollment?.values.shift ?? "", academicYear: value.academicYear, term: value.term, courses: [], latestSubmission: "" };
    existing.courses.push(item);
    if (existing.latestSubmission.localeCompare(value.submittedAtUtc) < 0) existing.latestSubmission = value.submittedAtUtc;
    groups.set(key, existing);
  }
  return [...groups.values()].map(student => ({ ...student, courses: student.courses.toSorted((left, right) => left.values.course.localeCompare(right.values.course, undefined, { numeric: true, sensitivity: "base" })) }));
}

function formatDate(value: string) { if (!value) return "Pending"; const date = new Date(value); return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat("en-GB", { day: "2-digit", month: "short", year: "numeric" }).format(date); }
