"use client";

import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { DataTable, DataTableEmptyState, DataTableToolbar, PaginatedDataRegion, type DataTableColumn } from "@/components/data-table";
import { Icon } from "@/components/icon";
import { ManagementDataCell } from "@/components/management-data-cell";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { workflowSourceSearch } from "@/lib/workflow-code";
import { resultApi } from "./result-api";
import type { SemesterResult } from "./result-types";

type ResultMode = "current" | "history";
const copy: Record<ResultMode, { eyebrow: string; title: string; description: string }> = {
  current: { eyebrow: "Assessment · Semester declaration", title: "Semester Result", description: "Review confirmed Student grades, attendance, and totals. Declaration is required before semester progression." },
  history: { eyebrow: "Declared semester archive", title: "Result Semester", description: "Declared results move here as read-only history after their semester ends." },
};

export function ResultWorkspace({ mode }: { mode: ResultMode }) {
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [rows, setRows] = useState<SemesterResult[]>([]);
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [outcome, setOutcome] = useState("all");
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);
  const [publishing, setPublishing] = useState("");
  const [publishingAll, setPublishingAll] = useState(false);
  const [actionError, setActionError] = useState("");
  const load = useCallback(() => resultApi.get(departmentId, year, mode === "history").then(value => { setRows(value); setReady(true); setError(false); }).catch(() => setError(true)), [departmentId, mode, year]);
  useEffect(() => { void load(); }, [load]);
  const visible = useMemo(() => rows.filter(row => {
    const text = workflowSourceSearch(query).toLowerCase();
    const normalizedOutcome = resultOutcome(row.totalGrade).toLowerCase().replaceAll(" ", "-");
    const matchesOutcome = outcome === "all" || normalizedOutcome === outcome;
    return matchesOutcome && (!text || [row.resultCode, row.fullName, row.studentCode, row.department, row.shift, row.semester, row.attendanceGrade, row.totalGrade, ...row.grades.flatMap(grade => [grade.courseCode, grade.name, grade.grade])].some(value => value.toLowerCase().includes(text)));
  }).toSorted((left, right) => left.year - right.year || semesterNumber(left.semester) - semesterNumber(right.semester) || shiftNumber(left.shift) - shiftNumber(right.shift) || left.fullName.localeCompare(right.fullName) || left.academicYear.localeCompare(right.academicYear, undefined, { numeric: true })), [outcome, query, rows]);
  const readyCount = rows.filter(row => row.publicationStatus === "Ready").length;
  const details = copy[mode];
  if (error) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;

  async function publish(row: SemesterResult) {
    const key = resultKey(row);
    setPublishing(key); setActionError("");
    try { await resultApi.publish(row); await load(); }
    catch (reason) { setActionError(reason instanceof Error ? reason.message : "Could not declare this Semester Result."); }
    finally { setPublishing(""); }
  }

  async function publishAll() {
    setPublishingAll(true); setActionError("");
    try { await resultApi.publishAll(departmentId, year); await load(); }
    catch (reason) { setActionError(reason instanceof Error ? reason.message : "Could not declare the ready Semester Results."); }
    finally { setPublishingAll(false); }
  }

  return <div className="viewport-data-page result-viewport-page">
    <PageHeading
      eyebrow={details.eyebrow}
      title={details.title}
      description={`${details.description}${year ? ` Showing Year ${year}.` : ""}`}
      actions={mode === "current" ? <button type="button" className="button primary" disabled={readyCount === 0 || publishingAll || Boolean(publishing)} onClick={() => void publishAll()}>{publishingAll ? "Declaring…" : `Declare Semester Results (${readyCount})`}</button> : undefined}
    />
    {actionError && <section className="result-action-error" role="alert">{actionError}</section>}
    <DataTableToolbar query={query} onQueryChange={setQuery} searchPlaceholder="Search result code, Student, course, shift, or department…" searchAriaLabel="Search results" resultLabel={`${visible.length} results`} className="record-toolbar panel result-toolbar" searchClassName="record-search management-search module-search-field">
      <select value={outcome} onChange={event => setOutcome(event.target.value)} aria-label="Result outcome"><option value="all">All outcomes</option><option value="pass">Pass</option><option value="retake">Retake</option><option value="fail">Fail</option><option value="pending">Pending</option></select>
    </DataTableToolbar>
    <PaginatedDataRegion items={visible} resetKey={`${outcome}-${query}`} className="result-paginated-region" empty={<DataTableEmptyState icon={<Icon name="grade" size={28}/>} title="No semester results found" description={mode === "history" ? "Declared results will archive here after the semester ends." : "Confirm complete grades in Student Results first."}/>}>{pageItems => <DataTable as="section" className="panel horizontal-management-table semester-result-table" headerClassName="horizontal-management-head semester-result-head" rowSelector=":scope > .semester-result-row" columns={resultColumns(mode)} ariaLabel="Semester Result">{pageItems.map(row => <ResultRow row={row} mode={mode} publishing={publishing === resultKey(row)} onPublish={() => void publish(row)} key={resultKey(row)}/>)}</DataTable>}</PaginatedDataRegion>
  </div>;
}

