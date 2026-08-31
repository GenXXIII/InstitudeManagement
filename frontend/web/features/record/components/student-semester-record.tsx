"use client";

import { useRouter } from "next/navigation";
import Image from "next/image";
import { useState } from "react";
import { Icon } from "@/components/icon";
import { WorkflowCodeFlow } from "@/components/workflow-code-flow";
import { workflowCode, type WorkflowCodeStage } from "@/lib/workflow-code";
import { recordApi } from "../record-api";
import type { OperationalRecord, OperationalRecordGrade, OperationalRecordInsights } from "../record-types";

const emptyInsights: OperationalRecordInsights = { presentCount: 0, permissionCount: 0, absentCount: 0, grades: [], expectedCourses: 5, totalScore: 0, average: 0, result: "In progress", isFinal: false };

export function StudentSemesterRecordHeader({ history = false }: { history?: boolean }) {
  return <div className="student-semester-record-head"><span>{history ? "History code" : "Record code"}</span><span>Photo</span><span>Name</span><span>Department</span><span>{history ? "Completed level" : "Year"}</span><span>{history ? "Four-year attendance" : "Attendance"}</span><span>{history ? "Semester grade totals" : "Five course grades"}</span><span>{history ? "Program result" : "Semester result"}</span></div>;
}

export function StudentSemesterRecord({ row, stage = "record", detailHref, detailPage = false, editable = false, onUpdated }: { row: OperationalRecord; stage?: WorkflowCodeStage; detailHref?: string; detailPage?: boolean; editable?: boolean; onUpdated?: () => void }) {
  const router = useRouter();
  const insights = row.insights ?? emptyInsights;
  const gradeSlots = slots(insights);
  if (detailPage) return <StudentSemesterDetail row={row} stage={stage} insights={insights} gradeSlots={gradeSlots} editable={editable} onUpdated={onUpdated}/>;
  const open = () => { if (detailHref) router.push(detailHref); };
  return <article className="student-semester-record-row record-row-clickable" role="link" tabIndex={0} onClick={open} onKeyDown={event => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); open(); } }}>
    <div className="workflow-ledger-code"><strong className="student-record-code">{workflowCode(row.code || row.identifier.split(" · ")[0], "student", stage)}</strong><small>Source {workflowCode(row.code, "student", "management")}</small></div>
    <StudentPhoto row={row}/>
    <div className="student-record-name"><strong>{row.subject}</strong><span>{identityDetail(row.identifier)}</span></div>
    <div className="student-record-department"><strong>{row.department || "Unassigned"}</strong><span>{row.academicYear} · {row.term}</span></div>
    <div className="student-record-year"><strong>{stage === "history" ? "Year 1–4" : recordYear(row)}</strong><span>{stage === "history" ? "Completed Year 4 Semester 2" : recordShift(row)}</span></div>
    <AttendanceCards insights={insights}/>
    {stage === "history" ? <ProgramGradeTotals row={row}/> : <GradeCards grades={gradeSlots} stage={stage}/>}
    {stage === "history" ? <ProgramResultCard insights={insights}/> : <ResultCard insights={insights}/>}
  </article>;
}

function StudentSemesterDetail({ row, stage, insights, gradeSlots, editable, onUpdated }: { row: OperationalRecord; stage: WorkflowCodeStage; insights: OperationalRecordInsights; gradeSlots: Array<OperationalRecordGrade | null>; editable: boolean; onUpdated?: () => void }) {
  if (stage === "history") return <StudentProgramHistoryDetail row={row} insights={insights}/>;
  const sessions = row.activities.filter(activity => activity.Activity === "Class attendance");
  return <article className="student-semester-detail">
    <header><StudentPhoto row={row}/><div><span className="eyebrow">{row.academicYear} · {row.term}</span><h2>{row.subject}</h2><p>{workflowCode(row.code, "student", stage)} · {row.department} · {identityDetail(row.identifier)}</p></div></header>
    <WorkflowCodeFlow sourceCode={row.code} resource="student" currentStage={stage}/>
    <section className="semester-record-information" aria-label="Student semester information"><Information label="Record code" value={workflowCode(row.code, "student", stage)}/><Information label="Management source" value={workflowCode(row.code, "student", "management")}/><Information label="Department" value={row.department}/><Information label="Year" value={recordYear(row)}/><Information label="Shift" value={recordShift(row)}/><Information label="Academic year" value={row.academicYear}/><Information label="Semester" value={row.term}/><Information label="Enrollment" value={enrollmentValue(row, "Enrollment status")}/></section>
    <div className="student-detail-insights"><AttendanceCards insights={insights}/><GradeCards grades={gradeSlots} stage={stage}/><ResultCard insights={insights}/></div>
    <section className="student-record-detail-section"><header><div><strong>Class-session attendance</strong><span>Every held or cancelled timetable period recorded for this semester</span></div><b>{sessions.length} sessions</b></header><div className="student-session-detail-list">{sessions.length ? sessions.map((session, index) => <AttendanceDetailRow session={session} studentId={row.resourceId} editable={editable} onUpdated={onUpdated} key={`${session.Date}-${session.Time}-${index}`}/>) : <p className="student-record-detail-empty">No recorded class sessions in this semester.</p>}</div></section>
    <section className="student-record-detail-section"><header><div><strong>Course grades</strong><span>Five course-grade records preserved for this semester</span></div><b>{insights.grades.length}/5 assigned</b></header><div className="student-grade-detail-grid">{gradeSlots.map((grade, index) => <GradeDetailCard grade={grade} index={index} studentId={row.resourceId} stage={stage} editable={editable} onUpdated={onUpdated} key={grade?.courseCode ?? index}/>)}</div><footer className={`student-final-result result-${toneKey(insights.result)}`}><div><span>Total score</span><strong>{insights.totalScore.toFixed(1)} ÷ 5 = {insights.average.toFixed(2)}</strong></div><b>{insights.result}</b></footer></section>
  </article>;
}

