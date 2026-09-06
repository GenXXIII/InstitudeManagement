"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { recordApi } from "./record-api";
import type { OperationalRecord } from "./record-types";

const areas = [
  { module: "students", apiModule: "students", icon: "users", group: "People", title: "Students", detail: "Attendance, grades, and every semester through Year 4 Semester 2" },
  { module: "teachers", apiModule: "teachers", icon: "teacher", group: "People", title: "Teachers", detail: "Attendance, assigned courses, year levels, and completed classes" },
  { module: "class-sessions", apiModule: "sessions", icon: "calendar", group: "Teaching evidence", title: "Class sessions", detail: "Each completed timetable period with its student attendance" },
  { module: "timetable", apiModule: "timetable", icon: "calendar", group: "Teaching evidence", title: "Timetables", detail: "The course, teacher, classroom, year, day, and time that were used" },
  { module: "courses", apiModule: "courses", icon: "book", group: "Academic structure", title: "Courses", detail: "Enrollment, assigned teacher, capacity, and semester activity" },
  { module: "classrooms", apiModule: "classrooms", icon: "room", group: "Academic structure", title: "Classrooms", detail: "Assigned courses, year levels, completed classes, and availability" },
  { module: "departments", apiModule: "departments", icon: "building", group: "Academic structure", title: "Departments", detail: "Students, teachers, courses, classrooms, and teaching coverage" },
] as const;

const groups = ["People", "Teaching evidence", "Academic structure"] as const;

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
  const activityTotal = Object.values(data).flat().reduce((count, row) => count + row.activities.length, 0);
  const latest = areas.flatMap(area => data[area.module].map(row => ({ area, row })))
    .toSorted((left, right) => Date.parse(right.row.lastActivityAt ?? "") - Date.parse(left.row.lastActivityAt ?? ""))
    .slice(0, 6);

  return <div className="viewport-data-page history-control-overview-page record-control-overview-page">
    <PageHeading eyebrow="Academic evidence" title="Record Overview" description="Understand what is currently recorded, then open the exact people, teaching, or academic-structure register you need."/>
    <div className="record-overview-scroll">
      <section className="panel record-overview-guide">
        <div className="record-overview-guide-copy">
          <span>What Record means</span>
          <h2>Verified academic activity kept by semester</h2>
          <p>Enrollment says who and what is assigned. Operation captures what happened. Record brings that evidence together so it is easy to review.</p>
          <strong>{scopeLabel(departmentId, year)}</strong>
        </div>
        <div className="record-lifecycle-guide" aria-label="Record lifecycle">
          <LifecycleStep number="1" label="Enrollment" detail="Assignments"/>
          <Icon name="arrow" size={14}/>
          <LifecycleStep number="2" label="Operation" detail="Activity"/>
          <Icon name="arrow" size={14}/>
          <LifecycleStep number="3" label="Record" detail="Review now" active/>
          <Icon name="arrow" size={14}/>
          <LifecycleStep number="4" label="History" detail="Read-only archive"/>
        </div>
        <div className="record-retention-rules">
          <div><Icon name="users" size={16}/><p><strong>Students accumulate</strong><span>All semesters remain in Record until Year 4 Semester 2 is completed.</span></p></div>
          <div><Icon name="archive" size={16}/><p><strong>Other resources close by semester</strong><span>After semester close, their completed evidence moves to read-only History.</span></p></div>
          <Link href="/records/overview">Open History <Icon name="arrow" size={13}/></Link>
        </div>
      </section>

      <section className="record-overview-metrics">
        <RecordMetric icon="users" label="Student cycles" value={data.students.length} detail="Attendance and grade records" href={href("students", departmentId, year)}/>
        <RecordMetric icon="calendar" label="Class sessions" value={data["class-sessions"].length} detail="Completed teaching periods" href={href("class-sessions", departmentId, year)}/>
        <RecordMetric icon="book" label="All record registers" value={total} detail="Across seven connected modules" href={href("courses", departmentId, year)}/>
        <RecordMetric icon="archive" label="Evidence entries" value={activityTotal} detail="Recorded semester activities" href={href("students", departmentId, year)}/>
      </section>

      <div className="record-overview-main">
        <section className="panel record-resource-directory">
          <header><div><span>Browse Record</span><h2>Choose the information you want to understand</h2><p>Each register keeps its code connected to the original Management identity.</p></div></header>
          <div className="record-resource-groups">{groups.map(group => <section key={group}>
            <h3>{group}</h3>
            <div>{areas.filter(area => area.group === group).map(area => <Link href={href(area.module, departmentId, year)} key={area.module}>
              <span><Icon name={area.icon} size={17}/></span>
              <div><strong>{area.title}</strong><p>{area.detail}</p></div>
              <b>{data[area.module].length.toLocaleString()}<small> records</small></b>
              <Icon name="arrow" size={14}/>
            </Link>)}</div>
          </section>)}</div>
        </section>

        <aside className="panel record-recent-evidence">
          <header><div><span>Latest evidence</span><h2>Recently updated records</h2></div></header>
          <div>{latest.map(({ area, row }) => <Link href={detailHref(area.module, row.id, departmentId, year)} key={`${area.module}-${row.id}`}>
            <span><Icon name={area.icon} size={15}/></span>
            <div><small>{area.title}</small><strong>{row.subject}</strong><p>{row.identifier || row.code}</p></div>
            <time>{row.lastActivityAt ? relativeDate(row.lastActivityAt) : "No activity"}</time>
          </Link>)}</div>
          {!latest.length && <div className="record-overview-empty"><Icon name="archive" size={22}/><strong>No records in this scope</strong><span>Completed enrollment and operation activity will appear here.</span></div>}
        </aside>
      </div>
    </div>
  </div>;
}

function RecordMetric({ icon, label, value, detail, href: path }: { icon: Parameters<typeof Icon>[0]["name"]; label: string; value: number; detail: string; href: string }) {
  return <Link className="panel record-overview-metric" href={path}><span><Icon name={icon} size={17}/></span><div><small>{label}</small><strong>{value.toLocaleString()}</strong><p>{detail}</p></div><Icon name="arrow" size={14}/></Link>;
}

function LifecycleStep({ number, label, detail, active = false }: { number: string; label: string; detail: string; active?: boolean }) {
  return <div className={active ? "active" : ""}><b>{number}</b><p><strong>{label}</strong><span>{detail}</span></p></div>;
}

function href(module: string, departmentId: string, year: string) { const params = new URLSearchParams(); if (departmentId) params.set("departmentId", departmentId); if (year) params.set("year", year); return `/record/${module}${params.size ? `?${params}` : ""}`; }
function detailHref(module: string, id: string, departmentId: string, year: string) { const base = href(module, departmentId, year); const [path, query] = base.split("?"); return `${path}/${encodeURIComponent(id)}${query ? `?${query}` : ""}`; }
function matchesYear(row: OperationalRecord, year: string) { return !year || JSON.stringify(row).toLowerCase().includes(`year ${year}`); }
function scopeLabel(departmentId: string, year: string) { return `${departmentId ? "Selected department" : "All departments"} · ${year ? `Year ${year}` : "All year levels"}`; }
function relativeDate(value: string) { const date = new Date(value); return Number.isNaN(date.valueOf()) ? "Recorded" : date.toLocaleDateString(undefined, { month: "short", day: "numeric" }); }
