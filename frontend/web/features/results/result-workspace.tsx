"use client";

import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Icon } from "@/components/icon";
import { DataPagination, useDataPagination } from "@/components/data-pagination";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { resultApi } from "./result-api";
import type { SemesterResult } from "./result-types";

type ResultMode = "history";
const copy: Record<ResultMode, { eyebrow: string; title: string; description: string }> = {
  history: { eyebrow: "Permanent academic history", title: "Result Semester", description: "Every completed semester outcome remains available here after the active semester ledger advances." },
};

export function ResultWorkspace({ mode }: { mode: ResultMode }) {
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [rows, setRows] = useState<SemesterResult[]>([]);
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [semester, setSemester] = useState("all");
  const [academicYear, setAcademicYear] = useState("all");
  const [outcome, setOutcome] = useState("final");
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);
  const load = useCallback(() => resultApi.get(departmentId, year).then(value => { setRows(value); setReady(true); setError(false); }).catch(() => setError(true)), [departmentId, year]);
  useEffect(() => { void load(); }, [load]);
  const semesters = useMemo(() => [...new Set(rows.map(row => row.semester))].sort(), [rows]);
  const academicYears = useMemo(() => [...new Set(rows.map(row => row.academicYear))].sort().reverse(), [rows]);
  const visible = useMemo(() => rows.filter(row => {
    const text = query.trim().toLowerCase();
    const matchesOutcome = outcome === "all" || outcome === "final" && row.totalGrade !== "Pending" || row.totalGrade.toLowerCase().replaceAll(" ", "-") === outcome;
    return matchesOutcome && (semester === "all" || row.semester === semester) && (academicYear === "all" || row.academicYear === academicYear) && (!text || [row.fullName, row.studentCode, row.department, row.semester, ...row.grades.flatMap(grade => [grade.courseCode, grade.name])].some(value => value.toLowerCase().includes(text)));
  }).toSorted((left, right) => mode === "history" ? right.academicYear.localeCompare(left.academicYear) || semesterNumber(right.semester) - semesterNumber(left.semester) || left.fullName.localeCompare(right.fullName) : left.year - right.year || left.fullName.localeCompare(right.fullName)), [academicYear, mode, outcome, query, rows, semester]);
  const pagination = useDataPagination(visible, `${academicYear}-${semester}-${outcome}-${query}-${year}`);
  const details = copy[mode];
  if (error) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;
  const failed = visible.filter(row => row.totalGrade === "Fail").length;
  const retake = visible.filter(row => row.totalGrade === "Retake Exam").length;
  return <div className="viewport-data-page result-viewport-page">
    <PageHeading eyebrow={details.eyebrow} title={details.title} description={`${details.description}${year ? ` Showing Year ${year}.` : ""}`} actions={<button className="button secondary" onClick={load}><Icon name="pulse" size={15}/>Refresh</button>}/>
    <section className="result-rule-notice"><Icon name="grade" size={20}/><div><strong>Final result logic</strong><span>8+ absences = Fail · 6–7 absences = Retake Exam · any F = Retake Exam · five courses are required. Average = total score ÷ 5, using the grade thresholds configured in Administration.</span></div></section>
    <section className="operational-record-summary result-summary"><article className="panel"><span>Semester results</span><strong>{visible.length}</strong><small>One student per semester</small></article><article className="panel"><span>Retake exam</span><strong>{retake}</strong><small>6–7 absent records</small></article><article className="panel"><span>Failed by attendance</span><strong>{failed}</strong><small>8 or more absent records</small></article></section>
    <section className="record-toolbar panel result-toolbar"><label className="record-search"><Icon name="search" size={17}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder="Search student, course, or department…"/></label><select value={academicYear} onChange={event => setAcademicYear(event.target.value)}><option value="all">All academic years</option>{academicYears.map(value => <option value={value} key={value}>{value}</option>)}</select><select value={semester} onChange={event => setSemester(event.target.value)}><option value="all">All semesters</option>{semesters.map(value => <option value={value} key={value}>{value}</option>)}</select><select value={outcome} onChange={event => setOutcome(event.target.value)} aria-label="Result outcome"><option value="final">Completed results</option><option value="retake-exam">Retake exam</option><option value="fail">Failed</option><option value="all">All outcomes</option></select><span className="record-count">{visible.length} results</span></section>
    {visible.length ? <section className="result-paginated-region"><div className="panel semester-result-table"><div className="semester-result-head"><span>Student</span><span>Year / semester</span><span>Attendance</span><span>Course grades</span><span>Total</span><span>Average</span><span>Total grade</span></div><div>{pagination.pageItems.map(row => <ResultRow row={row} key={`${row.studentId}-${row.academicYear}-${row.semester}`}/>)}</div></div><DataPagination page={pagination.page} pageCount={pagination.pageCount} total={visible.length} onPage={pagination.setPage}/></section> : <section className="panel empty-state"><div className="empty-icon"><Icon name="grade" size={28}/></div><strong>No semester results found</strong><span>Change the year, semester, or search filters.</span></section>}
  </div>;
}

function ResultRow({ row }: { row: SemesterResult }) {
  return <article className="semester-result-row"><div className="result-student"><span>{initials(row.fullName)}</span><div><strong>{row.fullName}</strong><small>{row.studentCode} · {row.department}</small></div></div><div className="result-period"><strong>Year {row.year}</strong><span>{row.semester}</span><small>{row.academicYear}</small></div><div className="result-attendance"><span className="present"><b>{row.presentCount}</b> Present</span><span className="absent"><b>{row.absentCount}</b> Absent</span><span className="permission"><b>{row.permissionCount}</b> Permission</span></div><div className="result-course-grades">{row.grades.length ? row.grades.map(grade => <span key={grade.courseId}><strong>{grade.courseCode} {grade.score.toFixed(1)}/{grade.grade}</strong><small>{grade.name}</small></span>) : <i>No course grades yet</i>}</div><div className="result-total"><strong>{row.totalScore.toFixed(1)}</strong><span>{row.totalCourses}/5 courses</span></div><div className="result-average"><strong>{row.average.toFixed(2)}%</strong><span>Total ÷ 5</span></div><span className={`result-final result-${row.totalGrade.toLowerCase().replaceAll(" ", "-")}`}>{row.totalGrade}</span></article>;
}
function initials(value: string) { return value.split(" ").map(part => part[0]).join("").slice(0, 2).toUpperCase(); }
function semesterNumber(value: string) { return Number(value.match(/\d+/)?.[0] ?? 0); }
