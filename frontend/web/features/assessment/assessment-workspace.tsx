"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { DataTable } from "@/components/data-table";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import type { EnrollmentItem } from "@/features/enrollment/common/enrollment-types";
import { studentEnrollmentApi } from "@/features/enrollment/students/student-enrollment-api";
import { assessmentApi } from "./assessment-api";
import { statusLabel, statusTone } from "./assessment-calculation";
import { groupCourseAssessments, type CourseAssessmentGroup } from "./assessment-groups";
import type { GradeAssessment } from "./assessment-types";

export function AssessmentWorkspace() {
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const suffix = departmentId ? `?departmentId=${encodeURIComponent(departmentId)}` : "";
  const [rows, setRows] = useState<GradeAssessment[]>([]);
  const [enrollments, setEnrollments] = useState<EnrollmentItem[]>([]);
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);
  const load = useCallback(() => Promise.all([
    assessmentApi.get(departmentId),
    studentEnrollmentApi.get("", departmentId),
  ]).then(([gradeRows, enrollmentRows]) => {
    setRows(gradeRows);
    setEnrollments(enrollmentRows);
    setReady(true);
    setError(false);
  }).catch(() => setError(true)), [departmentId]);
  useEffect(() => { void load(); }, [load]);

  const groups = useMemo(() => groupCourseAssessments(rows, enrollments)
    .toSorted((left, right) => dateValue(right.submittedAtUtc || right.reviewedAtUtc) - dateValue(left.submittedAtUtc || left.reviewedAtUtc)), [enrollments, rows]);
  const summary = useMemo(() => ({
    students: new Set(rows.map(item => item.values.studentId)).size,
    requests: groups.filter(item => ["SubmissionRequested", "ResubmitRequested"].includes(item.status)).length,
    review: groups.filter(item => ["Submitted", "Pending"].includes(item.status)).length,
    authorized: groups.filter(item => ["SubmissionAuthorized", "ResubmitAuthorized"].includes(item.status)).length,
    accepted: groups.filter(item => item.status === "Approved").length,
  }), [groups, rows]);

  if (error) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;

  const attention = [
    { label: "Authorization requests", count: summary.requests, detail: "Teacher course rosters waiting for Administrator authorization.", href: `/assessment/submission-approvals${suffix}` },
    { label: "Submitted rosters for review", count: summary.review, detail: "Complete course rosters waiting for acceptance or correction.", href: `/assessment/submission-approvals${suffix}` },
    { label: "Authorized rosters not submitted", count: summary.authorized, detail: "Teachers can now submit the complete class roster.", href: `/assessment/course-submissions${suffix}` },
  ];

  return <div className="viewport-data-page assessment-viewport-page assessment-overview-page">
    <PageHeading eyebrow="Institutional assessment control" title="Assessment Overview" description="Monitor course assessment progress, review whole-roster submissions, and open the exact register that needs attention."/>
    <div className="assessment-overview-scroll">
      <section className="assessment-overview-metrics" aria-label="Assessment activity">
        <OverviewMetric href={`/assessment/student-results${suffix}`} icon="users" label="Semester results" value={summary.students} detail="Attendance, course totals, and publication"/>
        <OverviewMetric href={`/assessment/course-submissions${suffix}`} icon="book" label="Course rosters" value={groups.length} detail="Latest course submission state"/>
        <OverviewMetric href={`/assessment/submission-approvals${suffix}`} icon="pulse" label="Approval queue" value={summary.requests + summary.review} detail="Authorization and final review" attention={summary.requests + summary.review > 0}/>
        <OverviewMetric href={`/assessment/course-submissions${suffix}`} icon="check" label="Accepted rosters" value={summary.accepted} detail="Administrator-approved courses"/>
      </section>

      <section className="assessment-overview-main">
        <article className="panel assessment-overview-flow">
          <header><div><span>Whole-course governance</span><h2>Assessment submission workflow</h2><p>Every action applies to one Teacher, one course, and the complete enrolled Student roster.</p></div></header>
          <div className="assessment-overview-flow-list">
            <WorkflowStep number="1" title="Teacher requests authorization" detail="The request covers the complete assigned course roster." href={`/assessment/course-submissions${suffix}`}/>
            <WorkflowStep number="2" title="Administrator authorizes submission" detail="Authorization is granted once for the entire course cohort." href={`/assessment/submission-approvals${suffix}`}/>
            <WorkflowStep number="3" title="Teacher submits all results" detail="One submission sends every Student grade in the roster." href={`/assessment/course-submissions${suffix}`}/>
            <WorkflowStep number="4" title="Administrator accepts or returns" detail="Accepted results become Student Results and permanent Assessment History." href={`/assessment/submission-approvals${suffix}`}/>
          </div>
        </article>

        <aside className="panel assessment-overview-attention">
          <header><div><span>Automatic checks</span><h2>Needs attention</h2></div><strong>{summary.requests + summary.review + summary.authorized}</strong></header>
          <div>{attention.map(item => <Link href={item.href} className={item.count ? "has-issue" : "is-ready"} key={item.label}>
            <span>{item.count || <Icon name="check" size={15}/>}</span>
            <div><strong>{item.label}</strong><small>{item.count ? item.detail : "No course rosters are waiting in this stage."}</small></div>
            <Icon name="arrow" size={14}/>
          </Link>)}</div>
        </aside>
      </section>

      <section className="panel assessment-overview-latest">
        <header><div><span>Current assessment activity</span><h2>Latest course submissions</h2><p>Newest whole-roster activity across the current assessment workspace.</p></div><Link className="button secondary" href={`/assessment/course-submissions${suffix}`}>View all courses <Icon name="arrow" size={14}/></Link></header>
        <DataTable className="horizontal-management-table assessment-overview-register" headerClassName="horizontal-management-head" rowSelector=":scope > .assessment-overview-row" columns={[{ key: "course", label: "Course", minimumWidth: 190 }, { key: "teacher", label: "Assigned Teacher", minimumWidth: 150 }, { key: "cohort", label: "Cohort", minimumWidth: 150 }, { key: "students", label: "Students", minimumWidth: 80, align: "right" }, { key: "status", label: "Latest Status", minimumWidth: 130, align: "center" }, { key: "date", label: "Latest Submission", minimumWidth: 140, align: "center" }]}>
          {groups.slice(0, 6).map(group => <LatestCourseRow group={group} suffix={suffix} key={group.key}/>)}
          {!groups.length && <div className="assessment-overview-empty"><Icon name="grade" size={22}/><strong>No assessment activity yet</strong><span>Teacher course-roster activity will appear here.</span></div>}
        </DataTable>
      </section>
    </div>
  </div>;
}

