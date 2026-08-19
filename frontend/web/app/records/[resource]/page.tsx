"use client";

import { Suspense, useCallback, useEffect, useMemo, useState } from "react";
import { useParams, useSearchParams } from "next/navigation";
import { api } from "@/lib/api";
import type { RecordItem } from "@/lib/types";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { Icon } from "@/components/icon";

const recordTypes: Record<string, { type: string; title: string; description: string }> = {
  students: { type: "Student", title: "Student history", description: "Immutable enrollment, profile, department, and status changes." },
  teachers: { type: "Teacher", title: "Teacher history", description: "Immutable faculty profile, assignment, and status changes." },
  classrooms: { type: "Classroom", title: "Classroom history", description: "Immutable room ownership, capacity, device, and status changes." },
  courses: { type: "Course", title: "Course history", description: "Immutable course, department, teacher, and capacity changes." },
  timetable: { type: "Timetable", title: "Timetable history", description: "Immutable class scheduling and cancellation history." },
  attendance: { type: "Attendance", title: "Attendance history", description: "Immutable attendance entries and correction history." },
  departments: { type: "Department", title: "Department history", description: "Immutable department and head-of-department changes." },
  grades: { type: "Grade", title: "Grade history", description: "Immutable grade submissions, updates, and removals." },
  "audit-logs": { type: "all", title: "Complete audit history", description: "Every recorded change across the institute in chronological order." },
};

export default function RecordsRoute() { return <Suspense fallback={<LoadingPage/>}><RecordHistory/></Suspense>; }

function RecordHistory() {
  const { resource } = useParams<{ resource: string }>(); const searchParams = useSearchParams(); const config = recordTypes[resource] ?? recordTypes["audit-logs"];
  const [query, setQuery] = useState(searchParams.get("q") ?? ""); const [rows, setRows] = useState<RecordItem[]>([]); const [error, setError] = useState(false); const [ready, setReady] = useState(false);
  const load = useCallback(() => api.records(query, config.type).then(data => { setRows(data); setReady(true); }).catch(() => setError(true)), [config.type, query]);
  useEffect(() => { const timer = setTimeout(load, 180); return () => clearTimeout(timer); }, [load]);
  const groups = useMemo(() => groupByDate(rows), [rows]);
  if (error) return <ErrorPage retry={load}/>; if (!ready) return <LoadingPage/>;
  return <>
    <PageHeading eyebrow="Immutable record center" title={config.title} description={config.description} actions={<button className="button secondary" onClick={() => exportCsv(rows)}><Icon name="archive" size={15}/> Export history</button>}/>
    <section className="record-lock-notice"><div><Icon name="archive" size={18}/></div><p><strong>Read-only historical record</strong><span>Entries here cannot be added, edited, or removed. Changes are made in Management and automatically recorded here.</span></p></section>
    <section className="record-toolbar panel"><div className="record-search"><Icon name="search" size={17}/><input value={query} onChange={event => setQuery(event.target.value)} placeholder={`Search ${config.title.toLowerCase()}…`}/></div><span className="record-count">{rows.length} historical entries</span></section>
    <RecordModuleSummary resource={resource} rows={rows}/>
    {rows.length ? <section className="history-groups">{Object.entries(groups).map(([date, entries]) => <article className="history-day" key={date}><div className="history-date"><strong>{date}</strong><span>{entries.length} changes</span></div><div className="history-timeline">{entries.map(entry => <HistoryEntry key={entry.id} entry={entry}/>)}</div></article>)}</section> : <section className="panel empty-state"><div className="empty-icon"><Icon name="archive" size={28}/></div><strong>No history found</strong><span>Matching management changes will appear here automatically.</span></section>}
  </>;
}

