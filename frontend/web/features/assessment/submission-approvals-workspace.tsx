"use client";

import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { DataTable, DataTableEmptyState, DataTableToolbar, PaginatedDataRegion } from "@/components/data-table";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import type { EnrollmentItem } from "@/features/enrollment/common/enrollment-types";
import { studentEnrollmentApi } from "@/features/enrollment/students/student-enrollment-api";
import { workflowSourceSearch } from "@/lib/workflow-code";
import { assessmentApi } from "./assessment-api";
import { groupCourseAssessments, type CourseAssessmentGroup } from "./assessment-groups";
import { statusLabel, statusTone } from "./assessment-calculation";

const columns = [
  { key: "code", label: "Code", minimumWidth: 125, align: "center" as const },
  { key: "teacher", label: "Teacher Name", minimumWidth: 175 },
  { key: "course", label: "Course", minimumWidth: 190 },
  { key: "department", label: "Department", minimumWidth: 160 },
  { key: "year", label: "Year", minimumWidth: 120, align: "center" as const },
  { key: "action", label: "Action", minimumWidth: 190, align: "center" as const },
];

export function SubmissionApprovalsWorkspace() {
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const [rows, setRows] = useState<Awaited<ReturnType<typeof assessmentApi.get>>>([]);
  const [enrollments, setEnrollments] = useState<EnrollmentItem[]>([]);
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("Action");
  const [selectedId, setSelectedId] = useState("");
  const [note, setNote] = useState("");
  const [working, setWorking] = useState(false);
  const [actionError, setActionError] = useState("");
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

  const groups = useMemo(() => groupCourseAssessments(rows, enrollments), [enrollments, rows]);
  const visible = useMemo(() => {
    const text = workflowSourceSearch(query).toLowerCase();
    return groups.filter(group => {
      const actionStatus = ["SubmissionRequested", "ResubmitRequested", "Submitted", "Pending"].includes(group.status);
      const matchesStatus = status === "All" || status === "Action" && actionStatus || group.status === status;
      const searchable = [group.teacher, group.course, group.department, group.year, group.shift, group.academicYear, group.term, statusLabel(group.status), ...group.grades.map(item => item.values.student)];
      return matchesStatus && (!text || searchable.some(value => value.toLowerCase().includes(text)));
    }).toSorted((left, right) => dateValue(right.submittedAtUtc) - dateValue(left.submittedAtUtc) || left.course.localeCompare(right.course));
  }, [groups, query, status]);
  const selected = groups.find(group => group.anchorId === selectedId);

  if (error) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;

  async function review(group: CourseAssessmentGroup, decision: "Approved" | "Rejected", reviewNote = note) {
    const reason = reviewNote.trim();
    if (decision === "Rejected" && !reason) {
      setActionError("Add an institutional review note before rejecting this course submission.");
      return;
    }
    setWorking(true);
    setActionError("");
    try {
      await assessmentApi.reviewCourse(group.anchorId, decision, reason);
      await load();
      setNote("");
    } catch (reason) {
      setActionError(reason instanceof Error ? reason.message : "The course approval workflow could not be updated.");
    } finally {
      setWorking(false);
    }
  }

  return <div className="viewport-data-page assessment-viewport-page submission-requests-page">
    <PageHeading eyebrow="Assessment · Institutional review" title="Submission Approvals" description="Review one complete Teacher-assigned course roster at a time. Approval and rejection apply to every Student in that course submission."/>
    {actionError && <section className="result-action-error" role="alert">{actionError}</section>}
    <DataTableToolbar query={query} onQueryChange={setQuery} searchPlaceholder="Search Teacher, course, cohort, or Student..." searchAriaLabel="Search course submission approvals" resultLabel={`${visible.length} course submissions`} className="record-toolbar panel submission-request-toolbar" searchClassName="record-search management-search module-search-field">
      <select value={status} onChange={event => setStatus(event.target.value)} aria-label="Approval status"><option value="Action">Action required</option><option>All</option><option value="SubmissionRequested">Initial approval</option><option value="ResubmitRequested">Resubmission approval</option><option value="Submitted">Final review</option><option value="SubmissionAuthorized">Submission authorized</option><option value="ResubmitAuthorized">Resubmission authorized</option><option value="Approved">Accepted</option><option value="Rejected">Rejected</option></select>
    </DataTableToolbar>
    <div className={`submission-request-layout${selected ? " has-detail" : ""}`}>
      <PaginatedDataRegion items={visible} resetKey={`${query}-${status}`} className="submission-request-region" empty={<DataTableEmptyState icon={<Icon name="check" size={28}/>} title="No course submissions" description="Course-level approval requests matching this filter will appear here."/>}>{pageItems => <DataTable as="section" className="panel horizontal-management-table submission-request-table" headerClassName="horizontal-management-head submission-request-head" rowSelector=":scope > .submission-request-row" columns={columns} ariaLabel="Course submission approvals">{pageItems.map(group => <ApprovalRow group={group} active={selectedId === group.anchorId} working={working} onApprove={() => void review(group, "Approved", "")} onReject={() => { setSelectedId(group.anchorId); setNote(""); setActionError(""); }} onView={() => { setSelectedId(group.anchorId); setNote(""); setActionError(""); }} key={group.key}/>)}</DataTable>}</PaginatedDataRegion>
      {selected && <ApprovalDetail group={selected} note={note} working={working} onNote={setNote} onClose={() => { setSelectedId(""); setNote(""); setActionError(""); }} onReview={decision => review(selected, decision)}/>} 
    </div>
  </div>;
}