function OverviewMetric({ href, icon, label, value, detail, attention = false }: { href: string; icon: Parameters<typeof Icon>[0]["name"]; label: string; value: number; detail: string; attention?: boolean }) {
  return <Link className="panel assessment-overview-metric" href={href}>
    <span className={attention ? "attention" : "complete"}><Icon name={icon} size={17}/></span>
    <div><small>{label}</small><strong>{value.toLocaleString()}</strong><p>{detail}</p></div>
    <Icon name="arrow" size={14}/>
  </Link>;
}

function WorkflowStep({ number, title, detail, href }: { number: string; title: string; detail: string; href: string }) {
  return <Link href={href}><b>{number}</b><div><strong>{title}</strong><small>{detail}</small></div><Icon name="arrow" size={14}/></Link>;
}

function LatestCourseRow({ group, suffix }: { group: CourseAssessmentGroup; suffix: string }) {
  return <Link className="horizontal-management-row assessment-overview-row" href={`/assessment/course-submissions${suffix}`}>
    <span className="assessment-register-copy"><strong>{group.course}</strong><small>{group.department}</small></span>
    <span className="assessment-register-copy"><strong>{group.teacher}</strong><small>Assigned Teacher</small></span>
    <span className="assessment-register-copy"><strong>{group.year ? `Year ${group.year}` : "Year not assigned"}</strong><small>{group.shift || "Shift not assigned"} / {group.academicYear} / {group.term}</small></span>
    <strong>{group.grades.length}</strong>
    <span className={`assessment-course-state status-${statusTone(group.status)}`}>{statusLabel(group.status)}</span>
    <time>{formatDate(group.submittedAtUtc)}</time>
  </Link>;
}

function formatDate(value: string) {
  if (!value) return "Not submitted";
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat("en-GB", { day: "2-digit", month: "short", year: "numeric" }).format(date);
}

function dateValue(value: string) {
  const parsed = new Date(value).valueOf();
  return Number.isNaN(parsed) ? 0 : parsed;
}
