"use client";

import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { DataTable, DataTableEmptyState, DataTableToolbar, PaginatedDataRegion, type DataTableColumn } from "@/components/data-table";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { workflowSourceSearch } from "@/lib/workflow-code";
import { assessmentApi } from "./assessment-api";
import type { GradeAssessment } from "./assessment-types";

export function AssessmentWorkspace() {
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const [rows, setRows] = useState<GradeAssessment[]>([]);
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [status, setStatus] = useState("Action");
  const [notes, setNotes] = useState<Record<string, string>>({});
  const [working, setWorking] = useState("");
  const [actionError, setActionError] = useState("");
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);
  const load = useCallback(() => assessmentApi.get(departmentId).then(value => { setRows(value); setReady(true); setError(false); }).catch(() => setError(true)), [departmentId]);
  useEffect(() => { void load(); }, [load]);
  const visible = useMemo(() => rows.filter(item => {
    const text = workflowSourceSearch(query).toLowerCase();
    const matchesStatus = status === "All" || status === "Action" && ["Pending", "ResubmitRequested"].includes(item.values.reviewStatus) || item.values.reviewStatus === status;
    return matchesStatus && (!text || [item.values.student, item.values.course, item.values.department, item.values.submittedByTeacher, item.values.gradeCode].some(value => value.toLowerCase().includes(text)));
  }).toSorted((left, right) => right.values.submittedAtUtc.localeCompare(left.values.submittedAtUtc)), [query, rows, status]);
  if (error) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;
  async function review(item: GradeAssessment, decision: "Approved" | "Rejected") {
    const note = notes[item.id]?.trim() ?? "";
    if (decision === "Rejected" && !note) { setActionError("Add a correction note before requesting a new submission."); return; }
    setWorking(item.id); setActionError("");
    try { await assessmentApi.review(item.id, decision, note); await load(); }
    catch (reason) { setActionError(reason instanceof Error ? reason.message : "Could not review this grade submission."); }
    finally { setWorking(""); }
  }

  async function authorizeResubmission(item: GradeAssessment) {
    setWorking(item.id); setActionError("");
    try { await assessmentApi.authorizeResubmission(item.id); await load(); }
    catch (reason) { setActionError(reason instanceof Error ? reason.message : "Could not grant resubmission permission."); }
    finally { setWorking(""); }
  }

  return <div className="viewport-data-page assessment-viewport-page">
    <PageHeading eyebrow="Administrator grade control" title="Assessment" description="Confirm accurate Teacher submissions or reject them with a correction note. Rejected grades return to the Teacher for a new submission."/>
    {actionError && <section className="result-action-error" role="alert">{actionError}</section>}
    <DataTableToolbar query={query} onQueryChange={setQuery} searchPlaceholder="Search student, Teacher, course…" searchAriaLabel="Search assessments" resultLabel={`${visible.length} submissions`} className="record-toolbar panel assessment-toolbar" searchClassName="record-search management-search module-search-field"><select value={status} onChange={event => setStatus(event.target.value)} aria-label="Review status"><option value="Action">Action required</option><option>Pending</option><option value="ResubmitRequested">Resubmit requests</option><option>Approved</option><option>Rejected</option><option value="ResubmitAuthorized">Resubmit allowed</option><option>All</option></select></DataTableToolbar>
    <PaginatedDataRegion items={visible} resetKey={`${query}-${status}`} className="assessment-paginated-region" empty={<DataTableEmptyState icon={<Icon name="check" size={28}/>} title="No assessment submissions" description="Teacher grade submissions matching this filter will appear here."/>}>{pageItems => <DataTable className="panel assessment-record-table" headerClassName="assessment-record-head" rowSelector=".assessment-record-row" columns={assessmentColumns}><div>{pageItems.map(item => <AssessmentRow item={item} note={notes[item.id] ?? ""} working={working === item.id} onNote={value => setNotes(current => ({ ...current, [item.id]: value }))} onReview={decision => void review(item, decision)} onAuthorize={() => void authorizeResubmission(item)} key={item.id}/>)}</div></DataTable>}</PaginatedDataRegion>
  </div>;
}

