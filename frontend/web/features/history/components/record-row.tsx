import Link from "next/link";
import { Icon } from "@/components/icon";
import { workflowCode, workflowResource } from "@/lib/workflow-code";
import type { RecordGroup } from "../history-types";
import { displayValue, formatDate, isHistoryFieldVisible, isTechnicalField, pretty } from "../history-utils";

export function RecordRow({ group, detailHref }: { group: RecordGroup; detailHref: string }) {
  const preview = group.values.filter(([key]) => !isTechnicalField(key) && isHistoryFieldVisible(key)).slice(0, 3);
  const first = group.entries.at(-1); const latest = group.entries[0];
  const sourceCode = historyBusinessCode(group);
  const code = workflowCode(sourceCode, workflowResource(group.type), "history");
  return <Link className="record-row history-management-row record-row-clickable" href={detailHref}><div className="record-row-main"><div className="record-identity"><div className="record-profile-mark"><Icon name={recordIcon(group.type)} size={20}/></div><div><span>HistoryCode</span><strong>{code}</strong><small>Source {workflowCode(sourceCode, workflowResource(group.type), "management")} · {group.subject !== sourceCode ? `${group.subject} · ` : ""}{latest?.auditLogCode ? `${latest.auditLogCode} · ` : ""}{group.entries.length} snapshot{group.entries.length === 1 ? "" : "s"} · since {first ? formatDate(first.date) : "—"}</small></div></div><div className="record-latest-details">{preview.map(([key, value]) => <span key={key}><b>{pretty(key)}:</b> {displayValue(key, value)}</span>)}</div><time>{latest ? formatDate(latest.date) : "—"}</time></div></Link>;
}

function historyBusinessCode(group: RecordGroup) {
  const expected = historyCodeLabel(group.type).toLowerCase();
  return group.values.find(([key]) => key.replaceAll(" ", "").toLowerCase() === expected)?.[1] ?? group.subject;
}

function historyCodeLabel(type: string) {
  return type === "Class session" ? "ClassSessionRecordCode" : `${type.replaceAll(" ", "")}Code`;
}

function recordIcon(type: string): Parameters<typeof Icon>[0]["name"] { return type === "Student" ? "users" : type === "Teacher" ? "teacher" : type === "Classroom" ? "room" : type === "Course" ? "book" : type === "Timetable" ? "calendar" : type === "Attendance" ? "check" : type === "Department" ? "building" : type === "Grade" ? "grade" : "archive"; }
