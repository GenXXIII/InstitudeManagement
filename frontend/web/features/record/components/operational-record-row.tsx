"use client";

import { useMemo, useState } from "react";
import { Icon } from "@/components/icon";
import type { OperationalRecord } from "../record-types";

export function OperationalRecordRow({ row }: { row: OperationalRecord }) {
  const [open, setOpen] = useState(false);
  const groups = useMemo(() => groupActivities(row.activities), [row.activities]);
  if (row.module === "Session") return <SessionRecordCard row={row} open={open} onToggle={() => setOpen(value => !value)}/>;
  return <article className={`operational-record-row ${row.module === "Session" ? "session-record-card" : ""} ${open ? "open" : ""}`}>
    <div className="operational-record-main">
      <div className="operational-record-identity"><span className="operational-record-icon"><Icon name={recordIcon(row.module)} size={17}/></span><div><strong>{row.subject}</strong><span>{row.identifier}</span></div></div>
      <p>{row.summary}</p><span className={`table-status ${row.status.toLowerCase().replaceAll(" ", "-")}`}>{row.status}</span>
      <time>{row.lastActivityAt ? new Date(row.lastActivityAt).toLocaleString() : "No activity yet"}</time>
      <button className="record-dropdown-button" onClick={() => setOpen(value => !value)} aria-expanded={open} disabled={!row.activities.length}><span>{row.activities.length ? open ? "Hide timeline" : "View time timeline" : "No activity"}</span><b>{row.activities.length ? open ? "−" : "+" : "—"}</b></button>
    </div>
    {open && <div className="operational-time-timeline"><header><div><strong>{row.module === "Session" ? "Students recorded when this class ended" : "Completed teaching by timetable time"}</strong><span>{row.module === "Session" ? "Present, late, absent, and permission status frozen for this period" : "Only classes that reached their timetable end are shown"}</span></div><b>{row.activities.length} records</b></header>{groups.map(group => <section className="operational-date-group" key={group.date}><div className="operational-date-label"><Icon name="calendar" size={15}/><strong>{displayDate(group.date)}</strong><span>{group.items.length} action{group.items.length === 1 ? "" : "s"}</span></div><div className="operational-time-list">{groupByTime(group.items).map(timeGroup => <ActivitiesAtTime items={timeGroup.items} time={timeGroup.time} key={`${group.date}-${timeGroup.time}`}/>)}</div></section>)}</div>}
  </article>;
}

function SessionRecordCard({ row, open, onToggle }: { row: OperationalRecord; open: boolean; onToggle: () => void }) {
  const summary = row.activities.find(activity => activity.Activity === "Completed class");
  const students = row.activities.filter(activity => activity.Activity === "Student attendance");
  const time = summary?.Time ?? "Timetable time unavailable";
  const date = summary?.Date ?? "Date unavailable";
  const present = students.filter(student => student.Attendance === "Present" || student.Attendance === "Late").length;
  const absent = students.filter(student => student.Attendance === "Absent").length;
  const permission = students.filter(student => student.Attendance === "Excused" || student.Attendance === "Permission").length;
  return <article className={`session-timetable-card ${open ? "open" : ""}`}>
    <button className="session-timetable-trigger" type="button" onClick={onToggle} aria-expanded={open}>
      <div className="session-card-time"><span>{row.classSessionRecordCode || "Class session"}</span><strong>{time}</strong><small>{sessionName(time)} session</small></div>
      <div className="session-card-date"><span className="session-card-calendar"><Icon name="calendar" size={17}/></span><div><small>Class date</small><strong>{displayNumericDate(date)}</strong></div></div>
      <div className="session-card-course"><small>Timetable cohort</small><strong>{summary?.Year ?? "Year unavailable"}</strong><span>Room {summary?.Classroom ?? "—"}</span></div>
      <div className="session-card-class"><small>Scheduled teaching</small><strong>{summary?.Teacher ?? "Teacher unavailable"}</strong><span>{summary?.Course ?? "Course unavailable"}</span></div>
      <div className="session-card-attendance"><span className="present"><b>{present}</b> came</span><span className="absent"><b>{absent}</b> absent</span>{permission > 0 && <span className="permission"><b>{permission}</b> permission</span>}</div>
      <span className="session-card-expand"><small>{open ? "Hide details" : "View details"}</small><b>{open ? "−" : "+"}</b></span>
    </button>
    {open && <div className="session-timetable-details"><header><div><strong>Timetable attendance record</strong><span>{displayDate(date)} · {time} · Room {summary?.Classroom ?? "—"} · {students.length} students</span></div><span className="table-status completed">Completed</span></header><ActivitiesAtTime items={row.activities} time={time}/></div>}
  </article>;
}