function StudentProgramHistoryDetail({ row, insights }: { row: OperationalRecord; insights: OperationalRecordInsights }) {
  const periods = programPeriods(row);
  return <article className="student-semester-detail student-program-history-detail">
    <header><StudentPhoto row={row}/><div><span className="eyebrow">Graduated in {row.academicYear} · Year 4 Semester 2</span><h2>{row.subject}</h2><p>{workflowCode(row.code, "student", "history")} · {row.department} · Permanent read-only archive</p></div></header>
    <WorkflowCodeFlow sourceCode={row.code} resource="student" currentStage="history"/>
    <section className="semester-record-information" aria-label="Completed student program information"><Information label="History code" value={workflowCode(row.code, "student", "history")}/><Information label="Management source" value={workflowCode(row.code, "student", "management")}/><Information label="Department" value={row.department}/><Information label="Completed level" value="Year 4 Semester 2"/><Information label="Graduation academic year" value={row.academicYear}/><Information label="Archived semesters" value={periods.length.toString()}/><Information label="State" value="Graduated"/><Information label="Editing" value="Permanent read-only"/></section>
    <div className="student-detail-insights program-history-insights"><AttendanceCards insights={insights}/><ProgramGradeTotals row={row}/><ProgramResultCard insights={insights}/></div>
    <section className="student-program-timeline"><header><div><strong>Complete Year 1–4 record</strong><span>Every semester retains its information, class attendance, grade codes, course results, and totals.</span></div><b>{periods.length} semesters</b></header>{periods.map(period => <article className="student-program-period" key={period.key}><header><div><span>{period.year}</span><h3>{period.academicYear}</h3></div><strong>{period.term}</strong><small>{period.attendance.length} sessions · {period.grades.length} grades · {period.total.toFixed(1)} total</small></header><section><div><strong>Attendance detail</strong><span>{period.attendance.filter(item => item.Attendance === "Present" || item.Attendance === "Late").length} present · {period.attendance.filter(item => item.Attendance === "Permission" || item.Attendance === "Excused").length} permission · {period.attendance.filter(item => item.Attendance === "Absent").length} absent</span></div><div className="student-session-detail-list">{period.attendance.length ? period.attendance.map((session, index) => <AttendanceDetailRow session={session} studentId={row.resourceId} editable={false} key={`${period.key}-${session.Date}-${session.Time}-${index}`}/>) : <p className="student-record-detail-empty">No class-session evidence for this semester.</p>}</div></section><section><div><strong>Grade detail</strong><span>Business codes, course scores, letter grades, and semester total</span></div><div className="program-period-grade-grid">{period.grades.length ? period.grades.map((grade, index) => <HistoryGradeCard grade={grade} key={`${period.key}-${grade["Grade code"]}-${index}`}/>) : <p className="student-record-detail-empty">No grade evidence for this semester.</p>}</div><footer><span>Semester total</span><strong>{period.total.toFixed(1)}</strong><b>Average {period.average.toFixed(2)}</b></footer></section></article>)}</section>
  </article>;
}

function ProgramGradeTotals({ row }: { row: OperationalRecord }) {
  const periods = programPeriods(row);
  return <div className="student-grade-cards program-grade-totals">{periods.map(period => <span title={`${period.academicYear} · ${period.term} · average ${period.average.toFixed(2)}`} key={period.key}><b>{period.total.toFixed(0)}</b><small>{period.shortLabel}</small></span>)}</div>;
}