function ResultRow({ row, mode, publishing, onPublish }: { row: SemesterResult; mode: ResultMode; publishing: boolean; onPublish: () => void }) {
  return <article className="horizontal-management-row semester-result-row">
    <Cell label="Resultcode" className="horizontal-detail result-record-code"><strong className="management-code-value">{row.resultCode}</strong></Cell>
    <Cell label="Name"><strong>{row.fullName}</strong></Cell>
    <Cell label="Department"><strong>{row.department}</strong></Cell>
    <Cell label="Year"><strong>Year {row.year}</strong></Cell>
    <Cell label="Shift"><strong>{row.shift || "—"}</strong></Cell>
    <Cell label="Present" className="result-attendance-number is-present"><strong>{row.presentCount}</strong></Cell>
    <Cell label="Permission" className="result-attendance-number is-permission"><strong>{row.permissionCount}</strong></Cell>
    <Cell label="Absent" className="result-attendance-number is-absent"><strong>{row.absentCount}</strong></Cell>
    <Cell label="Attendance" className="result-attendance-score"><div className={`semester-result-summary-card grade-${gradeTone(row.attendanceGrade)}`} data-preserve-table-font=""><strong>{formatScore(row.attendanceScore)}/{row.attendanceGrade}</strong></div></Cell>
    <Cell label="Courses" className="result-courses-cell"><CourseCards row={row}/></Cell>
    <Cell label="Total" className="result-total-cell"><div className={`semester-result-summary-card semester-result-total-card grade-${gradeTone(row.overallGrade)}`} data-preserve-table-font=""><strong>{formatScore(row.totalScore)}/{row.overallGrade}</strong></div></Cell>
    <Cell label="Result" className="result-outcome-cell"><span className={`table-status result-${row.totalGrade.toLowerCase().replaceAll(" ", "-")}`}>{resultOutcome(row.totalGrade)}</span></Cell>
    <Cell label="Ready / Declared" className="result-status-cell"><span className={`table-status result-publication-state state-${row.publicationStatus.toLowerCase()}`}>{row.publicationStatus}</span></Cell>
    <Cell label={mode === "history" ? "Declared at" : "Actions"} className="management-action-cell result-action-cell">{mode === "current" ? <div className="management-actions"><button type="button" disabled={row.publicationStatus !== "Ready" || publishing} onClick={onPublish}>{publishing ? "Declaring…" : row.publicationStatus === "Declared" ? "Declared" : "Declare"}</button></div> : <time>{row.publishedAtUtc ? new Date(row.publishedAtUtc).toLocaleDateString() : "—"}</time>}</Cell>
  </article>;
}

const baseResultColumns: DataTableColumn[] = [
  { key: "result-code", label: "Resultcode", align: "center", minimumWidth: 125 },
  { key: "name", label: "Name", minimumWidth: 135 },
  { key: "department", label: "Department", minimumWidth: 130 },
  { key: "year", label: "Year", align: "center", minimumWidth: 65 },
  { key: "shift", label: "Shift", align: "center", minimumWidth: 80 },
  { key: "present", label: "Present", align: "center", minimumWidth: 65 },
  { key: "permission", label: "Permission", align: "center", minimumWidth: 80 },
  { key: "absent", label: "Absent", align: "center", minimumWidth: 65 },
  { key: "attendance", label: "Attendance", align: "center", minimumWidth: 95 },
  { key: "courses", label: "Courses", minimumWidth: 390 },
  { key: "total", label: "Total", align: "center", minimumWidth: 105 },
  { key: "result", label: "Result", align: "center", minimumWidth: 90 },
  { key: "status", label: "Ready / Declared", align: "center", minimumWidth: 100 },
];

function resultColumns(mode: ResultMode): DataTableColumn[] {
  return [...baseResultColumns, { key: "actions", label: mode === "history" ? "Declared at" : "Actions", align: "center", minimumWidth: 95 }];
}

function CourseCards({ row }: { row: SemesterResult }) {
  const courses = Array.from({ length: row.expectedCourseCount || 5 }, (_, index) => row.grades[index] ?? { courseId: `draft-${index}`, courseCode: "", name: `Course ${index + 1}`, score: null, grade: "Draft", isApproved: false });
  return <div className="result-course-cards">{courses.map((course, index) => <span className={course.isApproved ? "is-approved" : "is-draft"} key={`${course.courseId}-${index}`}><small>{course.courseCode || `Course ${index + 1}`}</small><strong>{course.name}</strong><b>{course.isApproved && course.score !== null ? `${formatScore(course.score)}/${course.grade}` : "Draft"}</b></span>)}</div>;
}

function Cell({ label, children, className = "horizontal-detail" }: { label: string; children: React.ReactNode; className?: string }) {
  return <ManagementDataCell label={label} className={className}>{children}</ManagementDataCell>;
}

function resultKey(row: SemesterResult) { return `${row.studentId}-${row.academicYear}-${row.semester}`; }
function resultOutcome(value: string) { return value === "Retake Exam" ? "Retake" : ["A", "B", "C", "D", "E"].includes(value) ? "Pass" : value; }
function formatScore(value: number) { return Number.isInteger(value) ? value.toFixed(0) : value.toFixed(2).replace(/0+$/, "").replace(/\.$/, ""); }
function gradeTone(value: string) { return value.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "") || "draft"; }
function semesterNumber(value: string) { return Number(value.match(/\d+/)?.[0] ?? 0); }
function shiftNumber(value: string) { const order = ["morning", "afternoon", "evening", "weekend"]; const index = order.indexOf(value.toLowerCase()); return index === -1 ? order.length : index; }