function ActivitiesAtTime({ items, time }: { items: Record<string, string>[]; time: string }) {
  const students = items.filter(item => item.Activity === "Student attendance");
  const summary = items.find(item => item.Activity === "Completed class");
  if (!students.length) return <>{items.map((activity, index) => <ActivityAtTime activity={activity} key={`${time}-${index}`}/>)}</>;
  return <article className="session-attendance-visual">
    <div className="session-attendance-head"><div className="operational-time-block"><time>{time}</time><span>{sessionName(time)}</span></div><div><span className="operational-action-kind">Completed class</span><strong>{summary?.Course ?? "Scheduled class"}</strong><small>{[summary?.Year, summary?.Teacher, summary?.Classroom].filter(Boolean).join(" · ")}</small></div><b>{summary?.Attendance}</b></div>
    <div className="session-student-status-grid">{students.map((student, index) => <div className={`session-student-status attendance-${student.Attendance.toLowerCase()}`} key={`${student.StudentCode}-${index}`}><span>{student.Student.split(" ").map(value => value[0]).join("").slice(0, 2)}</span><div><strong>{student.Student}</strong><small>{student.StudentCode} · {student["Check in"]}</small></div><b>{attendanceLabel(student.Attendance)}</b></div>)}</div>
  </article>;
}

function ActivityAtTime({ activity }: { activity: Record<string, string> }) {
  const title = activity.Activity || "Recorded action";
  const time = activity.Time || "No class time";
  const subject = activity.Course || activity.Student || activity.Teacher || activity.Classroom || activity.Status || "Institute activity";
  const state = activity.Attendance || activity.Grade || activity.Status;
  const detailKeys = Object.keys(activity).filter(key => !["Activity", "Date", "Time", "Attendance", "Grade", "Status"].includes(key));
  return <article className={`operational-time-entry ${title === "Completed class" ? "completed-class" : ""}`}>
    <div className="operational-time-block"><time>{time}</time><span>{sessionName(time)}</span></div>
    <div className="operational-action-block"><div><span className="operational-action-kind">{title}</span>{state && <b className={`operational-action-state state-${state.toLowerCase().replaceAll(" ", "-")}`}>{attendanceLabel(state)}</b>}</div><strong>{subject}</strong><div className="operational-action-details">{detailKeys.map(key => <span key={key}><small>{key}</small><b>{activity[key]}</b></span>)}</div></div>
  </article>;
}

function groupActivities(activities: Record<string, string>[]) {
  const groups = new Map<string, Record<string, string>[]>();
  for (const activity of activities) { const date = activity.Date || (activity.Day ? `Weekly · ${activity.Day}` : "Other recorded activity"); groups.set(date, [...(groups.get(date) ?? []), activity]); }
  return Array.from(groups, ([date, items]) => ({ date, items }));
}
function groupByTime(activities: Record<string, string>[]) { const groups = new Map<string, Record<string, string>[]>(); for (const activity of activities) { const time = activity.Time || "No class time"; groups.set(time, [...(groups.get(time) ?? []), activity]); } return Array.from(groups, ([time, items]) => ({ time, items })); }
function displayDate(value: string) { const parsed = new Date(`${value}T00:00:00`); return Number.isNaN(parsed.valueOf()) ? value : parsed.toLocaleDateString(undefined, { weekday: "long", day: "2-digit", month: "short", year: "numeric" }); }
function displayNumericDate(value: string) { const parsed = new Date(`${value}T00:00:00`); return Number.isNaN(parsed.valueOf()) ? value : parsed.toLocaleDateString("en-US", { month: "numeric", day: "numeric", year: "numeric" }); }
function sessionName(value: string) { const match = value.match(/^(\d{1,2}):/); if (!match) return "Activity"; const hour = Number(match[1]); return hour >= 17 ? "Evening" : hour >= 13 ? "Afternoon" : "Morning"; }
function attendanceLabel(value: string) { return value.toLowerCase() === "excused" ? "Permission" : value; }
function recordIcon(module: string): Parameters<typeof Icon>[0]["name"] { return module === "Session" ? "calendar" : module === "Student" || module === "Attendance" ? "users" : module === "Teacher" ? "teacher" : module === "Classroom" ? "room" : "book"; }