function ProgramResultCard({ insights }: { insights: OperationalRecordInsights }) {
  return <div className="student-result-card result-a"><strong>Graduated</strong><span>All-course average</span><b>{insights.average.toFixed(2)}</b></div>;
}

function HistoryGradeCard({ grade }: { grade: Record<string, string> }) {
  return <article className={`student-grade-detail grade-${toneKey(grade.Grade || "pending")}`}><span>{workflowCode(grade["Grade code"], "grade", "history")}</span><strong>{grade["Course code"] || "Course"}</strong><p>{grade.Course}</p><div><b>{Number(grade.Score || 0).toFixed(1)}</b><em>{grade.Grade || "—"}</em></div></article>;
}

function programPeriods(row: OperationalRecord) {
  const groups = new Map<string, Record<string, string>[]>();
  for (const activity of row.activities) {
    const academicYear = activity["Academic year"];
    const term = activity.Term;
    if (!academicYear || !term) continue;
    const key = `${academicYear}|${term}`;
    groups.set(key, [...(groups.get(key) ?? []), activity]);
  }
  return [...groups.entries()].toSorted(([left], [right]) => left.localeCompare(right, undefined, { numeric: true })).map(([key, activities], index) => {
    const [academicYear, term] = key.split("|");
    const grades = activities.filter(activity => activity.Activity === "Course grade");
    const attendance = activities.filter(activity => activity.Activity === "Class attendance");
    const total = grades.reduce((sum, grade) => sum + Number(grade.Score || 0), 0);
    const enrollmentYear = activities.find(activity => activity.Activity === "Student enrollment")?.Year;
    const year = enrollmentYear || `Year ${Math.floor(index / 2) + 1}`;
    const semesterNumber = term.match(/\d+/)?.[0] ?? `${index % 2 + 1}`;
    return { key, academicYear, term, year, shortLabel: `Y${year.match(/\d+/)?.[0] ?? Math.floor(index / 2) + 1} S${semesterNumber}`, activities, grades, attendance, total, average: grades.length ? total / grades.length : 0 };
  });
}

function AttendanceDetailRow({ session, studentId, editable, onUpdated }: { session: Record<string, string>; studentId: string; editable: boolean; onUpdated?: () => void }) {
  const [editing, setEditing] = useState(false);
  const [status, setStatus] = useState(session.Attendance === "Permission" ? "Excused" : session.Attendance);
  const [checkedInAt, setCheckedInAt] = useState(session["Check in"] === "No check-in" ? "" : session["Check in"]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const needsCheckIn = status === "Present" || status === "Late";
  const classHeld = session["Session status"] === "Running";
  async function save() {
    if (needsCheckIn && !/^\d{2}:\d{2}$/.test(checkedInAt)) { setError("Present and late require a check-in time."); return; }
    setSaving(true); setError("");
    try { await recordApi.updateStudentAttendance(session.ClassSessionId, studentId, status, needsCheckIn ? checkedInAt : ""); setEditing(false); onUpdated?.(); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "Could not update attendance."); }
    finally { setSaving(false); }
  }
  if (editing) return <article className="student-session-detail student-record-inline-editor"><div><time>{session.Date}</time><strong>{session.Time}</strong></div><div><strong>{session.Course}</strong><span>{session.Teacher} · Room {session.Classroom}</span></div><label><span>Attendance</span><select value={status} onChange={event => { const value = event.target.value; setStatus(value); if ((value === "Present" || value === "Late") && !checkedInAt) setCheckedInAt(session.Time.match(/^\d{2}:\d{2}/)?.[0] ?? ""); }}><option>Present</option><option>Late</option><option>Absent</option><option value="Excused">Permission</option></select></label><label><span>Check-in</span><input type="time" disabled={!needsCheckIn} value={checkedInAt} onChange={event => setCheckedInAt(event.target.value)}/></label><div className="student-inline-actions"><button type="button" className="button secondary" onClick={() => setEditing(false)} disabled={saving}>Cancel</button><button type="button" className="button primary" onClick={save} disabled={saving}>{saving ? "Saving…" : "Save"}</button></div>{error && <small className="student-inline-error">{error}</small>}</article>;
  return <article className={`student-session-detail attendance-${toneKey(session.Attendance)} ${classHeld ? "" : "class-not-held"}`}><div><time>{session.Date}</time><strong>{session.Time}</strong><small>{session["Class session code"]}</small></div><div><strong>{session.Course}</strong><span>{session["Timetable code"]} · {session.Teacher} · Room {session.Classroom}</span><small>{session.Reason}</small></div><div><b>{classHeld ? attendanceLabel(session.Attendance) : "Class not held"}</b><span>{classHeld ? session["Check in"] : session["Teacher attendance"]}</span></div>{editable && classHeld && <button type="button" className="student-record-edit-button" onClick={() => setEditing(true)}><Icon name="edit" size={14}/>Edit</button>}</article>;
}

