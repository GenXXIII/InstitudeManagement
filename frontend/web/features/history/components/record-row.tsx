import Link from "next/link";
import { Icon } from "@/components/icon";
import type { RecordGroup } from "../history-types";
import { displayValue, formatDate, isHistoryFieldVisible, isTechnicalField, pretty } from "../history-utils";

export function RecordRow({ group, detailHref }: { group: RecordGroup; detailHref: string }) {
  const preview = group.values.filter(([key]) => !isTechnicalField(key) && isHistoryFieldVisible(key)).slice(0, 3);
  const first = group.entries.at(-1); const latest = group.entries[0];
  return <Link className="record-row history-management-row record-row-clickable" href={detailHref}><div className="record-row-main"><div className="record-identity"><div className="record-profile-mark"><Icon name={recordIcon(group.type)} size={20}/></div><div><span>{group.type}</span><strong>{group.subject}</strong><small>{latest?.auditLogCode ? `${latest.auditLogCode} · ` : ""}{group.entries.length} snapshot{group.entries.length === 1 ? "" : "s"} · since {first ? formatDate(first.date) : "—"}</small></div></div><div className="record-latest-details">{preview.map(([key, value]) => <span key={key}><b>{pretty(key)}:</b> {displayValue(key, value)}</span>)}</div><time>{latest ? formatDate(latest.date) : "—"}</time></div></Link>;
}

function recordIcon(type: string): Parameters<typeof Icon>[0]["name"] { return type === "Student" ? "users" : type === "Teacher" ? "teacher" : type === "Classroom" ? "room" : type === "Course" ? "book" : type === "Timetable" ? "calendar" : type === "Attendance" ? "check" : type === "Department" ? "building" : type === "Grade" ? "grade" : "archive"; }