function ApprovalRow({ group, active, working, onApprove, onReject, onView }: { group: CourseAssessmentGroup; active: boolean; working: boolean; onApprove: () => void; onReject: () => void; onView: () => void }) {
  const requestStage = group.status === "SubmissionRequested" || group.status === "ResubmitRequested";
  const finalStage = group.status === "Submitted" || group.status === "Pending";
  const actionable = requestStage || finalStage;
  return <div className={`horizontal-management-row submission-request-row${active ? " is-active" : ""}`} role="button" tabIndex={0} onClick={onView} onKeyDown={event => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); onView(); } }}>
    <strong className="management-code-value">{group.grades[0]?.values.gradeCode || "Not assigned"}</strong>
    <span className="submission-request-person"><strong>{group.teacher}</strong><small>{group.department}</small></span>
    <span className="submission-request-person"><strong>{group.course}</strong><small>{group.academicYear} / {group.term}</small></span>
    <span className="submission-request-person"><strong>{group.department}</strong><small>{group.grades.length} Students / Version {group.version}</small></span>
    <span className="submission-request-period"><strong>{group.year ? `Year ${group.year}` : "Not assigned"}</strong><small>{group.shift || "Shift not assigned"}</small></span>
    <span className="submission-request-action">
      {actionable ? <><button type="button" className="button primary" disabled={working} onClick={event => { event.stopPropagation(); onApprove(); }}>{finalStage ? "Accept" : "Approve"}</button><button type="button" className="button secondary assessment-reject" disabled={working} onClick={event => { event.stopPropagation(); onReject(); }}>Reject</button></> : <span className={`assessment-course-state status-${statusTone(group.status)}`}>{statusLabel(group.status)}</span>}
    </span>
  </div>;
}

