"use client";

import { useRouter } from "next/navigation";
import { Icon } from "@/components/icon";
import { WorkflowCodeFlow } from "@/components/workflow-code-flow";
import { workflowCode, workflowResource, type WorkflowCodeStage } from "@/lib/workflow-code";
import type { OperationalRecord } from "../record-types";
import { RecordLedgerValue } from "./record-ledger-value";

type StructureModule = "Department" | "Timetable";

export function StructureSemesterRecord({ row, stage = "record", detailHref, detailPage = false }: { row: OperationalRecord; stage?: WorkflowCodeStage; detailHref?: string; detailPage?: boolean }) {
  const router = useRouter();
  if (detailPage) return <StructureRecordDetail row={row} stage={stage}/>;
  const recordModule = row.module as StructureModule;
  const summary = summaryActivity(row);
  const completed = completedSessions(row);
  const open = () => { if (detailHref) router.push(detailHref); };
  return <article className="structure-semester-record-row record-row-clickable" role="link" tabIndex={0} onClick={open} onKeyDown={event => { if (event.key === "Enter" || event.key === " ") { event.preventDefault(); open(); } }}>
    <div className="workflow-ledger-code"><strong className="entity-record-code">{workflowCode(row.code, workflowResource(row.module), stage)}</strong></div>
    <span className={`structure-record-type structure-${recordModule.toLowerCase()}`}><Icon name={recordModule === "Department" ? "building" : "calendar"} size={17}/><small>{recordModule}</small></span>
    <div className="entity-record-name"><strong>{row.subject}</strong></div>
    <div className="entity-record-department"><strong>{recordModule === "Department" ? row.identifier : row.department}</strong></div>
    <div className="entity-record-year"><strong>{yearLabels(row)}</strong></div>
    {recordModule === "Department" ? <>
      <RecordLedgerValue value={summary.Students || "0"} className="record-value-blue"/>
      <RecordLedgerValue value={summary.Teachers || "0"} className="record-value-green"/>
      <RecordLedgerValue value={summary.Courses || "0"} className="record-value-blue"/>
      <RecordLedgerValue value={summary.Classrooms || "0"} className="record-value-green"/>
      <RecordLedgerValue value={completed.length} className="record-value-activity"/>
    </> : <>
      <RecordLedgerValue value={summary.Day || "Weekly"} className="record-value-neutral"/>
      <RecordLedgerValue value={summary.Time || "—"} className="record-value-neutral"/>
      <RecordLedgerValue value={summary.Teacher || "Not assigned"} className="record-value-blue"/>
      <RecordLedgerValue value={summary.Classroom || "—"} className="record-value-green"/>
      <RecordLedgerValue value={summary["Student count"] || "0"} className="record-value-blue"/>
      <RecordLedgerValue value={completed.length} className="record-value-activity"/>
    </>}
  </article>;
}

