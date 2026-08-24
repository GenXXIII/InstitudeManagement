"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/icon";
import { recordApi } from "../record-api";
import type { ClassSessionAttendanceUpdate, OperationalRecord } from "../record-types";
import { EntitySemesterRecord } from "./entity-semester-record";
import { StudentSemesterRecord } from "./student-semester-record";

export function OperationalRecordRow({ row, editable = false, showStatus = true, onUpdated, detailHref, detailPage = false }: { row: OperationalRecord; editable?: boolean; showStatus?: boolean; onUpdated?: () => void; detailHref?: string; detailPage?: boolean }) {
  const router = useRouter();
  const [open, setOpen] = useState(detailPage);
  const expanded = detailPage || open;
  const openDetails = () => detailHref ? router.push(detailHref) : setOpen(value => !value);
  const openFromKeyboard = (event: React.KeyboardEvent) => { if (detailHref && (event.key === "Enter" || event.key === " ")) { event.preventDefault(); router.push(detailHref); } };
  const groups = useMemo(() => groupActivities(row.activities), [row.activities]);
  if (row.module === "Session") return <SessionRecordCard row={row} open={expanded} editable={editable} showStatus={showStatus} detailPage={detailPage} onToggle={openDetails} onUpdated={onUpdated}/>;
  if (row.module === "Student") return <StudentSemesterRecord row={row} detailHref={detailHref} detailPage={detailPage} editable={editable} onUpdated={onUpdated}/>;
  if (row.module === "Teacher" || row.module === "Course" || row.module === "Classroom") return <EntitySemesterRecord row={row} detailHref={detailHref} detailPage={detailPage}/>;
  return <article className={`operational-record-row ${expanded ? "open" : ""} ${detailHref ? "record-row-clickable" : ""} ${showStatus ? "" : "without-record-status"}`} role={detailHref ? "link" : undefined} tabIndex={detailHref ? 0 : undefined} onClick={detailHref ? openDetails : undefined} onKeyDown={openFromKeyboard}>
    <div className="operational-record-main">
      <div className="operational-record-identity"><span className="operational-record-icon"><Icon name={recordIcon(row.module)} size={17}/></span><div><strong>{row.subject}</strong><span>{row.identifier}</span></div></div>
      <p>{row.summary}</p>{showStatus && <span className={`table-status ${row.status.toLowerCase().replaceAll(" ", "-")}`}>{row.status}</span>}
      <time>{row.lastActivityAt ? new Date(row.lastActivityAt).toLocaleString() : "No activity yet"}</time>
    </div>
    {expanded && <div className="operational-time-timeline"><header><div><strong>{row.module === "Session" ? "Students recorded when this class ended" : "Completed teaching by timetable time"}</strong><span>{row.module === "Session" ? "Present, late, absent, and permission status frozen for this period" : "Only classes that reached their timetable end are shown"}</span></div><b>{row.activities.length} records</b></header>{groups.map(group => <section className="operational-date-group" key={group.date}><div className="operational-date-label"><Icon name="calendar" size={15}/><strong>{displayDate(group.date)}</strong><span>{group.items.length} action{group.items.length === 1 ? "" : "s"}</span></div><div className="operational-time-list">{groupByTime(group.items).map(timeGroup => <ActivitiesAtTime items={timeGroup.items} time={timeGroup.time} key={`${group.date}-${timeGroup.time}`}/>)}</div></section>)}</div>}
  </article>;
}

