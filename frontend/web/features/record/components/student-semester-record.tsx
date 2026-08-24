"use client";

import { useRouter } from "next/navigation";
import Image from "next/image";
import { useState } from "react";
import { Icon } from "@/components/icon";
import { recordApi } from "../record-api";
import type { OperationalRecord, OperationalRecordGrade, OperationalRecordInsights } from "../record-types";

const emptyInsights: OperationalRecordInsights = { presentCount: 0, permissionCount: 0, absentCount: 0, grades: [], expectedCourses: 5, totalScore: 0, average: 0, result: "In progress", isFinal: false };

export function StudentSemesterRecordHeader() {
  return <div className="student-semester-record-head"><span>Code</span><span>Photo</span><span>Name</span><span>Department</span><span>Attendance</span><span>Five course grades</span><span>Semester result</span></div>;
}

export function StudentSemesterRecord({ row, detailHref, detailPage = false, editable = false, onUpdated }: { row: OperationalRecord; detailHref?: string; detailPage?: boolean; editable?: boolean; onUpdated?: () => void }) {
  const router = useRouter();
  const insights = row.insights ?? emptyInsights;
  const gradeSlots = slots(insights);
  if (detailPage) return <StudentSemesterDetail row={row} insights={insights} gradeSlots={gradeSlots} editable={editable} onUpdated={onUpdated}/>;
  const open = () => { if (detailHref) router.push(detailHref); };
  return <article className="student-semester-record-row record-row-clickable" role="link" tabIndex={0} onClick={open} onKeyDown={event => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); open(); } }}>
    <strong className="student-record-code">{row.code || row.identifier.split(" · ")[0]}</strong>
    <StudentPhoto row={row}/>
    <div className="student-record-name"><strong>{row.subject}</strong><span>{identityDetail(row.identifier)}</span></div>
    <div className="student-record-department"><strong>{row.department || "Unassigned"}</strong><span>{row.academicYear} · {row.term}</span></div>
    <AttendanceCards insights={insights}/>
    <GradeCards grades={gradeSlots}/>
    <ResultCard insights={insights}/>
  </article>;
}

function StudentSemesterDetail({ row, insights, gradeSlots, editable, onUpdated }: { row: OperationalRecord; insights: OperationalRecordInsights; gradeSlots: Array<OperationalRecordGrade | null>; editable: boolean; onUpdated?: () => void }) {
  const sessions = row.activities.filter(activity => activity.Activity === "Class attendance");
  return <article className="student-semester-detail">
    <header><StudentPhoto row={row}/><div><span className="eyebrow">{row.academicYear} · {row.term}</span><h2>{row.subject}</h2><p>{row.code} · {row.department} · {identityDetail(row.identifier)}</p></div></header>
    <div className="student-detail-insights"><AttendanceCards insights={insights}/><GradeCards grades={gradeSlots}/><ResultCard insights={insights}/></div>
    <section className="student-record-detail-section"><header><div><strong>Class-session attendance</strong><span>Every completed timetable session recorded for this semester</span></div><b>{sessions.length} sessions</b></header><div className="student-session-detail-list">{sessions.length ? sessions.map((session, index) => <AttendanceDetailRow session={session} studentId={row.resourceId} editable={editable} onUpdated={onUpdated} key={`${session.Date}-${session.Time}-${index}`}/>) : <p className="student-record-detail-empty">No completed class sessions in this semester.</p>}</div></section>
    <section className="student-record-detail-section"><header><div><strong>Course grades</strong><span>Five department-course results for this semester</span></div><b>{insights.grades.length}/5 assigned</b></header><div className="student-grade-detail-grid">{gradeSlots.map((grade, index) => <GradeDetailCard grade={grade} index={index} studentId={row.resourceId} editable={editable} onUpdated={onUpdated} key={grade?.courseCode ?? index}/>)}</div><footer className={`student-final-result result-${toneKey(insights.result)}`}><div><span>Total score</span><strong>{insights.totalScore.toFixed(1)} ÷ 5 = {insights.average.toFixed(2)}</strong></div><b>{insights.result}</b></footer></section>
  </article>;
}

