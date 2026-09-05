"use client";

import Image from "next/image";
import { useRouter } from "next/navigation";
import { Icon } from "@/components/icon";
import { WorkflowCodeFlow } from "@/components/workflow-code-flow";
import { workflowCode, workflowResource, type WorkflowCodeStage } from "@/lib/workflow-code";
import type { OperationalRecord } from "../record-types";

type RecordModule = "Teacher" | "Course" | "Classroom";
type Counts = { present: number; permission: number; absent: number };

export function EntitySemesterRecordHeader({ module, history = false }: { module: RecordModule; history?: boolean }) {
  const relation = module === "Teacher" ? "Assigned courses" : module === "Course" ? "Students" : "Courses";
  return <div className="entity-semester-record-head"><span>{history ? "History code" : "Record code"}</span><span>{module === "Teacher" ? "Photo" : "Type"}</span><span>{module === "Classroom" ? "Classroom" : `${module} name`}</span><span>{module === "Classroom" ? "Building" : "Department"}</span><span>{history ? "Completed level" : "Year"}</span><span>{relation}</span><span>{module === "Teacher" ? "Teacher attendance" : "Running / available"}</span><span>Semester activity</span></div>;
}

export function EntitySemesterRecord({ row, stage = "record", detailHref, detailPage = false }: { row: OperationalRecord; stage?: WorkflowCodeStage; detailHref?: string; detailPage?: boolean }) {
  const router = useRouter();
  const counts = attendanceCounts(completedSessions(row));
  const teacherCounts = teacherAttendanceCounts(completedSessions(row));
  if (detailPage) return <EntityRecordDetail row={row} stage={stage} counts={counts}/>;
  const recordModule = row.module as RecordModule;
  const open = () => { if (detailHref) router.push(detailHref); };
  return <article className="entity-semester-record-row record-row-clickable" role="link" tabIndex={0} onClick={open} onKeyDown={event => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); open(); } }}>
    <div className="workflow-ledger-code"><strong className="entity-record-code">{workflowCode(row.code || row.identifier, workflowResource(row.module), stage)}</strong><small>From {workflowCode(row.code || row.identifier, workflowResource(row.module), "enrollment")}</small></div>
    <EntityVisual row={row}/>
    <div className="entity-record-name"><strong>{row.subject}</strong><span>{row.identifier}</span></div>
    <div className="entity-record-department"><strong>{row.department || "Unassigned"}</strong><span>{row.academicYear} · {row.term}</span></div>
    <div className="entity-record-year"><strong>{yearLabels(row)}</strong><span>Semester level</span></div>
    {recordModule === "Teacher" ? <RelationCount value={relationCount(row)} label="assigned courses"/> : <RelationCount value={relationCount(row)} label={recordModule === "Course" ? "enrolled students" : "assigned courses"}/>}
    {recordModule === "Teacher" ? <TeacherAttendanceCards counts={teacherCounts}/> : <SemesterAvailabilityCards counts={teacherCounts}/>}
    <div className="entity-record-activity"><strong>{completedSessions(row).length}</strong><span>recorded periods</span><small>{row.lastActivityAt ? new Date(row.lastActivityAt).toLocaleDateString() : "No activity"}</small></div>
  </article>;
}