function AssessmentRow({ item, note, working, onNote, onReview, onAuthorize }: { item: GradeAssessment; note: string; working: boolean; onNote: (value: string) => void; onReview: (decision: "Approved" | "Rejected") => void; onAuthorize: () => void }) {
  const value = item.values;
  const pending = value.reviewStatus === "Pending";
  const requested = value.reviewStatus === "ResubmitRequested";
  return <article className="assessment-record-row">
    <div className="assessment-code"><strong>{value.gradeCode}</strong><span>Version {value.submissionVersion}</span></div>
    <div className="assessment-student"><strong>{value.student}</strong><span>{value.department}</span></div>
    <div className="assessment-course"><strong>{value.course}</strong><span>{value.academicYear} · {value.term}</span></div>
    <div className="assessment-components"><Score label="Attendance" score={value.attendanceScore} maximum={value.attendanceMaximum}/><Score label="Assignment" score={value.assignmentScore} maximum={value.assignmentMaximum}/><Score label="Midterm" score={value.midtermScore} maximum={value.midtermMaximum}/><Score label="Final" score={value.finalExamScore} maximum={value.finalExamMaximum}/></div>
    <div className="assessment-total"><strong>{value.score || "0"}</strong><span>{value.grade || "—"}</span></div>
    <div className="assessment-submitter"><strong>{value.submittedByTeacher || "Teacher"}</strong><small>{value.submittedAtUtc ? new Date(value.submittedAtUtc).toLocaleDateString() : "No date"}</small></div>
    <span className={`assessment-status status-${value.reviewStatus.toLowerCase()}`}>{statusLabel(value.reviewStatus)}</span>
    <div className="assessment-decision">{pending ? <><input value={note} onChange={event => onNote(event.target.value)} placeholder="Correction note for rejection…"/><div><button type="button" className="button secondary assessment-reject" disabled={working} onClick={() => onReview("Rejected")}>Reject</button><button type="button" className="button primary" disabled={working} onClick={() => onReview("Approved")}>{working ? "Saving…" : "Confirm"}</button></div></> : requested ? <><p><strong>Teacher requests permission</strong><span>{value.reviewNote || "Allow the Teacher to refill these scores."}</span></p><button type="button" className="button primary" disabled={working} onClick={onAuthorize}>{working ? "Saving…" : "Allow resubmit"}</button></> : <p><strong>{value.reviewStatus === "Rejected" ? "Correction sent" : value.reviewStatus === "ResubmitAuthorized" ? "Resubmit permission granted" : "Administrator decision"}</strong><span>{value.reviewNote || (value.reviewStatus === "Approved" ? "Grade confirmed" : "Waiting for Teacher")}</span></p>}</div>
  </article>;
}

function Score({ label, score, maximum }: { label: string; score: string; maximum: string }) { return <span><small>{label}</small><strong>{score || "0"}/{maximum}</strong></span>; }
function statusLabel(status: GradeAssessment["values"]["reviewStatus"]) { return status === "Approved" ? "Confirmed" : status === "ResubmitRequested" ? "Resubmit requested" : status === "ResubmitAuthorized" ? "Resubmit allowed" : status; }
const assessmentColumns: DataTableColumn[] = [
  { key: "code", label: "Grade code", align: "center", minimumWidth: 100 },
  { key: "student", label: "Student", align: "left", minimumWidth: 145 },
  { key: "course", label: "Course", align: "left", minimumWidth: 145 },
  { key: "components", label: "Recorded scores", align: "center", minimumWidth: 285 },
  { key: "total", label: "Total", align: "center", minimumWidth: 72 },
  { key: "teacher", label: "Teacher", align: "left", minimumWidth: 125 },
  { key: "status", label: "Status", align: "center", minimumWidth: 120 },
  { key: "decision", label: "Administrator review", align: "center", minimumWidth: 285 },
];
