"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { recordApi } from "./record-api";
import type { OperationalRecord } from "./record-types";

const areas = [
  { module: "students", apiModule: "students", icon: "users", code: "RSTU-XX", title: "Student Record", detail: "Accumulated semesters from enrollment through Year 4 Semester 2" },
  { module: "teachers", apiModule: "teachers", icon: "teacher", code: "RTEA-XX", title: "Teacher Record", detail: "Teacher attendance, assigned courses, and recorded periods" },
  { module: "class-sessions", apiModule: "sessions", icon: "calendar", code: "RSES-XX", title: "Class Sessions", detail: "Completed enrolled timetable sessions and attendance" },
  { module: "courses", apiModule: "courses", icon: "book", code: "RCOU-XX", title: "Course Record", detail: "Enrolled students, assigned teacher, and semester activity" },
  { module: "classrooms", apiModule: "classrooms", icon: "room", code: "RCLA-XX", title: "Classroom Record", detail: "Assigned courses, running sessions, and availability" },
  { module: "timetable", apiModule: "timetable", icon: "calendar", code: "RTIM-XX", title: "Timetable Record", detail: "Course, teacher, classroom, year, day, and time" },
  { module: "departments", apiModule: "departments", icon: "building", code: "RDEP-XX", title: "Department Record", detail: "Current-semester enrollment and operation coverage" },
] as const;

type RecordCollections = Record<string, OperationalRecord[]>;

export function RecordOverview() {
  const searchParams = useSearchParams();
  const departmentId = searchParams.get("departmentId") ?? "";
  const year = searchParams.get("year") ?? "";
  const [data, setData] = useState<RecordCollections>();
  const [error, setError] = useState(false);

  const load = useCallback(async () => {
    try {
      const rows = await Promise.all(areas.map(area => recordApi.get(area.apiModule, "", departmentId)));
      setData(Object.fromEntries(areas.map((area, index) => [area.module, rows[index].filter(row => matchesYear(row, year))])));
      setError(false);
    } catch { setError(true); }
  }, [departmentId, year]);
  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);

  if (error) return <ErrorPage retry={load}/>;
  if (!data) return <LoadingPage/>;
  const total = Object.values(data).reduce((count, rows) => count + rows.length, 0);

  return <div className="viewport-data-page history-control-overview-page">
    <PageHeading eyebrow="Accumulating academic evidence" title="Record Overview" description="Each new semester is added above the earlier semesters. A student leaves Record only after completing Year 4 Semester 2, when the complete read-only archive moves to History."/>
    <div className="history-control-overview-scroll">
      <section className="enrollment-overview-metrics">
        <RecordMetric icon="users" label="Student records" value={data.students.length} detail="Attendance and grades" href={href("students", departmentId, year)}/>
        <RecordMetric icon="teacher" label="Teacher records" value={data.teachers.length} detail="Attendance and classes" href={href("teachers", departmentId, year)}/>
        <RecordMetric icon="calendar" label="Completed sessions" value={data["class-sessions"].length} detail="Operation evidence" href={href("class-sessions", departmentId, year)}/>
        <RecordMetric icon="archive" label="All retained records" value={total} detail="Current and earlier semesters" href={href("students", departmentId, year)}/>
      </section>
      <section className="panel history-data-map">
        <header><div><span>Workflow stage</span><h2>Record grows from Enrollment and Operation</h2><p>Management defines the source, Enrollment links it, Operation performs it, and Record keeps every semester until Year 4 Semester 2 graduation creates permanent History.</p></div></header>
        <div>{areas.map(area => <Link href={href(area.module, departmentId, year)} key={area.module}><span><Icon name={area.icon} size={17}/></span><div><small>{area.code}</small><strong>{area.title}</strong><p>{area.detail}</p></div><b>{data[area.module].length}</b></Link>)}</div>
      </section>
    </div>
  </div>;
}

function RecordMetric({ icon, label, value, detail, href: path }: { icon: Parameters<typeof Icon>[0]["name"]; label: string; value: number; detail: string; href: string }) {
  return <Link className="panel enrollment-overview-metric" href={path}><span className="complete"><Icon name={icon} size={17}/></span><div><small>{label}</small><strong>{value.toLocaleString()}</strong><p>{detail}</p></div><Icon name="arrow" size={14}/></Link>;
}

function href(module: string, departmentId: string, year: string) { const params = new URLSearchParams(); if (departmentId) params.set("departmentId", departmentId); if (year) params.set("year", year); return `/record/${module}${params.size ? `?${params}` : ""}`; }
function matchesYear(row: OperationalRecord, year: string) { return !year || JSON.stringify(row).toLowerCase().includes(`year ${year}`); }