function EntityRecordDetail({ row, stage, counts }: { row: OperationalRecord; stage: WorkflowCodeStage; counts: Counts }) {
  const recordModule = row.module as RecordModule;
  const sessions = completedSessions(row);
  const teacherCounts = teacherAttendanceCounts(sessions);
  const relations = relationNames(row);
  return <article className="entity-semester-detail">
    <header><EntityVisual row={row}/><div><span className="eyebrow">{row.academicYear} · {row.term}</span><h2>{row.subject}</h2><p>{workflowCode(row.code, workflowResource(row.module), stage)} · {row.department} · {row.identifier}</p></div></header>
    <WorkflowCodeFlow sourceCode={row.code} resource={workflowResource(row.module)} currentStage={stage}/>
    <section className="semester-record-information" aria-label={`${recordModule} semester information`}><Information label={`${stage === "history" ? "History" : "Record"} code`} value={workflowCode(row.code, workflowResource(row.module), stage)}/><Information label="Enrollment source" value={workflowCode(row.code, workflowResource(row.module), "enrollment")}/><Information label={recordModule === "Classroom" ? "Building" : "Department"} value={row.department}/><Information label="Year" value={yearLabels(row)}/><Information label="Academic year" value={row.academicYear}/><Information label="Semester" value={row.term}/><Information label={recordModule === "Teacher" ? "Assigned courses" : recordModule === "Course" ? "Enrolled students" : "Assigned courses"} value={relationCount(row).toString()}/><Information label="State" value={recordModule === "Teacher" ? row.status : "Running / Available"}/></section>
    <div className="entity-detail-insights">{recordModule === "Teacher" ? <><section><span>Student attendance</span><AttendanceCards counts={counts}/></section><section><span>Teacher attendance</span><TeacherAttendanceCards counts={teacherCounts}/></section></> : <><section className="entity-semester-total"><span>{recordModule === "Course" ? "Enrolled students" : "Assigned courses"}</span><strong>{relationCount(row)}</strong><small>{yearLabels(row)}</small></section><section><span>Current state</span><SemesterAvailabilityCards counts={teacherCounts}/></section></>}<section className="entity-semester-total"><span>Semester activity</span><strong>{sessions.length}</strong><small>recorded periods</small></section></div>
    {relations.length > 0 && <section className="student-record-detail-section entity-relation-detail"><header><div><strong>{recordModule === "Classroom" ? "Courses assigned to this classroom" : recordModule === "Course" ? "Students enrolled in this course" : "Courses assigned to this teacher"}</strong><span>Relationship details frozen inside this semester</span></div><b>{relations.length} records</b></header><div>{relations.map(relation => <article key={relation.name}><span><Icon name={recordModule === "Course" ? "users" : "book"} size={15}/></span><div><strong>{relation.name}</strong><small>{relation.detail}</small></div></article>)}</div></section>}
    <section className="entity-session-register"><header><div><strong>Full class-session detail</strong><span>Held and not-held periods with teacher evidence and student snapshot</span></div><b>{sessions.length} sessions</b></header><div>{sessions.length ? sessions.map((session, index) => <EntitySessionCard session={session} module={recordModule} key={`${session.Date}-${session.Time}-${index}`}/>) : <p className="student-record-detail-empty">No recorded class sessions in this semester.</p>}</div></section>
  </article>;
}

function EntitySessionCard({ session, module }: { session: Record<string, string>; module: RecordModule }) {
  const counts = activityCounts(session);
  const students = studentSnapshot(session.Students);
  const title = module === "Course" ? session.Teacher : session.Course;
  const context = module === "Classroom" ? `${session.Teacher} · ${session.Year}` : `Room ${session.Classroom} · ${session.Year}${module === "Teacher" ? ` · Teacher ${session["Teacher attendance"] || "Present"}` : ""}`;
  const classHeld = session["Session status"] === "Running";
  const availabilityState = module !== "Teacher";
  const state = classHeld ? "Running" : availabilityState ? "Available" : session["Session status"];
  const teacherAttendance = session["Teacher attendance"] || "Present";
  const teacherTone = teacherAttendance === "Absent" ? "absent" : teacherAttendance === "Permission" || teacherAttendance === "Excused" ? "permission" : "present";
  const stateClass = availabilityState ? `class-${state.toLowerCase()}` : `teacher-${teacherTone}`;
  return <article className={`entity-session-card ${stateClass}`}><div className="entity-session-main"><div><time>{session.Date}</time><strong>{session.Time}</strong><span>{state}</span></div><div><strong>{title || "Recorded class"}</strong><span>{context}</span><small>{session.Reason}</small></div>{classHeld ? <AttendanceCards counts={counts}/> : <div className="entity-session-not-held"><strong>{availabilityState ? "Available" : "Not held"}</strong><span>{availabilityState ? `Teacher ${session["Teacher attendance"]?.toLowerCase() || "absent"}` : session["Teacher attendance"]}</span></div>}</div>{students.length > 0 && <details><summary>View {students.length} student attendance details</summary><div className="entity-student-snapshot">{students.map((student, index) => <span className={`attendance-${toneKey(student.status)}`} key={`${student.name}-${index}`}><strong>{student.name}</strong><b>{attendanceLabel(student.status)}</b></span>)}</div></details>}</article>;
}

function EntityVisual({ row }: { row: OperationalRecord }) {
  if (row.module === "Teacher" && row.photoDataUrl) return <Image className="entity-record-photo" src={row.photoDataUrl} alt={`${row.subject} portrait`} width={42} height={58} unoptimized/>;
  if (row.module === "Teacher") return <span className="entity-record-photo entity-record-fallback">{initials(row.subject)}</span>;
  return <span className={`entity-record-type entity-type-${row.module.toLowerCase()}`}><Icon name={row.module === "Classroom" ? "room" : "book"} size={17}/><small>{row.module === "Classroom" ? roomType(row.identifier) : "Course"}</small></span>;
}

