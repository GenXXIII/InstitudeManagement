"use client";

import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Icon } from "@/components/icon";
import { DataPagination, useDataPagination } from "@/components/data-pagination";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { OperationalRecordRow } from "./components/operational-record-row";
import { EntitySemesterRecordHeader } from "./components/entity-semester-record";
import { StudentSemesterRecordHeader } from "./components/student-semester-record";
import { recordApi } from "./record-api";
import type { OperationalRecord } from "./record-types";

const modules: Record<string, { title: string; description: string; singular: string }> = {
  sessions: { title: "Completed timetable sessions", description: "One card per completed timetable occurrence, identified by its date, scheduled time, and room. Expand a card to see the course, teacher, cohort, and frozen attendance.", singular: "session" },
  students: { title: "Student semester records", description: "Attendance, five course grades, and the semester result for every student. Open a row for full class-session and grade details.", singular: "student" },
  teachers: { title: "Teacher semester records", description: "Teacher identity, attendance state, and student attendance from every completed class. Open a row for full session details.", singular: "teacher" },
  courses: { title: "Course semester records", description: "Course identity, department, live In Study or Available state, and completed-class attendance evidence.", singular: "course" },
  classrooms: { title: "Classroom semester records", description: "Classroom identity, building, live In Study or Available state, and every completed class held in the room.", singular: "classroom" },
};

export function OperationalRecordWorkspace({ module: rawModule, history = false }: { module: string; history?: boolean }) {
  const currentModule = rawModule in modules ? rawModule : "students";
  const config = modules[currentModule];
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [rows, setRows] = useState<OperationalRecord[]>([]);
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);
  const load = useCallback(() => recordApi.get(currentModule, query, departmentId, history).then(data => { setRows(data); setReady(true); setError(false); }).catch(() => setError(true)), [currentModule, departmentId, history, query]);
  useEffect(() => { const timer = window.setTimeout(load, 180); return () => window.clearTimeout(timer); }, [load]);
  useEffect(() => { const timer = window.setTimeout(() => setQuery(searchParams.get("q") ?? ""), 0); return () => window.clearTimeout(timer); }, [searchParams]);

  const yearRows = useMemo(() => rows
    .filter(row => !year || JSON.stringify(row).toLowerCase().includes(`year ${year}`))
    .toSorted((left, right) => recordYear(left) - recordYear(right) || Date.parse(right.lastActivityAt ?? "") - Date.parse(left.lastActivityAt ?? "")), [rows, year]);
  const visible = yearRows;
  const pagination = useDataPagination(visible, `${currentModule}-${history}-${departmentId}-${year}-${query}`);
  const detailQuery = searchParams.toString();
  const detailHref = (id: string) => `${history ? "/record-history" : "/record"}/${currentModule}/${encodeURIComponent(id)}${detailQuery ? `?${detailQuery}` : ""}`;
  const activityCount = yearRows.reduce((total, row) => total + row.activities.length, 0);
  if (error) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;

  return <div className="viewport-data-page record-viewport-page">
    <PageHeading eyebrow={history ? "Read-only semester history" : "Active-semester records"} title={history ? `${config.title} history` : config.title} description={`${history ? "Closed semesters remain permanent and read-only in History. " : "When the semester advances, this view resets while its old data remains in History. "}${config.description}${year ? ` Showing Year ${year}.` : ""}`}/>
    <section className="operational-record-summary"><article className="panel"><span>All {config.singular}s</span><strong>{yearRows.length.toLocaleString()}</strong><small>Completed timetable evidence</small></article><article className="panel"><span>Recorded activities</span><strong>{activityCount.toLocaleString()}</strong><small>Completed classes and attendance only</small></article><article className="panel"><span>With activity</span><strong>{yearRows.filter(row => row.activities.length > 0).length.toLocaleString()}</strong><small>Expandable timetable records</small></article></section>
    <section className="record-toolbar panel"><label className="record-search"><Icon name="search" size={17}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder={`Search ${config.title.toLowerCase()}…`} aria-label={`Search ${config.title}`}/></label><span className="record-count">Showing {visible.length} of {yearRows.length}</span></section>
    {visible.length
      ? <section className="record-paginated-region">{currentModule === "students"
        ? <div className="student-semester-record-ledger panel"><StudentSemesterRecordHeader/><div>{pagination.pageItems.map(row => <OperationalRecordRow row={row} editable={!history} showStatus={false} onUpdated={load} detailHref={detailHref(row.id)} key={row.id}/>)}</div></div>
        : currentModule !== "sessions"
          ? <div className="entity-semester-record-ledger panel"><EntitySemesterRecordHeader module={moduleName(currentModule)}/><div>{pagination.pageItems.map(row => <OperationalRecordRow row={row} editable={false} showStatus={false} detailHref={detailHref(row.id)} key={row.id}/>)}</div></div>
          : <div className="session-record-list">{pagination.pageItems.map(row => <OperationalRecordRow row={row} editable={!history} showStatus={!history} onUpdated={load} detailHref={detailHref(row.id)} key={row.id}/>)}</div>}<DataPagination page={pagination.page} pageCount={pagination.pageCount} total={visible.length} onPage={pagination.setPage}/></section>
      : <section className="panel empty-state"><div className="empty-icon"><Icon name="archive" size={28}/></div><strong>{history ? "No semester records found" : "No active-semester records found"}</strong><span>{history ? "Current records appear here read-only and remain permanently after semester rollover." : "Records appear automatically when a scheduled timetable period ends or a grade is assigned."}</span></section>}
  </div>;
}

function recordYear(record: OperationalRecord) {
  const match = JSON.stringify(record).match(/Year\s+([1-4])/i);
  return match ? Number(match[1]) : 99;
}

function moduleName(module: string): "Teacher" | "Course" | "Classroom" { return module === "teachers" ? "Teacher" : module === "courses" ? "Course" : "Classroom"; }
