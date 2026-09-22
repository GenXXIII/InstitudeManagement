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
  current: { eyebrow: "Administrator publication control", title: "Academic Results", description: "Review semester evidence in a Management-style table, then publish complete results to Students." },
  history: { eyebrow: "Published semester archive", title: "Result Semester", description: "Every published result is preserved here as read-only semester history." },
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
    const matchesOutcome = outcome === "all" || row.totalGrade.toLowerCase().replaceAll(" ", "-") === outcome;
    return matchesOutcome && (!text || [row.resultCode, row.fullName, row.studentCode, row.department, row.semester, ...row.grades.flatMap(grade => [grade.courseCode, grade.name])].some(value => value.toLowerCase().includes(text)));
  }).toSorted((left, right) => left.year - right.year || semesterNumber(left.semester) - semesterNumber(right.semester) || shiftNumber(left.shift) - shiftNumber(right.shift) || left.fullName.localeCompare(right.fullName) || left.academicYear.localeCompare(right.academicYear, undefined, { numeric: true })), [outcome, query, rows]);
  const readyCount = rows.filter(row => row.publicationStatus === "Ready").length;
  const details = copy[mode];
  if (error) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;

  async function publish(row: SemesterResult) {
    const key = resultKey(row);
    setPublishing(key); setActionError("");
    try { await resultApi.publish(row); await load(); }
    catch (reason) { setActionError(reason instanceof Error ? reason.message : "Could not publish this semester result."); }
    finally { setPublishing(""); }
  }

  async function publishAll() {
    setPublishingAll(true); setActionError("");
    try { await resultApi.publishAll(departmentId, year); await load(); }
    catch (reason) { setActionError(reason instanceof Error ? reason.message : "Could not publish the ready semester results."); }
    finally { setPublishingAll(false); }
  }

  return <div className="viewport-data-page result-viewport-page">
    <PageHeading eyebrow={details.eyebrow} title={details.title} description={`${details.description}${year ? ` Showing Year ${year}.` : ""}`}/>
    {actionError && <section className="result-action-error" role="alert">{actionError}</section>}
    <DataTableToolbar query={query} onQueryChange={setQuery} searchPlaceholder="Search result code, student, course, or department…" searchAriaLabel="Search results" resultLabel={`${visible.length} results`} className="record-toolbar panel result-toolbar" searchClassName="record-search management-search module-search-field">
      <select value={outcome} onChange={event => setOutcome(event.target.value)} aria-label="Result outcome"><option value="all">All outcomes</option><option value="pass">Pass</option><option value="retake-exam">Retake exam</option><option value="fail">Fail</option><option value="pending">Pending</option></select>
      {mode === "current" && <button type="button" className="button primary result-publish-all" disabled={readyCount === 0 || publishingAll || Boolean(publishing)} onClick={() => void publishAll()}>{publishingAll ? "Publishing…" : `Publish all (${readyCount})`}</button>}
    </DataTableToolbar>
    <PaginatedDataRegion items={visible} resetKey={`${outcome}-${query}`} className="result-paginated-region" empty={<DataTableEmptyState icon={<Icon name="grade" size={28}/>} title="No semester results found" description={mode === "history" ? "Published semester results will archive here." : "Assessment evidence will appear here."}/>}>{pageItems => <DataTable as="section" className="panel horizontal-management-table semester-result-table" headerClassName="horizontal-management-head semester-result-head" rowSelector=":scope > .semester-result-row" columns={resultColumns(mode)}>{pageItems.map(row => <ResultRow row={row} mode={mode} publishing={publishing === resultKey(row)} onPublish={() => void publish(row)} key={resultKey(row)}/>)}</DataTable>}</PaginatedDataRegion>
  </div>;
}

function ResultRow({ row, mode, publishing, onPublish }: { row: SemesterResult; mode: ResultMode; publishing: boolean; onPublish: () => void }) {
  return <article className="horizontal-management-row semester-result-row">
    <Cell label="Result code" className="horizontal-detail result-record-code"><strong className="management-code-value">{row.resultCode}</strong></Cell>
    <Cell label="Name"><strong>{row.fullName}</strong></Cell>
    <Cell label="Department"><strong>{row.department}</strong></Cell>
    <Cell label="Year"><strong>Year {row.year}</strong></Cell>
    <Cell label="Shift"><strong>{row.shift || "—"}</strong></Cell>
    <Cell label="Present" className="result-attendance-number is-present"><strong>{row.presentCount}</strong></Cell>
    <Cell label="Permission" className="result-attendance-number is-permission"><strong>{row.permissionCount}</strong></Cell>
    <Cell label="Absent" className="result-attendance-number is-absent"><strong>{row.absentCount}</strong></Cell>
    <Cell label="Courses" className="result-courses-cell"><CourseCards row={row}/></Cell>
    <Cell label="Draft / Ready" className="result-status-cell"><span className={`table-status result-publication-state state-${row.publicationStatus.toLowerCase()}`}>{row.publicationStatus}</span></Cell>
    <Cell label={mode === "history" ? "Published at" : "Actions"} className="management-action-cell result-action-cell">{mode === "current" ? <div className="management-actions"><button type="button" disabled={row.publicationStatus !== "Ready" || publishing} onClick={onPublish}>{publishing ? "Publishing…" : "Publish"}</button></div> : <time>{row.publishedAtUtc ? new Date(row.publishedAtUtc).toLocaleDateString() : "—"}</time>}</Cell>
  </article>;
}

const baseResultColumns: DataTableColumn[] = [
  { key: "result-code", label: "Result code", align: "center", minimumWidth: 125 },
  { key: "name", label: "Name", minimumWidth: 135 },
  { key: "department", label: "Department", minimumWidth: 130 },
  { key: "year", label: "Year", align: "center", minimumWidth: 65 },
  { key: "shift", label: "Shift", align: "center", minimumWidth: 80 },
  { key: "present", label: "Present", align: "center", minimumWidth: 65 },
  { key: "permission", label: "Permission", align: "center", minimumWidth: 75 },
  { key: "absent", label: "Absent", align: "center", minimumWidth: 65 },
  { key: "courses", label: "Courses", minimumWidth: 350 },
  { key: "status", label: "Draft / Ready", align: "center", minimumWidth: 85 },
];

function resultColumns(mode: ResultMode): DataTableColumn[] {
  return [...baseResultColumns, { key: "actions", label: mode === "history" ? "Published at" : "Actions", align: "center", minimumWidth: 95 }];
}

function CourseCards({ row }: { row: SemesterResult }) {
  const courses = Array.from({ length: 5 }, (_, index) => row.grades[index] ?? { courseId: `draft-${index}`, name: `Course ${index + 1}`, grade: "Draft", isApproved: false });
  return <div className="result-course-cards">{courses.map((course, index) => <span className={course.isApproved ? "is-approved" : "is-draft"} key={`${course.courseId}-${index}`}><strong>{course.name}</strong><b>{course.isApproved ? course.grade : "Draft"}</b></span>)}</div>;
}

function Cell({ label, children, className = "horizontal-detail" }: { label: string; children: React.ReactNode; className?: string }) {
  return <ManagementDataCell label={label} className={className}>{children}</ManagementDataCell>;
}

function resultKey(row: SemesterResult) { return `${row.studentId}-${row.academicYear}-${row.semester}`; }
function semesterNumber(value: string) { return Number(value.match(/\d+/)?.[0] ?? 0); }
function shiftNumber(value: string) { const order = ["morning", "afternoon", "evening", "weekend"]; const index = order.indexOf(value.toLowerCase()); return index === -1 ? order.length : index; }
