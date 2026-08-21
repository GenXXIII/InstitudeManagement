"use client";

import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { OperationalRecordRow } from "./components/operational-record-row";
import { recordApi } from "./record-api";
import type { OperationalRecord } from "./record-types";

const modules: Record<string, { title: string; description: string; singular: string }> = {
  students: { title: "Student operational records", description: "Read-only attendance and assessment activity recorded for every student, including records created while they were active or inactive.", singular: "student" },
  teachers: { title: "Teacher operational records", description: "Read-only course assignments and complete timetable activity showing what every teacher has worked on.", singular: "teacher" },
  classrooms: { title: "Classroom operational records", description: "Read-only timetable usage showing every course and teacher recorded against each classroom.", singular: "classroom" },
  courses: { title: "Course operational records", description: "Read-only timetable delivery and assessment activity recorded for every course.", singular: "course" },
};

export function OperationalRecordWorkspace({ module: rawModule }: { module: string }) {
  const currentModule = rawModule in modules ? rawModule : "students";
  const config = modules[currentModule];
  const departmentId = useSearchParams().get("departmentId") ?? "";
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("all");
  const [rows, setRows] = useState<OperationalRecord[]>([]);
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(false);
  const load = useCallback(() => recordApi.get(currentModule, query, departmentId).then(data => { setRows(data); setReady(true); setError(false); }).catch(() => setError(true)), [currentModule, departmentId, query]);
  useEffect(() => { const timer = window.setTimeout(load, 180); return () => window.clearTimeout(timer); }, [load]);

  const statuses = useMemo(() => [...new Set(rows.map(row => row.status))].sort(), [rows]);
  const visible = status === "all" ? rows : rows.filter(row => row.status === status);
  const activityCount = rows.reduce((total, row) => total + row.activities.length, 0);
  if (error) return <ErrorPage retry={load}/>;
  if (!ready) return <LoadingPage/>;

  return <><PageHeading eyebrow="Recorded institute operations" title={config.title} description={config.description}/><section className="operational-record-summary"><article className="panel"><span>All {config.singular}s</span><strong>{rows.length.toLocaleString()}</strong><small>Every current and former record</small></article><article className="panel"><span>Recorded activities</span><strong>{activityCount.toLocaleString()}</strong><small>Timetable, attendance, and assessment entries</small></article><article className="panel"><span>With activity</span><strong>{rows.filter(row => row.activities.length > 0).length.toLocaleString()}</strong><small>Expandable operational timelines</small></article></section><section className="record-toolbar panel"><label className="record-search"><Icon name="search" size={17}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder={`Search ${config.title.toLowerCase()}…`} aria-label={`Search ${config.title}`}/></label><select value={status} onChange={event => setStatus(event.target.value)} aria-label="Operational record status"><option value="all">All statuses</option>{statuses.map(value => <option value={value} key={value}>{value}</option>)}</select><span className="record-count">Showing {visible.length} of {rows.length}</span></section>{visible.length ? <section className="panel operational-record-table"><div className="operational-record-head"><span>{config.singular}</span><span>Recorded work summary</span><span>Status</span><span>Last activity</span><span>Details</span></div>{visible.map(row => <OperationalRecordRow row={row} key={row.id}/>)}</section> : <section className="panel empty-state"><div className="empty-icon"><Icon name="archive" size={28}/></div><strong>No operational records found</strong><span>Try another search or status.</span></section>}</>;
}