function AttendanceDetailRow({ session, studentId, editable, onUpdated }: { session: Record<string, string>; studentId: string; editable: boolean; onUpdated?: () => void }) {
  const [editing, setEditing] = useState(false);
  const [status, setStatus] = useState(session.Attendance === "Permission" ? "Excused" : session.Attendance);
  const [checkedInAt, setCheckedInAt] = useState(session["Check in"] === "No check-in" ? "" : session["Check in"]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const needsCheckIn = status === "Present" || status === "Late";
  async function save() {
    if (needsCheckIn && !/^\d{2}:\d{2}$/.test(checkedInAt)) { setError("Present and late require a check-in time."); return; }
    setSaving(true); setError("");
    try { await recordApi.updateStudentAttendance(session["Class session id"], studentId, status, needsCheckIn ? checkedInAt : ""); setEditing(false); onUpdated?.(); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "Could not update attendance."); }
    finally { setSaving(false); }
  }
  if (editing) return <article className="student-session-detail student-record-inline-editor"><div><time>{session.Date}</time><strong>{session.Time}</strong></div><div><strong>{session.Course}</strong><span>{session.Teacher} · Room {session.Classroom}</span></div><label><span>Attendance</span><select value={status} onChange={event => { const value = event.target.value; setStatus(value); if ((value === "Present" || value === "Late") && !checkedInAt) setCheckedInAt(session.Time.match(/^\d{2}:\d{2}/)?.[0] ?? ""); }}><option>Present</option><option>Late</option><option>Absent</option><option value="Excused">Permission</option></select></label><label><span>Check-in</span><input type="time" disabled={!needsCheckIn} value={checkedInAt} onChange={event => setCheckedInAt(event.target.value)}/></label><div className="student-inline-actions"><button type="button" className="button secondary" onClick={() => setEditing(false)} disabled={saving}>Cancel</button><button type="button" className="button primary" onClick={save} disabled={saving}>{saving ? "Saving…" : "Save"}</button></div>{error && <small className="student-inline-error">{error}</small>}</article>;
  return <article className={`student-session-detail attendance-${toneKey(session.Attendance)}`}><div><time>{session.Date}</time><strong>{session.Time}</strong></div><div><strong>{session.Course}</strong><span>{session.Teacher} · Room {session.Classroom}</span></div><div><b>{attendanceLabel(session.Attendance)}</b><span>{session["Check in"]}</span></div>{editable && <button type="button" className="student-record-edit-button" onClick={() => setEditing(true)}><Icon name="edit" size={14}/>Edit</button>}</article>;
}

function GradeDetailCard({ grade, index, studentId, editable, onUpdated }: { grade: OperationalRecordGrade | null; index: number; studentId: string; editable: boolean; onUpdated?: () => void }) {
  const [editing, setEditing] = useState(false);
  const [score, setScore] = useState(grade?.score.toString() ?? "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  async function save() {
    const value = Number(score);
    if (!grade || !Number.isFinite(value) || value < 0 || value > 100) { setError("Score must be from 0 to 100."); return; }
    setSaving(true); setError("");
    try { await recordApi.updateGrade(studentId, grade.courseId, value); setEditing(false); onUpdated?.(); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "Could not update grade."); }
    finally { setSaving(false); }
  }
  return <article className={`student-grade-detail grade-${toneKey(grade?.grade ?? "pending")}`}><span>Course {index + 1}</span><strong>{grade?.courseCode ?? "Pending"}</strong><p>{grade?.courseName ?? "Grade has not been assigned"}</p>{editing ? <div className="student-grade-inline-editor"><input type="number" min="0" max="100" step="0.1" value={score} onChange={event => setScore(event.target.value)} aria-label={`Score for ${grade?.courseCode}`}/><button type="button" onClick={() => setEditing(false)} disabled={saving}>Cancel</button><button type="button" onClick={save} disabled={saving}>{saving ? "Saving…" : "Save"}</button>{error && <small>{error}</small>}</div> : <div><b>{grade ? grade.score.toFixed(1) : "—"}</b><em>{grade?.grade ?? "—"}</em>{editable && grade && <button type="button" className="student-record-edit-button" onClick={() => setEditing(true)}><Icon name="edit" size={13}/>Edit</button>}</div>}</article>;
}

function StudentPhoto({ row }: { row: OperationalRecord }) {
  return row.photoDataUrl ? <Image className="student-semester-photo" src={row.photoDataUrl} alt={`${row.subject} portrait`} width={42} height={58} unoptimized/> : <span className="student-semester-photo student-photo-fallback">{initials(row.subject)}</span>;
}
function AttendanceCards({ insights }: { insights: OperationalRecordInsights }) { return <div className="student-attendance-cards"><span className="present"><b>{insights.presentCount}</b><small>Present</small></span><span className="permission"><b>{insights.permissionCount}</b><small>Permission</small></span><span className="absent"><b>{insights.absentCount}</b><small>Absent</small></span></div>; }
function GradeCards({ grades }: { grades: Array<OperationalRecordGrade | null> }) { return <div className="student-grade-cards">{grades.map((grade, index) => <span className={`grade-${toneKey(grade?.grade ?? "pending")}`} title={grade?.courseName ?? "Grade pending"} key={grade?.courseCode ?? index}><b>{grade?.grade ?? "—"}</b><small>{grade?.courseCode ?? `C${index + 1}`}</small></span>)}</div>; }
function ResultCard({ insights }: { insights: OperationalRecordInsights }) { return <div className={`student-result-card result-${toneKey(insights.result)}`}><strong>{insights.result}</strong><span>{insights.totalScore.toFixed(1)} ÷ 5</span><b>{insights.average.toFixed(2)}</b></div>; }
function slots(insights: OperationalRecordInsights) { return Array.from({ length: insights.expectedCourses || 5 }, (_, index) => insights.grades[index] ?? null); }
function identityDetail(identifier: string) { return identifier.split(" · ").slice(1).join(" · "); }
function initials(value: string) { return value.split(" ").map(part => part[0]).join("").slice(0, 2).toUpperCase(); }
function attendanceLabel(value?: string) { return value === "Excused" ? "Permission" : value || "Not recorded"; }
function toneKey(value: string) { return value.toLowerCase().replaceAll(" ", "-"); }