function StructureRecordDetail({ row, stage }: { row: OperationalRecord; stage: WorkflowCodeStage }) {
  const recordModule = row.module as StructureModule;
  const summary = summaryActivity(row);
  const completed = completedSessions(row);
  const courses = courseNames(row);
  return <article className="structure-semester-detail">
    <header><span className={`structure-record-type structure-${recordModule.toLowerCase()}`}><Icon name={recordModule === "Department" ? "building" : "calendar"} size={22}/><small>{recordModule}</small></span><div><span className="eyebrow">{row.academicYear} · {row.term}</span><h2>{row.subject}</h2><p>{workflowCode(row.code, workflowResource(row.module), stage)} · {recordModule === "Department" ? row.identifier : `${summary.Day} ${summary.Time}`}</p></div></header>
    <WorkflowCodeFlow sourceCode={row.code} resource={workflowResource(row.module)} currentStage={stage}/>
    <section className="semester-record-information"><Information label={`${stage === "history" ? "History" : "Record"} code`} value={workflowCode(row.code, workflowResource(row.module), stage)}/><Information label="Enrollment source" value={workflowCode(row.code, workflowResource(row.module), "enrollment")}/><Information label="Academic year" value={row.academicYear}/><Information label="Semester" value={row.term}/><Information label="Year" value={yearLabels(row)}/>{recordModule === "Department" ? <><Information label="Department head" value={row.identifier}/><Information label="Students" value={summary.Students}/><Information label="Teachers" value={summary.Teachers}/><Information label="Classrooms" value={summary.Classrooms}/></> : <><Information label="Day" value={summary.Day}/><Information label="Time" value={summary.Time}/><Information label="Teacher" value={summary.Teacher}/><Information label="Classroom" value={summary.Classroom}/><Information label="Students" value={summary["Student count"]}/></>}</section>
    <section className="structure-detail-metrics">{recordModule === "Department" ? <><Metric icon="users" label="Students" value={summary.Students}/><Metric icon="teacher" label="Teachers" value={summary.Teachers}/><Metric icon="book" label="Courses" value={summary.Courses}/><Metric icon="room" label="Classrooms" value={summary.Classrooms}/></> : <><Metric icon="users" label="Students" value={summary["Student count"]}/><Metric icon="calendar" label="Completed classes" value={completed.length.toString()}/><Metric icon="teacher" label="Teacher" value={summary["Teacher code"] || summary.Teacher}/><Metric icon="room" label="Classroom" value={summary.Classroom}/></>}</section>
    {courses.length > 0 && <section className="student-record-detail-section entity-relation-detail"><header><div><strong>{recordModule === "Department" ? "Department courses" : "Timetable course relationship"}</strong><span>Course data recorded for this semester</span></div><b>{courses.length} courses</b></header><div>{courses.map(course => <article key={course.name}><span><Icon name="book" size={15}/></span><div><strong>{course.name}</strong><small>{course.detail}</small></div></article>)}</div></section>}
    <section className="structure-session-register"><header><div><strong>Recorded timetable history</strong><span>Held and not-held periods remain under this semester</span></div><b>{completed.length} periods</b></header><div>{completed.length ? completed.map((activity, index) => <article className={activity["Session status"] === "Running" ? "" : "class-not-held"} key={`${activity.Date}-${activity.Time}-${index}`}><div><time>{activity.Date}</time><strong>{activity.Time}</strong></div><div><strong>{activity.Course || row.subject}</strong><span>{activity.Teacher} · Room {activity.Classroom} · {activity.Year}</span><small>{activity.Reason}</small></div><div><strong>{activity["Session status"] || "Running"}</strong><span>{activity["Teacher attendance"] || "Present"}</span></div><p>{activity.Attendance || activity.Reason || "Attendance summary unavailable"}</p></article>) : <p className="student-record-detail-empty">No recorded classes in this semester.</p>}</div></section>
  </article>;
}

function Information({ label, value }: { label: string; value?: string }) { return <div><span>{label}</span><strong>{value || "Not recorded"}</strong></div>; }
function Metric({ icon, label, value }: { icon: Parameters<typeof Icon>[0]["name"]; label: string; value?: string }) { return <article><span><Icon name={icon} size={16}/></span><div><small>{label}</small><strong>{value || "0"}</strong></div></article>; }
function summaryActivity(row: OperationalRecord) { const activity = row.activities.find(item => item.Activity === (row.module === "Department" ? "Department semester" : "Timetable enrollment")); return activity ?? {}; }
function completedSessions(row: OperationalRecord) { return row.activities.filter(activity => activity.Activity === "Completed class"); }
function yearLabels(row: OperationalRecord) { const years = [...new Set(row.activities.flatMap(activity => activity.Year?.match(/Year\s+[1-4]/gi) ?? []))]; return years.sort().join(", ") || "Not scheduled"; }
function courseNames(row: OperationalRecord) {
  const names = new Map<string, number>();
  for (const activity of completedSessions(row)) if (activity.Course) names.set(activity.Course, (names.get(activity.Course) ?? 0) + 1);
  const overview = summaryActivity(row);
  for (const course of (overview["Course names"] ?? overview.Course ?? "").split("; ").filter(Boolean)) if (!names.has(course)) names.set(course, 0);
  return [...names].map(([name, count]) => ({ name, detail: `${count} completed class${count === 1 ? "" : "es"}` }));
}
