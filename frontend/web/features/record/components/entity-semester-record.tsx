"use client";

import Image from "next/image";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/icon";
import type { OperationalRecord } from "../record-types";

type RecordModule = "Teacher" | "Course" | "Classroom";
type Counts = { present: number; permission: number; absent: number };

export function EntitySemesterRecordHeader({ module }: { module: RecordModule }) {
  return <div className="entity-semester-record-head"><span>{module} code</span><span>{module === "Teacher" ? "Photo" : "Type"}</span><span>{module === "Classroom" ? "Classroom" : `${module} name`}</span><span>{module === "Classroom" ? "Building" : "Department"}</span><span>Student attendance</span><span>{module === "Teacher" ? "Teacher attendance" : "Current state"}</span><span>Semester activity</span></div>;
}

export function EntitySemesterRecord({ row, detailHref, detailPage = false }: { row: OperationalRecord; detailHref?: string; detailPage?: boolean }) {
  const router = useRouter();
  const counts = attendanceCounts(row.activities);
  const teacherCounts = teacherAttendanceCounts(row.activities);
  if (detailPage) return <EntityRecordDetail row={row} counts={counts}/>;
  const open = () => { if (detailHref) router.push(detailHref); };
  return <article className="entity-semester-record-row record-row-clickable" role="link" tabIndex={0} onClick={open} onKeyDown={event => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); open(); } }}>
    <strong className="entity-record-code">{row.code || row.identifier}</strong>
    <EntityVisual row={row}/>
    <div className="entity-record-name"><strong>{row.subject}</strong><span>{row.identifier}</span></div>
    <div className="entity-record-department"><strong>{row.department || "Unassigned"}</strong><span>{row.academicYear} · {row.term}</span></div>
    <AttendanceCards counts={counts}/>
    {row.module === "Teacher" ? <TeacherAttendanceCards counts={teacherCounts}/> : <StateCards module={row.module as RecordModule} status={row.status}/>}
    <div className="entity-record-activity"><strong>{row.activities.length}</strong><span>completed classes</span><small>{row.lastActivityAt ? new Date(row.lastActivityAt).toLocaleDateString() : "No activity"}</small></div>
  </article>;
}

function EntityRecordDetail({ row, counts }: { row: OperationalRecord; counts: Counts }) {
  const sessions = row.activities.filter(activity => activity.Activity === "Completed class");
  const teacherCounts = teacherAttendanceCounts(sessions);
  return <article className="entity-semester-detail">
    <header><EntityVisual row={row}/><div><span className="eyebrow">{row.academicYear} · {row.term}</span><h2>{row.subject}</h2><p>{row.code} · {row.department} · {row.identifier}</p></div></header>
    <div className="entity-detail-insights"><section><span>Student attendance</span><AttendanceCards counts={counts}/></section><section><span>{row.module === "Teacher" ? "Teacher attendance" : "Current state"}</span>{row.module === "Teacher" ? <TeacherAttendanceCards counts={teacherCounts}/> : <StateCards module={row.module as RecordModule} status={row.status}/>}</section><section className="entity-semester-total"><span>Semester activity</span><strong>{sessions.length}</strong><small>completed classes</small></section></div>
    <section className="entity-session-register"><header><div><strong>Full class-session detail</strong><span>Course, teacher, classroom, cohort attendance, and student snapshot</span></div><b>{sessions.length} sessions</b></header><div>{sessions.length ? sessions.map((session, index) => <EntitySessionCard session={session} module={row.module as RecordModule} key={`${session.Date}-${session.Time}-${index}`}/>) : <p className="student-record-detail-empty">No completed class sessions in this semester.</p>}</div></section>
  </article>;
}