function RecordModuleSummary({ resource, rows }: { resource: string; rows: RecordItem[] }) {
  const latest = rows.slice(0, 6).map(row => ({ row, values: Object.fromEntries(parseDetails(row.details)) }));
  if (resource === "audit-logs") { const totals = rows.reduce<Record<string, number>>((result, row) => ({ ...result, [row.type]: (result[row.type] ?? 0) + 1 }), {}); return <section className="audit-coverage panel"><div><span>History coverage</span><strong>{Object.keys(totals).length} connected record types</strong></div><div>{Object.entries(totals).map(([type, count]) => <span key={type}><b>{count}</b>{type}</span>)}</div></section>; }
  if (resource === "students" || resource === "teachers") return <section className={`identity-history-grid history-${resource}`}>{latest.slice(0,4).map(({ row, values }) => <article className="panel" key={row.id}><div className="history-person-mark">{initials(String(values.name ?? row.subject))}</div><div><span>{values.number ?? (resource === "students" ? "Student" : "Teacher")}</span><h3>{values.name ?? row.subject}</h3><p>{values.department ?? "Relationship snapshot"}</p></div><strong>{row.action}</strong></article>)}</section>;
  if (resource === "classrooms") return <section className="room-history-strip">{latest.slice(0,4).map(({ row, values }) => <article className="panel" key={row.id}><span>{values.building ?? "Room history"}</span><h3>{values.code ?? row.subject}</h3><div><b>{values.capacity ?? "—"}</b> seats</div><small>Device {values.deviceOnline === "true" ? "online" : values.deviceOnline === "false" ? "offline" : "recorded"}</small><strong>{row.action}</strong></article>)}</section>;
  if (resource === "courses") return <section className="course-history-ledger panel">{latest.map(({ row, values }) => <article key={row.id}><span>{values.code ?? "COURSE"}</span><div><h3>{values.name ?? row.subject}</h3><p>{values.teacher ?? values.teacherId ?? "Teacher relationship recorded"}</p></div><small>{values.credits ? `${values.credits} credits` : "Revision"}</small><strong>{row.action}</strong></article>)}</section>;
  if (resource === "timetable") return <section className="schedule-history-board">{latest.map(({ row, values }) => <article className="panel" key={row.id}><time>{values.startsAt ?? new Date(row.date).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}</time><div><span>{values.dayOfWeek ?? "Schedule change"}</span><h3>{values.course ?? row.subject}</h3><p>{values.teacher ?? "Teacher"} · Room {values.classroom ?? values.classroomId ?? "recorded"}</p></div><strong>{row.action}</strong></article>)}</section>;
  if (resource === "attendance") { const counts = countActions(rows); return <section className="attendance-history-summary panel"><div className="attendance-history-counts">{["Present", "Late", "Absent", "Excused"].map(status => <div key={status}><strong>{counts[status] ?? 0}</strong><span>{status}</span></div>)}</div><div className="attendance-history-recent">{latest.slice(0,4).map(({ row, values }) => <div key={row.id}><time>{new Date(row.date).toLocaleDateString()}</time><strong>{values.student ?? row.subject}</strong><span>{values.status ?? row.action}</span></div>)}</div></section>; }
  if (resource === "departments") return <section className="department-history-tree">{latest.slice(0,4).map(({ row, values }) => <article className="panel" key={row.id}><div>{values.code ?? "DEPT"}</div><span>Department revision</span><h3>{values.name ?? row.subject}</h3><p>Head: {values.head ?? values.headTeacherId ?? "Recorded relationship"}</p><strong>{row.action}</strong></article>)}</section>;
  if (resource === "grades") return <section className="grade-history-board">{latest.map(({ row, values }) => { const score = Number(values.score); const letter = String(values.grade ?? (score >= 90 ? "A" : score >= 80 ? "B" : score >= 70 ? "C" : score >= 60 ? "D" : score >= 0 ? "F" : "•")); return <article className="panel" key={row.id}><span className={`grade-letter grade-${letter.toLowerCase()}`}>{letter}</span><div><h3>{values.student ?? row.subject}</h3><p>{values.course ?? "Grade relationship"} · {values.term ?? "Recorded term"}</p></div><b>{values.score ? `${values.score}%` : row.action}</b></article>; })}</section>;
  return null;
}

function HistoryEntry({ entry }: { entry: RecordItem }) { const [open, setOpen] = useState(false); const details = parseDetails(entry.details); return <div className={`history-entry history-${entry.type.toLowerCase()}`}><div className="history-node"><Icon name={entry.type === "Attendance" ? "check" : entry.type === "Grade" ? "grade" : entry.type === "Timetable" ? "calendar" : entry.type === "Department" ? "building" : "archive"} size={15}/></div><article className="panel"><div className="history-entry-main"><time>{new Date(entry.date).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}</time><div><span>{entry.type}</span><h3>{entry.subject}</h3><p>{entry.action}</p></div><span className={`history-action action-${entry.action.toLowerCase().replace(" ", "-")}`}>{entry.action}</span><button className="history-details-button" onClick={() => setOpen(value => !value)}>{open ? "Hide snapshot" : "View snapshot"}</button></div>{open && <div className="history-snapshot">{details.map(([key, value]) => key.toLowerCase().includes("photo") ? <div key={key}><span>{pretty(key)}</span><strong>4×6 photo stored</strong></div> : <div key={key}><span>{pretty(key)}</span><strong>{value}</strong></div>)}</div>}</article></div>; }

function parseDetails(details: string): [string, string][] { try { const value = JSON.parse(details) as Record<string, unknown>; return Object.entries(value).map(([key, item]) => [key, String(item ?? "—")]); } catch { return [["Details", details]]; } }
function pretty(value: string) { return value.replace(/([A-Z])/g, " $1").replace(/^./, first => first.toUpperCase()); }
function initials(name = "") { return name.split(" ").filter(Boolean).slice(0,2).map(part => part[0]).join("").toUpperCase(); }
function countActions(rows: RecordItem[]) { return rows.reduce<Record<string, number>>((result, row) => { const key = parseDetails(row.details).find(([name]) => name.toLowerCase() === "status")?.[1] ?? row.action; result[key] = (result[key] ?? 0) + 1; return result; }, {}); }
function groupByDate(rows: RecordItem[]) { return rows.reduce<Record<string, RecordItem[]>>((groups, row) => { const date = new Date(row.date).toLocaleDateString("en-US", { weekday: "long", day: "numeric", month: "long", year: "numeric" }); (groups[date] ??= []).push(row); return groups; }, {}); }
function exportCsv(rows: RecordItem[]) { const csv = ["Date,Type,Subject,Action,Details", ...rows.map(row => [row.date, row.type, row.subject, row.action, row.details].map(value => `"${String(value).replaceAll('"', '""')}"`).join(","))].join("\n"); const link = document.createElement("a"); link.href = URL.createObjectURL(new Blob([csv], { type: "text/csv" })); link.download = "institute-history.csv"; link.click(); URL.revokeObjectURL(link.href); }