function SessionRecordCard({ row, open, editable, showStatus, detailPage, onToggle, onUpdated }: { row: OperationalRecord; open: boolean; editable: boolean; showStatus: boolean; detailPage: boolean; onToggle: () => void; onUpdated?: () => void }) {
  const [editing, setEditing] = useState(false);
  const summary = row.activities.find(activity => activity.Activity === "Completed class");
  const students = row.activities.filter(activity => activity.Activity === "Student attendance");
  const time = summary?.Time ?? "Timetable time unavailable";
  const date = summary?.Date ?? "Date unavailable";
  const present = students.filter(student => student.Attendance === "Present" || student.Attendance === "Late").length;
  const absent = students.filter(student => student.Attendance === "Absent").length;
  const permission = students.filter(student => student.Attendance === "Excused" || student.Attendance === "Permission").length;
  const trigger = <>
      <div className="session-card-time"><span>{row.classSessionRecordCode || "Class session"}</span><strong>{time}</strong><small>{sessionName(time)} session</small></div>
      <div className="session-card-date"><span className="session-card-calendar"><Icon name="calendar" size={17}/></span><div><small>Class date</small><strong>{displayNumericDate(date)}</strong></div></div>
      <div className="session-card-course"><small>Timetable cohort</small><strong>{summary?.Year ?? "Year unavailable"}</strong><span>Room {summary?.Classroom ?? "—"}</span></div>
      <div className="session-card-class"><small>Scheduled teaching</small><strong>{summary?.Teacher ?? "Teacher unavailable"}</strong><span>{summary?.Course ?? "Course unavailable"}</span></div>
      <div className="session-card-attendance"><span className="present"><b>{present}</b> came</span><span className="absent"><b>{absent}</b> absent</span>{permission > 0 && <span className="permission"><b>{permission}</b> permission</span>}</div>
    </>;
  return <article className={`session-timetable-card ${open ? "open" : ""}`}>
    {detailPage ? <div className="session-timetable-trigger session-timetable-static">{trigger}</div> : <div className="session-timetable-trigger" role="link" tabIndex={0} onClick={onToggle} onKeyDown={event => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); onToggle(); } }}>{trigger}</div>}
    {open && <div className="session-timetable-details"><header><div><strong>Timetable attendance record</strong><span>{displayDate(date)} · {time} · Room {summary?.Classroom ?? "—"} · {students.length} students</span></div>{(showStatus || editable) && <div className="session-record-actions">{showStatus && <span className="table-status completed">Completed</span>}{editable && <button className="button secondary" type="button" onClick={() => setEditing(true)}><Icon name="edit" size={14}/>Edit record</button>}</div>}</header><ActivitiesAtTime items={row.activities} time={time}/></div>}
    {editing && <SessionRecordEditor row={row} onClose={() => setEditing(false)} onSaved={() => { setEditing(false); onUpdated?.(); }}/>}
  </article>;
}

function SessionRecordEditor({ row, onClose, onSaved }: { row: OperationalRecord; onClose: () => void; onSaved: () => void }) {
  const source = row.activities.filter(activity => activity.Activity === "Student attendance");
  const [students, setStudents] = useState<ClassSessionAttendanceUpdate[]>(source.map(student => ({ studentId: student.StudentId, status: student.Attendance === "Permission" ? "Excused" : student.Attendance, checkedInAt: student["Check in"] === "No check-in" ? "" : student["Check in"] })));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  async function save(event: React.FormEvent) {
    event.preventDefault();
    const missingTime = students.find(student => (student.status === "Present" || student.status === "Late") && !/^\d{2}:\d{2}$/.test(student.checkedInAt));
    if (missingTime) { setError("Present and late students require a check-in time."); return; }
    setSaving(true); setError("");
    try { await recordApi.updateSession(row.id, students); onSaved(); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "Could not update this class session record."); setSaving(false); }
  }
  function update(index: number, value: Partial<ClassSessionAttendanceUpdate>) { setStudents(current => current.map((student, position) => position === index ? { ...student, ...value } : student)); setError(""); }
  return <div className="modal-backdrop" onMouseDown={event => { if (event.target === event.currentTarget && !saving) onClose(); }}><form className="modal session-record-editor" onSubmit={save}><div className="modal-head"><div><span className="eyebrow">Active-semester record</span><h2>Edit {row.classSessionRecordCode}</h2><p>Corrections update every Record view. The record becomes read-only after semester rollover.</p></div><button className="icon-button" type="button" aria-label="Close editor" onClick={onClose} disabled={saving}><Icon name="close"/></button></div><div className="session-record-edit-list">{source.map((student, index) => <div className="session-record-edit-row" key={student.StudentId}><div><strong>{student.Student}</strong><span>{student.StudentCode}</span></div><label><span>Status</span><select value={students[index].status} onChange={event => update(index, { status: event.target.value, checkedInAt: event.target.value === "Present" || event.target.value === "Late" ? students[index].checkedInAt : "" })}><option>Present</option><option>Late</option><option>Absent</option><option value="Excused">Permission</option></select></label><label><span>Check-in</span><input type="time" value={students[index].checkedInAt} disabled={students[index].status === "Absent" || students[index].status === "Excused"} onChange={event => update(index, { checkedInAt: event.target.value })}/></label></div>)}</div>{error && <div className="form-error" role="alert">{error}</div>}<div className="modal-actions"><button className="button secondary" type="button" onClick={onClose} disabled={saving}>Cancel</button><button className="button primary" disabled={saving}>{saving ? "Saving correction..." : "Save record"}</button></div></form></div>;
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
  const detailKeys = Object.keys(activity).filter(key => !["Activity", "StudentId", "Date", "Time", "Attendance", "Grade", "Status"].includes(key));
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