function EntitySessionCard({ session, module }: { session: Record<string, string>; module: RecordModule }) {
  const counts = activityCounts(session);
  const students = studentSnapshot(session.Students);
  const title = module === "Course" ? session.Teacher : session.Course;
  const context = module === "Classroom" ? `${session.Teacher} · ${session.Year}` : `Room ${session.Classroom} · ${session.Year}${module === "Teacher" ? ` · Teacher ${session["Teacher attendance"] || "Present"}` : ""}`;
  return <article className="entity-session-card"><div className="entity-session-main"><div><time>{session.Date}</time><strong>{session.Time}</strong></div><div><strong>{title || "Completed class"}</strong><span>{context}</span></div><AttendanceCards counts={counts}/></div>{students.length > 0 && <details><summary>View {students.length} student attendance details</summary><div className="entity-student-snapshot">{students.map((student, index) => <span className={`attendance-${toneKey(student.status)}`} key={`${student.name}-${index}`}><strong>{student.name}</strong><b>{attendanceLabel(student.status)}</b></span>)}</div></details>}</article>;
}

function EntityVisual({ row }: { row: OperationalRecord }) {
  if (row.module === "Teacher" && row.photoDataUrl) return <Image className="entity-record-photo" src={row.photoDataUrl} alt={`${row.subject} portrait`} width={42} height={58} unoptimized/>;
  if (row.module === "Teacher") return <span className="entity-record-photo entity-record-fallback">{initials(row.subject)}</span>;
  return <span className={`entity-record-type entity-type-${row.module.toLowerCase()}`}><Icon name={row.module === "Classroom" ? "room" : "book"} size={17}/><small>{row.module === "Classroom" ? roomType(row.identifier) : "Course"}</small></span>;
}

function AttendanceCards({ counts }: { counts: Counts }) { return <div className="entity-attendance-cards"><span className="present"><b>{counts.present}</b><small>Present</small></span><span className="permission"><b>{counts.permission}</b><small>Permission</small></span><span className="absent"><b>{counts.absent}</b><small>Absent</small></span></div>; }
function TeacherAttendanceCards({ counts }: { counts: Counts }) { return <div className="entity-attendance-cards"><span className="present"><b>{counts.present}</b><small>Present</small></span><span className="permission"><b>{counts.permission}</b><small>Permission</small></span><span className="absent"><b>{counts.absent}</b><small>Absent</small></span></div>; }
function StateCards({ module, status }: { module: RecordModule; status: string }) {
  const states = module === "Teacher" ? ["Present", "Permission", "Absent"] : ["In Study", "Available", "Unavailable"];
  return <div className="entity-state-cards">{states.map(state => <span className={`state-${toneKey(state)} ${status.toLowerCase() === state.toLowerCase() ? "active" : ""}`} key={state}><b>{status.toLowerCase() === state.toLowerCase() ? 1 : 0}</b><small>{state}</small></span>)}</div>;
}
function attendanceCounts(activities: Record<string, string>[]) { return activities.reduce((total, activity) => { const counts = activityCounts(activity); return { present: total.present + counts.present, permission: total.permission + counts.permission, absent: total.absent + counts.absent }; }, { present: 0, permission: 0, absent: 0 }); }
function teacherAttendanceCounts(activities: Record<string, string>[]) { return activities.reduce((total, activity) => { const status = activity["Teacher attendance"] || "Present"; if (status === "Permission") total.permission++; else if (status === "Absent") total.absent++; else total.present++; return total; }, { present: 0, permission: 0, absent: 0 }); }
function activityCounts(activity: Record<string, string>): Counts { return { present: Number(activity.Present || 0), permission: Number(activity.Permission || 0), absent: Number(activity.Absent || 0) }; }
function studentSnapshot(value?: string) { return (value ?? "").split("; ").filter(Boolean).map(entry => { const split = entry.lastIndexOf(": "); return split < 0 ? { name: entry, status: "Unavailable" } : { name: entry.slice(0, split), status: entry.slice(split + 2) }; }); }
function attendanceLabel(value: string) { return value === "Excused" ? "Permission" : value; }
function toneKey(value: string) { return value.toLowerCase().replaceAll(" ", "-"); }
function initials(value: string) { return value.split(" ").map(part => part[0]).join("").slice(0, 2).toUpperCase(); }
function roomType(value: string) { return value.split(" · ")[0] || "Room"; }
