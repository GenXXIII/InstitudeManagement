"use client";

import { useParams, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { api } from "@/lib/api";

const operationPanelTitles: Record<string, string> = {
  "control-room": "Current class activity",
  students: "Live student roster",
  teachers: "Faculty availability",
  classrooms: "Classroom status board",
  courses: "Active course delivery",
  timetable: "Live timetable",
  attendance: "Attendance register",
  departments: "Department coverage",
  grades: "Current gradebook",
};
import type { Operation } from "@/lib/types";
import { ActivityList, DataTable, ErrorPage, LoadingPage, MetricCards, PageHeading } from "@/components/page-primitives";
import { Icon } from "@/components/icon";

export default function OperationPage() {
  const { module } = useParams<{ module: string }>(); const searchParams = useSearchParams(); const departmentId = searchParams.get("departmentId") ?? ""; const [data, setData] = useState<Operation>(); const [error, setError] = useState(false);
  const load = useCallback(() => { api.operation(module, departmentId).then(result => { setData(result); setError(false); }).catch(() => setError(true)); }, [module, departmentId]);
  useEffect(load, [load]);
  if (error) return <ErrorPage retry={load}/>; if (!data) return <LoadingPage/>;
  return <>
    <PageHeading eyebrow="Live operation" title={data.title} description={data.description} actions={<><span className="live-pill"><i/> Auto-refresh on</span><button className="button primary" onClick={load}><Icon name="pulse" size={16}/> Refresh live data</button></>}/>
    <MetricCards metrics={data.metrics}/>
    <section className="operation-layout">
      <article className={`panel data-panel operation-${data.module}`}><div className="panel-title"><div><span className="panel-kicker">Live data</span><h3>{operationPanelTitles[data.module] ?? "Operational activity"}</h3></div><span className="updated">Updated just now</span></div><OperationData module={data.module} rows={data.rows}/></article>
      <div className="operation-side"><article className="panel"><div className="panel-title"><div><span className="panel-kicker">Requires action</span><h3>Attention</h3></div><span className="count-badge">{data.attention.length}</span></div><ActivityList items={data.attention}/></article><article className="panel"><div className="panel-title"><div><span className="panel-kicker">Stream</span><h3>Recent activity</h3></div></div><ActivityList items={data.activity}/></article></div>
    </section>
  </>;
}

function OperationData({ module, rows }: { module: string; rows: Record<string, string>[] }) {
  if (module === "control-room") return <div className="control-room-board">{rows.map((row, index) => <article key={index}><div className="control-room-live"><i/><span>Live room</span></div><strong>{row.Room}</strong><h4>{row.Course}</h4><p>{row.Teacher}</p><div><time>{row.Time}</time><span className={`table-status ${row.Status?.toLowerCase()}`}>{row.Status}</span></div></article>)}</div>;
  if (module === "students") return <div className="student-presence-board"><div className="student-presence-head"><span>Photo</span><span>Student and department</span><span>Year level</span><span>Live status</span></div>{rows.map((row, index) => <article className="student-presence-row" key={index}><span className="initial-chip">{initials(row.Student)}</span><div><strong title={row.Student}>{row.Student}</strong><small title={`${row.ID} · ${row.Department}`}>{row.ID} · {row.Department}</small></div><b>Year {row.Year}</b><span className={`table-status ${row.Status?.toLowerCase()}`}>{row.Status}</span></article>)}</div>;
  if (module === "teachers") return <div className="faculty-availability-board">{rows.map((row, index) => <article key={index}><div className="faculty-avatar">{initials(row.Teacher)}</div><div><span>{row.ID}</span><h4>{row.Teacher}</h4><p>{row.Department}</p></div><div className={`faculty-state state-${row.Status?.toLowerCase().replace(" ", "-")}`}><i/><strong>{row.Status}</strong><small>{row.Status === "Teaching" ? "Currently in class" : row.Status === "Available" ? "Ready for assignment" : "Faculty schedule"}</small></div></article>)}</div>;
  if (module === "classrooms") return <div className="live-room-grid">{rows.map((row, index) => <div key={index}><div><span>{row.Building}</span><strong>{row.Room}</strong></div><span className={`table-status ${row.Status?.toLowerCase()}`}>{row.Status}</span><p><b>{row.Capacity}</b> seats</p><small>Attendance device · {row.Device}</small></div>)}</div>;
  if (module === "courses") return <div className="live-course-list">{rows.map((row, index) => <div key={index}><span className="course-code">{row.Code}</span><div><strong>{row.Course}</strong><small>{row.Department} · {row.Teacher}</small></div><b>{row.Capacity} seats</b><span className={`table-status ${row.Status?.toLowerCase()}`}>{row.Status}</span></div>)}</div>;
  if (module === "timetable") return <div className="live-timetable-list">{rows.map((row, index) => <div key={index}><time>{row.Time}</time><i/><div><strong>{row.Course}</strong><span>{row.Teacher} · Room {row.Room}</span></div><span className={`table-status ${row.Status?.toLowerCase()}`}>{row.Status}</span></div>)}</div>;
  if (module === "attendance") return <div className="live-scan-list">{rows.map((row, index) => <div key={index}><span className="scan-time">{row.Time}</span><span className="initial-chip">{initials(row.Student)}</span><div><strong>{row.Student}</strong><small>{row.ID} · {row.Method}</small></div><span className={`table-status ${row.Status?.toLowerCase()}`}>{row.Status}</span></div>)}</div>;
  if (module === "departments") return <div className="live-department-list">{rows.map((row, index) => <div key={index}><div><strong>{row.Department}</strong><span>{row.Head}</span></div><div className="department-mini-counts"><span><b>{row.Students}</b> students</span><span><b>{row.Teachers}</b> teachers</span><span><b>{row.Courses}</b> courses</span></div><span className={`table-status ${row.Status?.toLowerCase()}`}>{row.Status}</span></div>)}</div>;
  if (module === "grades") return <div className="live-grade-list">{rows.map((row, index) => <div key={index}><span className={`grade-letter grade-${row.Grade?.charAt(0).toLowerCase()}`}>{row.Grade}</span><div><strong>{row.Student}</strong><small>{row.Course} · {row.Term}</small></div><b>{row.Score}%</b></div>)}</div>;
  return <DataTable rows={rows}/>;
}

function initials(name = "") { return name.split(" ").filter(Boolean).slice(0, 2).map(part => part[0]).join("").toUpperCase(); }
