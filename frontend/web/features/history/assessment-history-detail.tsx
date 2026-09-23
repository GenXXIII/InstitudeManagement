"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { BrowserBackButton } from "@/components/browser-back-button";
import { Icon } from "@/components/icon";
import { ErrorPage, LoadingPage, PageHeading } from "@/components/page-primitives";
import { assessmentHistoryGroups } from "./assessment-history-model";
import { historyApi } from "./history-api";
import type { RecordItem } from "./history-types";

export function AssessmentHistoryDetail({ id }: { id: string }) {
  const [rows, setRows] = useState<RecordItem[]>();
  const [error, setError] = useState(false);
  const load = useCallback(() => historyApi.get("", "Grade").then(value => { setRows(value); setError(false); }).catch(() => setError(true)), []);
  useEffect(() => { void load(); }, [load]);
  const group = useMemo(() => assessmentHistoryGroups(rows ?? []).find(item => item.grades.some(grade => grade.recordId === id)), [id, rows]);
  if (error) return <ErrorPage retry={load}/>;
  if (!rows) return <LoadingPage/>;
  if (!group) return <div className="viewport-data-page history-viewport-page"><PageHeading eyebrow="Permanent academic archive" title="Assessment History" description="The requested course assessment record is unavailable." actions={<BrowserBackButton>Back to Assessment History</BrowserBackButton>}/></div>;
  const average = group.grades.length ? group.grades.reduce((sum, item) => sum + Number(item.score || 0), 0) / group.grades.length : 0;

  return <div className="viewport-data-page history-viewport-page assessment-history-detail-page">
    <PageHeading eyebrow="Permanent academic archive" title={group.course} description="Complete read-only assessment record for this Teacher-assigned course roster." actions={<BrowserBackButton>Back to Assessment History</BrowserBackButton>}/>
    <section className="panel assessment-history-detail-head"><span className="assessment-student-icon"><Icon name="archive" size={22}/></span><div><span>Course assessment record</span><h2>{group.course}</h2><p>{group.courseCode} · {group.department} · {group.yearLevel ? `Year ${group.yearLevel}` : "Year not assigned"} · {group.shift || "Shift not assigned"}</p></div><aside><small>Course average</small><strong>{average.toFixed(2)}</strong><b>{group.grades.length} Students</b></aside></section>
    <section className="panel assessment-history-metadata"><div><span>Assigned Teacher</span><strong>{group.teacher}</strong></div><div><span>Academic period</span><strong>{group.academicYear} · {group.term}</strong></div><div><span>Teacher submitted</span><strong>{formatDateTime(group.submittedAtUtc)}</strong></div><div><span>Administrator approved</span><strong>{formatDateTime(group.reviewedAtUtc)}</strong></div><div><span>Submission version</span><strong>{group.submissionVersion}</strong></div><div><span>Roster size</span><strong>{group.grades.length} Students</strong></div></section>
    <section className="assessment-history-student-list" aria-label="Student assessment grades">{group.grades.map(item => <article className="panel assessment-history-student" key={item.recordId}><header><div><span>{item.studentCode || item.gradeCode}</span><h3>{item.student}</h3><p>{item.gradeCode}</p></div><b>{item.grade || "—"}<small>{item.score || "0"}/100</small></b></header><div className="assessment-detail-scores"><Score label="Attendance" value={item.attendanceScore} maximum={item.attendanceMaximum}/><Score label="Assignment" value={item.assignmentScore} maximum={item.assignmentMaximum}/><Score label="Midterm" value={item.midtermScore} maximum={item.midtermMaximum}/><Score label="Final" value={item.finalExamScore} maximum={item.finalExamMaximum}/></div><footer><span>Teacher submitted <strong>{formatDateTime(item.submittedAtUtc)}</strong></span><span>Administrator approved <strong>{formatDateTime(item.reviewedAtUtc)}</strong></span>{item.reviewNote && <p>{item.reviewNote}</p>}</footer></article>)}</section>
  </div>;
}

function Score({ label, value, maximum }: { label: string; value: string; maximum: string }) { return <div><small>{label}</small><strong>{value || "0"}<span>/{maximum || "0"}</span></strong></div>; }
function formatDateTime(value: string) { if (!value) return "Not recorded"; const date = new Date(value); return Number.isNaN(date.valueOf()) ? value : new Intl.DateTimeFormat("en-GB", { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" }).format(date); }
