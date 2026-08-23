"use client";

import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { OperationalRecordRow } from "./components/operational-record-row";
import { recordApi } from "./record-api";
import type { OperationalRecord } from "./record-types";

const modules: Record<string, { title: string; description: string; singular: string }> = {
  sessions: { title: "Completed timetable sessions", description: "One card per completed timetable occurrence, identified by its date, scheduled time, and room. Expand a card to see the course, teacher, cohort, and frozen attendance.", singular: "session" },
  students: { title: "Student timetable attendance", description: "One card per ended timetable period. Expand it to see every student who was learning in that class and their frozen attendance status.", singular: "timetable session" },
  teachers: { title: "Teacher timetable sessions", description: "One card per ended timetable period, showing the scheduled teacher and every student recorded in that class.", singular: "timetable session" },
  courses: { title: "Course timetable sessions", description: "One card per ended timetable period, with the scheduled course as supporting detail and the full class attendance inside.", singular: "timetable session" },
  classrooms: { title: "Classroom timetable sessions", description: "Completed teaching work grouped by classroom, with its course, teacher, cohort, and frozen attendance evidence.", singular: "classroom record" },
};

export function OperationalRecordWorkspace({ module: rawModule, history = false }: { module: string; history?: boolean }) {
  const currentModule = rawModule in modules ? rawModule : "students";
  const config = modules[currentModule];
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [query, setQuery] = useState(searchParams.get("q") ?? "");
  const [status, setStatus] = useState("all");
  const [rows, setRows] = useState<OperationalRecord[]>([]);
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);
  const load = useCallback(() => recordApi.get(currentModule, query, departmentId, history).then(data => { setRows(data); setReady(true); setError(false); }).catch(() => setError(true)), [currentModule, departmentId, history, query]);
  useEffect(() => { const timer = window.setTimeout(load, 180); return () => window.clearTimeout(timer); }, [load]);
  useEffect(() => { const timer = window.setTimeout(() => setQuery(searchParams.get("q") ?? ""), 0); return () => window.clearTimeout(timer); }, [searchParams]);

  const yearRows = useMemo(() => rows
    .filter(row => !year || JSON.stringify(row).toLowerCase().includes(`year ${year}`))
    .toSorted((left, right) => recordYear(left) - recordYear(right) || Date.parse(right.lastActivityAt ?? "") - Date.parse(left.lastActivityAt ?? "")), [rows, year]);
  const statuses = useMemo(() => [...new Set(yearRows.map(row => row.status))].sort(), [yearRows]);
  const visible = history || status === "all" ? yearRows : yearRows.filter(row => row.status === status);
  const detailQuery = searchParams.toString();
  const detailHref = (id: string) => `${history ? "/record-history" : "/record"}/${currentModule}/${encodeURIComponent(id)}${detailQuery ? `?${detailQuery}` : ""}`;
  const activityCount = yearRows.reduce((total, row) => total + row.activities.length, 0);
  if (error) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;

  return <div className="viewport-data-page record-viewport-page">
    <PageHeading eyebrow={history ? "Closed-semester record history" : "Active-semester automatic records"} title={history ? `${config.title} history` : config.title} description={`${history ? "Read-only completed work from semesters before the active institute semester. " : "This page resets to the new active-semester view when the institute semester advances. "}${config.description}${year ? ` Showing Year ${year}.` : ""}`}/>
    <section className="operational-record-summary"><article className="panel"><span>All {config.singular}s</span><strong>{yearRows.length.toLocaleString()}</strong><small>Completed timetable evidence</small></article><article className="panel"><span>Recorded activities</span><strong>{activityCount.toLocaleString()}</strong><small>Completed classes and attendance only</small></article><article className="panel"><span>With activity</span><strong>{yearRows.filter(row => row.activities.length > 0).length.toLocaleString()}</strong><small>Expandable timetable records</small></article></section>
    <section className="record-toolbar panel"><label className="record-search"><Icon name="search" size={17}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder={`Search ${config.title.toLowerCase()}…`} aria-label={`Search ${config.title}`}/></label>{!history && <select value={status} onChange={event => setStatus(event.target.value)} aria-label="Operational record status"><option value="all">All statuses</option>{statuses.map(value => <option value={value} key={value}>{value}</option>)}</select>}<span className="record-count">Showing {visible.length} of {yearRows.length}</span></section>
    {visible.length
      ? <section className="session-record-list">{visible.map(row => <OperationalRecordRow row={row} editable={!history} showStatus={!history} onUpdated={load} detailHref={detailHref(row.id)} key={row.id}/>)}</section>
      : <section className="panel empty-state"><div className="empty-icon"><Icon name="archive" size={28}/></div><strong>{history ? "No closed-semester records found" : "No active-semester records found"}</strong><span>{history ? "Records move here automatically when the configured semester advances." : "Records appear automatically when a scheduled timetable period ends."}</span></section>}
  </div>;
}

function recordYear(record: OperationalRecord) {
  const match = JSON.stringify(record).match(/Year\s+([1-4])/i);
  return match ? Number(match[1]) : 99;
}
