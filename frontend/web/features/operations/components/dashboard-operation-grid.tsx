import Link from "next/link";
import { Icon } from "@/components/icon";
import type { Operation } from "../operations-types";
import { statusClass } from "../operation-utils";
import { workflowCode } from "@/lib/workflow-code";

const icons = { Students: "users", Teachers: "teacher", Classrooms: "room", Courses: "book" } as const;

export function DashboardOperationGrid({ data, departmentId, year }: { data: Operation; departmentId: string; year: number }) {
  const summaries = data.summary ?? [];
  const params = new URLSearchParams(); if (departmentId) params.set("departmentId", departmentId); if (year) params.set("year", String(year));
  const scope = params.size ? `?${params}` : "";
  const areas = [
    { name: "Students", route: "/operation/students", rows: (data.students ?? []).slice(0, 6).map(row => ({ title: row.student, detail: `${workflowCode(row.studentCode, "student", "enrollment")} · Year ${row.year} · ${row.shift}`, state: row.attendanceStatus })) },
    { name: "Teachers", route: "/operation/teachers", rows: (data.teachers ?? []).slice(0, 6).map(row => ({ title: row.teacher, detail: `${workflowCode(row.teacherCode, "teacher", "enrollment")} · ${row.department}`, state: row.status })) },
    { name: "Classrooms", route: "/operation/classrooms", rows: (data.classrooms ?? []).filter(row => row.status === "Running").slice(0, 6).map(row => ({ title: workflowCode(row.room, "classroom", "enrollment"), detail: `${row.roomType} · ${row.capacity} seats`, state: row.status })) },
    { name: "Courses", route: "/operation/courses", rows: (data.courses ?? []).slice(0, 6).map(row => ({ title: row.course, detail: `${workflowCode(row.courseCode, "course", "enrollment")} · ${row.teacher}`, state: row.status })) },
  ];
  return <div className="unified-operation-dashboard">
    <div className="unified-operation-summary">{summaries.map(row => <div className={`unified-summary-cell tone-${row.tone}`} key={row.module}><span><Icon name={icons[row.module as keyof typeof icons] ?? "dashboard"} size={18}/></span><div><small>{row.module}</small><strong>{row.value}</strong><p>{row.detail}</p></div><b className={`table-status ${statusClass(row.status)}`}>{row.status}</b></div>)}</div>
    <div className="unified-operation-board">
      <header>{areas.map(area => <div key={area.name}><span><Icon name={icons[area.name as keyof typeof icons]} size={17}/>{area.name} working now</span><Link href={`${area.route}${scope}`}>Full view →</Link></div>)}</header>
      <div className="unified-operation-columns">{areas.map(area => <section key={area.name}>{area.rows.length ? area.rows.map((row, index) => <article key={`${area.name}-${index}`}><i/><div><strong>{row.title}</strong><span>{row.detail}</span></div><b className={`table-status ${statusClass(row.state)}`}>{row.state}</b></article>) : <div className="unified-operation-empty">No current {area.name.toLowerCase()} data</div>}</section>)}</div>
    </div>
    <footer className="unified-operation-note"><Icon name="pulse" size={16}/><div><strong>One overview, four enrollment-powered operational areas</strong><span>Only active Enrollment relationships enter Operation. Management records that are not enrolled do not appear here.</span></div></footer>
  </div>;
}