function GradeDetailCard({ grade, index, studentId, stage, editable, onUpdated }: { grade: OperationalRecordGrade | null; index: number; studentId: string; stage: WorkflowCodeStage; editable: boolean; onUpdated?: () => void }) {
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
  return <article className={`student-grade-detail grade-${toneKey(grade?.grade ?? "pending")}`}><span>{grade ? workflowCode(grade.gradeCode, "grade", stage) : `Grade record ${index + 1}`}</span><strong>{grade?.courseCode ?? "Pending"}</strong><p>{grade?.courseName ?? "Grade has not been assigned"}</p>{editing ? <div className="student-grade-inline-editor"><input type="number" min="0" max="100" step="0.1" value={score} onChange={event => setScore(event.target.value)} aria-label={`Score for ${grade?.courseCode}`}/><button type="button" onClick={() => setEditing(false)} disabled={saving}>Cancel</button><button type="button" onClick={save} disabled={saving}>{saving ? "Saving…" : "Save"}</button>{error && <small>{error}</small>}</div> : <div><b>{grade ? grade.score.toFixed(1) : "—"}</b><em>{grade?.grade ?? "—"}</em>{editable && grade && <button type="button" className="student-record-edit-button" onClick={() => setEditing(true)}><Icon name="edit" size={13}/>Edit</button>}</div>}</article>;
}

function StudentPhoto({ row }: { row: OperationalRecord }) {
  return row.photoDataUrl ? <Image className="student-semester-photo" src={row.photoDataUrl} alt={`${row.subject} portrait`} width={42} height={58} unoptimized/> : <span className="student-semester-photo student-photo-fallback">{initials(row.subject)}</span>;
}
function AttendanceCards({ insights }: { insights: OperationalRecordInsights }) { return <div className="student-attendance-cards"><span className="present"><b>{insights.presentCount}</b><small>Present</small></span><span className="permission"><b>{insights.permissionCount}</b><small>Permission</small></span><span className="absent"><b>{insights.absentCount}</b><small>Absent</small></span></div>; }
function GradeCards({ grades, stage }: { grades: Array<OperationalRecordGrade | null>; stage: WorkflowCodeStage }) { return <div className="student-grade-cards">{grades.map((grade, index) => <span className={`grade-${toneKey(grade?.grade ?? "pending")}`} title={grade ? `${workflowCode(grade.gradeCode, "grade", stage)} · ${grade.courseName}` : "Grade pending"} key={grade?.courseCode ?? index}><b>{grade?.grade ?? "—"}</b><small>{grade?.courseCode ?? `C${index + 1}`}</small></span>)}</div>; }
function ResultCard({ insights }: { insights: OperationalRecordInsights }) { return <div className={`student-result-card result-${toneKey(insights.result)}`}><strong>{insights.result}</strong><span>{insights.totalScore.toFixed(1)} ÷ 5</span><b>{insights.average.toFixed(2)}</b></div>; }
function Information({ label, value }: { label: string; value: string }) { return <div><span>{label}</span><strong>{value || "Not recorded"}</strong></div>; }
function slots(insights: OperationalRecordInsights) { return Array.from({ length: insights.expectedCourses || 5 }, (_, index) => insights.grades[index] ?? null); }
function enrollmentValue(row: OperationalRecord, key: string) { return row.activities.find(activity => activity.Activity === "Student enrollment")?.[key] ?? "Not recorded"; }
function recordYear(row: OperationalRecord) { const value = enrollmentValue(row, "Year"); return value !== "Not recorded" ? value : row.identifier.match(/Year\s+[1-4]/i)?.[0] ?? "Not recorded"; }
function recordShift(row: OperationalRecord) { const value = enrollmentValue(row, "Shift"); return value !== "Not recorded" ? value : row.identifier.split(" · ").at(-1) ?? "Not recorded"; }
function identityDetail(identifier: string) { return identifier.split(" · ").slice(1).join(" · "); }
function initials(value: string) { return value.split(" ").map(part => part[0]).join("").slice(0, 2).toUpperCase(); }
function attendanceLabel(value?: string) { return value === "Excused" ? "Permission" : value || "Not recorded"; }
function toneKey(value: string) { return value.toLowerCase().replaceAll(" ", "-"); }
