"use client";

import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { DataTable, DataTableEmptyState, DataTableToolbar, PaginatedDataRegion } from "@/components/data-table";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { workflowCode, workflowSourceSearch } from "@/lib/workflow-code";
import { resultApi } from "./result-api";
import type { SemesterResult } from "./result-types";

type ResultMode = "current" | "history";
const copy: Record<ResultMode, { eyebrow: string; title: string; description: string }> = {
  current: { eyebrow: "Administrator publication control", title: "Academic Results", description: "Review approved semester evidence in a Record-style view, then publish complete results to Students." },
  history: { eyebrow: "Published semester archive", title: "Result Semester", description: "Every published result is preserved here as read-only semester history." },
};

export function ResultWorkspace({ mode }: { mode: ResultMode }) {
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [rows, setRows] = useState<SemesterResult[]>([]);
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [semester, setSemester] = useState("all");
  const [academicYear, setAcademicYear] = useState("all");
  const [outcome, setOutcome] = useState("all");
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);
  const [publishing, setPublishing] = useState("");
  const [actionError, setActionError] = useState("");
  const load = useCallback(() => resultApi.get(departmentId, year, mode === "history").then(value => { setRows(value); setReady(true); setError(false); }).catch(() => setError(true)), [departmentId, mode, year]);
  useEffect(() => { void load(); }, [load]);
  const semesters = useMemo(() => [...new Set(rows.map(row => row.semester))].sort(), [rows]);
  const academicYears = useMemo(() => [...new Set(rows.map(row => row.academicYear))].sort().reverse(), [rows]);
  const visible = useMemo(() => rows.filter(row => {
    const text = workflowSourceSearch(query).toLowerCase();
    const matchesOutcome = outcome === "all" || row.totalGrade.toLowerCase().replaceAll(" ", "-") === outcome;
    return matchesOutcome && (semester === "all" || row.semester === semester) && (academicYear === "all" || row.academicYear === academicYear) && (!text || [row.fullName, row.studentCode, row.department, row.semester, ...row.grades.flatMap(grade => [grade.courseCode, grade.name])].some(value => value.toLowerCase().includes(text)));
  }).toSorted((left, right) => left.year - right.year || semesterNumber(left.semester) - semesterNumber(right.semester) || left.fullName.localeCompare(right.fullName) || left.academicYear.localeCompare(right.academicYear, undefined, { numeric: true })), [academicYear, outcome, query, rows, semester]);
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

  return <div className="viewport-data-page result-viewport-page">
    <PageHeading eyebrow={details.eyebrow} title={details.title} description={`${details.description}${year ? ` Showing Year ${year}.` : ""}`}/>
    {actionError && <section className="result-action-error" role="alert">{actionError}</section>}
    <section className="result-rule-notice"><Icon name="grade" size={20}/><div><strong>Publication rule</strong><span>Teacher submits grades → Administrator approves each course in Assessment → five approved courses make the result Ready → Publish makes it visible to the Student and archives it in History.</span></div></section>
    <DataTableToolbar query={query} onQueryChange={setQuery} searchPlaceholder="Search student, course, or department…" searchAriaLabel="Search results" resultLabel={`${visible.length} results`} className="record-toolbar panel result-toolbar" searchClassName="record-search management-search module-search-field"><select value={academicYear} onChange={event => setAcademicYear(event.target.value)}><option value="all">All academic years</option>{academicYears.map(value => <option value={value} key={value}>{value}</option>)}</select><select value={semester} onChange={event => setSemester(event.target.value)}><option value="all">All semesters</option>{semesters.map(value => <option value={value} key={value}>{value}</option>)}</select><select value={outcome} onChange={event => setOutcome(event.target.value)} aria-label="Result outcome"><option value="all">All outcomes</option><option value="pass">Pass</option><option value="retake-exam">Retake exam</option><option value="fail">Fail</option><option value="pending">Pending</option></select></DataTableToolbar>
    <PaginatedDataRegion items={visible} resetKey={`${academicYear}-${semester}-${outcome}-${query}-${year}`} className="result-paginated-region" empty={<DataTableEmptyState icon={<Icon name="grade" size={28}/>} title="No semester results found" description={mode === "history" ? "Published semester results will archive here." : "Approved assessment evidence will appear here."}/>}>{pageItems => <DataTable className="panel semester-result-table" headerClassName="semester-result-head" rowSelector=".semester-result-row" columns={["Student", "Year / semester", "Attendance", "Course grades", "Total", "Average", "Total grade", mode === "history" ? "Published" : "Publication"]}><div>{pageItems.map(row => <ResultRow row={row} mode={mode} publishing={publishing === resultKey(row)} onPublish={() => void publish(row)} key={resultKey(row)}/>)}</div></DataTable>}</PaginatedDataRegion>
  </div>;
}

function ResultRow({ row, mode, publishing, onPublish }: { row: SemesterResult; mode: ResultMode; publishing: boolean; onPublish: () => void }) {
  return <article className="semester-result-row"><div className="result-student"><span>{initials(row.fullName)}</span><div><strong>{row.fullName}</strong><small>{workflowCode(row.studentCode, "student", "history")} · {row.department}</small></div></div><div className="result-period"><strong>Year {row.year}</strong><span>{row.semester}</span><small>{row.academicYear}</small></div><div className="result-attendance"><span className="present"><b>{row.presentCount}</b> Present</span><span className="absent"><b>{row.absentCount}</b> Absent</span><span className="permission"><b>{row.permissionCount}</b> Permission</span></div><div className="result-course-grades">{row.grades.length ? row.grades.map(grade => <span key={grade.courseId}><strong>{workflowCode(grade.courseCode, "course", "history")} {grade.score == null ? "—" : grade.score.toFixed(1)}/{grade.grade}</strong><small>{grade.name}</small></span>) : <i>No approved course grades yet</i>}</div><div className="result-total"><strong>{row.totalScore.toFixed(1)}</strong><span>{row.totalCourses}/5 courses</span></div><div className="result-average"><strong>{row.average.toFixed(2)}%</strong><span>Total ÷ 5</span></div><span className={`result-final result-${row.totalGrade.toLowerCase().replaceAll(" ", "-")}`}>{row.totalGrade}</span><div className="result-publication"><span className={`result-publication-state state-${row.publicationStatus.toLowerCase()}`}>{row.publicationStatus}</span>{mode === "current" && !row.isPublished && <button type="button" className="button primary" disabled={row.publicationStatus !== "Ready" || publishing} onClick={onPublish}>{publishing ? "Publishing…" : "Publish"}</button>}{row.isPublished && <small>{row.publishedAtUtc ? new Date(row.publishedAtUtc).toLocaleDateString() : "Visible to Student"}</small>}</div></article>;
}

function resultKey(row: SemesterResult) { return `${row.studentId}-${row.academicYear}-${row.semester}`; }
function initials(value: string) { return value.split(" ").map(part => part[0]).join("").slice(0, 2).toUpperCase(); }
function semesterNumber(value: string) { return Number(value.match(/\d+/)?.[0] ?? 0); }