function ApprovalDetail({ group, note, working, onNote, onClose, onReview }: { group: CourseAssessmentGroup; note: string; working: boolean; onNote: (value: string) => void; onClose: () => void; onReview: (decision: "Approved" | "Rejected") => void }) {
  const requestStage = group.status === "SubmissionRequested";
  const resubmitStage = group.status === "ResubmitRequested";
  const finalStage = group.status === "Submitted" || group.status === "Pending";
  const actionable = requestStage || resubmitStage || finalStage;
  return <aside className="submission-request-detail panel" aria-label="Course submission approval detail">
    <header><div><span>Course Submission</span><h2>{group.course}</h2><p>{group.teacher} · Version {group.version}</p></div><button type="button" aria-label="Close course submission" onClick={onClose}><Icon name="close" size={17}/></button></header>
    <dl className="submission-request-meta">
      <div><dt>Assigned Teacher</dt><dd>{group.teacher}</dd></div><div><dt>Course</dt><dd>{group.course}</dd></div>
      <div><dt>Academic Period</dt><dd>{group.academicYear} · {group.term}</dd></div><div><dt>Cohort</dt><dd>{group.year ? `Year ${group.year}` : "Year not assigned"} · {group.shift || "Shift not assigned"}</dd></div>
      <div><dt>Teacher submitted</dt><dd>{formatDateTime(group.submittedAtUtc)}</dd></div><div><dt>Administrator reviewed</dt><dd>{formatDateTime(group.reviewedAtUtc)}</dd></div>
      <div><dt>Roster</dt><dd>{group.grades.length} Students</dd></div><div><dt>Status</dt><dd><span className={`assessment-course-state status-${statusTone(group.status)}`}>{statusLabel(group.status)}</span></dd></div>
    </dl>
    <section className="submission-roster-summary"><h3>Complete course roster</h3>{group.grades.map(item => <div className="submission-roster-row" key={item.id}><span><strong>{item.values.student}</strong><small>{item.values.gradeCode}</small></span><Score label="Assignment" value={`${item.values.assignmentScore}/${item.values.assignmentMaximum}`}/><Score label="Midterm" value={`${item.values.midtermScore}/${item.values.midtermMaximum}`}/><Score label="Final" value={`${item.values.finalExamScore}/${item.values.finalExamMaximum}`}/><b>{item.values.score}/100 · {item.values.grade}</b></div>)}</section>
    {group.note && !actionable && <section className="submission-request-existing-note"><strong>Institutional review note</strong><p>{group.note}</p></section>}
    {actionable && <label className="submission-request-note"><span>Institutional review note {"(required for rejection)"}</span><textarea value={note} onChange={event => onNote(event.target.value)} placeholder="Record a clear reason for the Teacher if this complete course submission is rejected..."/></label>}
    <footer className="submission-request-controls">
      {actionable && <><button type="button" className="button secondary assessment-reject" disabled={working} onClick={() => onReview("Rejected")}>Reject course submission</button><button type="button" className="button primary" disabled={working} onClick={() => onReview("Approved")}>{working ? "Saving..." : requestStage ? "Authorize submission" : resubmitStage ? "Authorize resubmission" : "Accept course results"}</button></>}
      {!actionable && <p><strong>{statusLabel(group.status)}</strong><span>{workflowMessage(group)}</span></p>}
    </footer>
  </aside>;
}

function Score({ label, value }: { label: string; value: string }) { return <span><small>{label}</small><strong>{value}</strong></span>; }
function workflowMessage(group: CourseAssessmentGroup) {
  if (group.status === "SubmissionAuthorized") return "The Teacher may now submit the complete saved course roster.";
  if (group.status === "ResubmitAuthorized") return "The Teacher may now correct and resubmit the complete course roster.";
  if (group.status === "Rejected") return group.note || "The Teacher must correct the course roster before requesting approval again.";
  if (group.status === "Approved") return `Accepted by Administrator on ${formatDateTime(group.reviewedAtUtc)}.`;
  return "No Administrator action is available for this workflow state.";
}
function formatDateTime(value: string) { if (!value) return "Not recorded"; const date = new Date(value); return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat("en-GB", { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" }).format(date); }
function dateValue(value: string) { const parsed = new Date(value).valueOf(); return Number.isNaN(parsed) ? 0 : parsed; }
