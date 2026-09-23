"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { BrowserBackButton } from "@/components/browser-back-button";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { administrationApi } from "@/features/administration/administration-api";
import { defaultSettings } from "@/features/administration/administration-defaults";
import type { EnrollmentItem } from "@/features/enrollment/common/enrollment-types";
import { studentEnrollmentApi } from "@/features/enrollment/students/student-enrollment-api";
import { assessmentApi } from "./assessment-api";
import { assessmentScore, gradeLetter, statusLabel, statusTone } from "./assessment-calculation";
import type { GradeAssessment } from "./assessment-types";

export function StudentResultDetail({ studentId }: { studentId: string }) {
  const [rows, setRows] = useState<GradeAssessment[]>([]);
  const [enrollments, setEnrollments] = useState<EnrollmentItem[]>([]);
  const [gradeRules, setGradeRules] = useState<Record<string, string>>(defaultSettings["grade-rules"]);
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);
  const load = useCallback(() => Promise.all([assessmentApi.get(), studentEnrollmentApi.get(), administrationApi.get("grade-rules")]).then(([gradeRows, enrollmentRows, settings]) => {
    setRows(gradeRows.filter(item => item.values.studentId === studentId));
    setEnrollments(enrollmentRows);
    setGradeRules({ ...defaultSettings["grade-rules"], ...settings.values });
    setReady(true);
    setError(false);
  }).catch(() => setError(true)), [studentId]);
  useEffect(() => { void load(); }, [load]);

  const courses = useMemo(() => rows.toSorted((left, right) => left.values.course.localeCompare(right.values.course, undefined, { numeric: true, sensitivity: "base" })), [rows]);
  if (error) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;
  const first = courses[0];
  const enrollment = enrollments.find(item => item.id === studentId && item.values.periodState === "Current");
  const average = courses.length ? courses.reduce((total, item) => total + assessmentScore(item), 0) / courses.length : 0;

  return <div className="viewport-data-page assessment-viewport-page assessment-detail-page">
    <PageHeading eyebrow="Assessment · Student record" title={first?.values.student ?? "Student Result"} description="Complete inside view of every Teacher-assigned course grade for this Student." actions={<BrowserBackButton>Back to Student Results</BrowserBackButton>}/>
    {first ? <>
      <section className="panel assessment-detail-identity"><span className="assessment-student-icon"><Icon name="grade" size={22}/></span><div><span>Student result summary</span><h2>{first.values.student}</h2><p>{first.values.department} · {enrollment?.values.year ? `Year ${enrollment.values.year}` : "Year not assigned"} · {enrollment?.values.shift || "Shift not assigned"} · {first.values.academicYear} · {first.values.term}</p></div><aside><small>Overall grade</small><strong>{gradeLetter(average, gradeRules)}</strong><b>{average.toFixed(2)}/100</b></aside></section>
      <section className="assessment-course-detail-grid">{courses.map(item => <CourseResultDetail item={item} gradeRules={gradeRules} key={item.id}/>)}</section>
    </> : <section className="panel assessment-detail-empty"><Icon name="grade" size={28}/><h2>No current result</h2><p>No Teacher-assigned course grades are available for this Student in the current academic period.</p></section>}
  </div>;
}

function CourseResultDetail({ item, gradeRules }: { item: GradeAssessment; gradeRules: Record<string, string> }) {
  const value = item.values;
  const score = assessmentScore(item);
  return <article className="panel assessment-course-detail">
    <header><div><span>{value.gradeCode}</span><h2>{value.course}</h2><p>Assigned by {value.submittedByTeacher || "Institute record"}</p></div><span className={`assessment-course-state status-${statusTone(value.reviewStatus)}`}>{statusLabel(value.reviewStatus)}</span></header>
    <section className="assessment-detail-scores"><Score label="Assignment" value={value.assignmentScore} maximum={value.assignmentMaximum}/><Score label="Midterm" value={value.midtermScore} maximum={value.midtermMaximum}/><Score label="Final" value={value.finalExamScore} maximum={value.finalExamMaximum}/></section>
    <section className="assessment-detail-total"><span><small>Grade-only total</small><strong>{score.toFixed(2)} / 100</strong></span><b>{gradeLetter(score, gradeRules)}</b></section>
    <dl><div><dt>Teacher submission</dt><dd>{formatDateTime(value.submittedAtUtc)}</dd></div><div><dt>Administrator approval</dt><dd>{formatDateTime(value.reviewedAtUtc)}</dd></div><div><dt>Submission version</dt><dd>{value.submissionVersion || "1"}</dd></div><div><dt>Academic period</dt><dd>{value.academicYear} · {value.term}</dd></div></dl>
    {value.reviewNote && <footer><strong>Institutional review note</strong><p>{value.reviewNote}</p></footer>}
  </article>;
}

function Score({ label, value, maximum }: { label: string; value: string; maximum: string }) { return <div><small>{label}</small><strong>{value || "0"}<span>/{maximum}</span></strong></div>; }
function formatDateTime(value: string) { if (!value) return "Not recorded"; const date = new Date(value); return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat("en-GB", { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" }).format(date); }