function Information({ label, value }: { label: string; value: string }) { return <div><span>{label}</span><strong>{value || "Not recorded"}</strong></div>; }
function RelationCount({ value, label }: { value: number; label: string }) { return <div className="entity-relation-count"><strong>{value}</strong><span>{label}</span></div>; }
function AttendanceCards({ counts }: { counts: Counts }) { return <div className="entity-attendance-cards"><span className="present"><b>{counts.present}</b><small>Present</small></span><span className="permission"><b>{counts.permission}</b><small>Permission</small></span><span className="absent"><b>{counts.absent}</b><small>Absent</small></span></div>; }
function TeacherAttendanceCards({ counts }: { counts: Counts }) { return <div className="entity-attendance-cards"><span className="present"><b>{counts.present}</b><small>Present</small></span><span className="permission"><b>{counts.permission}</b><small>Permission</small></span><span className="absent"><b>{counts.absent}</b><small>Absent</small></span></div>; }
function SemesterAvailabilityCards({ counts }: { counts: Counts }) { return <div className="semester-availability-cards"><span className="running"><b>{counts.present}</b><small>Running</small></span><span className="available"><b>{counts.permission + counts.absent}</b><small>Available</small></span></div>; }
function completedSessions(row: OperationalRecord) { return row.activities.filter(activity => activity.Activity === "Completed class"); }
function yearLabels(row: OperationalRecord) { const years = [...new Set(row.activities.flatMap(activity => activity.Year?.match(/Year\s+[1-4]/gi) ?? []))]; return years.sort().join(", ") || "Not scheduled"; }
function relationCount(row: OperationalRecord) { const key = row.module === "Course" ? "Enrolled students" : row.module === "Classroom" ? "Course count" : "Assigned courses"; const values = row.activities.map(activity => Number(activity[key] || 0)); return values.length ? Math.max(...values) : relationNames(row).length; }
function relationNames(row: OperationalRecord) {
  if (row.module === "Course") { const names = new Map<string, string>(); for (const activity of completedSessions(row)) for (const student of studentSnapshot(activity.Students)) names.set(student.name, student.status); return [...names].map(([name, detail]) => ({ name, detail })); }
  const names = new Map<string, number>();
  for (const activity of completedSessions(row)) if (activity.Course) names.set(activity.Course, (names.get(activity.Course) ?? 0) + 1);
  if (row.module === "Classroom") for (const activity of row.activities.filter(item => item.Activity === "Classroom assignment")) for (const course of (activity.Courses ?? "").split("; ").filter(value => value && value !== "No enrolled courses")) if (!names.has(course)) names.set(course, 0);
  if (row.module === "Teacher") for (const activity of completedSessions(row)) if (activity.Course && !names.has(activity.Course)) names.set(activity.Course, 0);
  return [...names].map(([name, count]) => ({ name, detail: `${count} recorded period${count === 1 ? "" : "s"}` }));
}
function attendanceCounts(activities: Record<string, string>[]) { return activities.reduce((total, activity) => { const counts = activityCounts(activity); return { present: total.present + counts.present, permission: total.permission + counts.permission, absent: total.absent + counts.absent }; }, { present: 0, permission: 0, absent: 0 }); }
function teacherAttendanceCounts(activities: Record<string, string>[]) { return activities.reduce((total, activity) => { const status = activity["Teacher attendance"] || "Present"; if (status === "Permission") total.permission++; else if (status === "Absent") total.absent++; else total.present++; return total; }, { present: 0, permission: 0, absent: 0 }); }
function activityCounts(activity: Record<string, string>): Counts { return { present: Number(activity.Present || 0), permission: Number(activity.Permission || 0), absent: Number(activity.Absent || 0) }; }
function studentSnapshot(value?: string) { return (value ?? "").split("; ").filter(Boolean).map(entry => { const split = entry.lastIndexOf(": "); return split < 0 ? { name: entry, status: "Enrolled" } : { name: entry.slice(0, split), status: entry.slice(split + 2) }; }); }
function attendanceLabel(value: string) { return value === "Excused" ? "Permission" : value; }
function toneKey(value: string) { return value.toLowerCase().replaceAll(" ", "-"); }
function initials(value: string) { return value.split(" ").map(part => part[0]).join("").slice(0, 2).toUpperCase(); }
function roomType(value: string) { return value.split(" · ")[0] || "Room"; }
