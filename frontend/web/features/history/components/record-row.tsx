"use client";

import { useState } from "react";
import { Icon } from "@/components/icon";
import type { RecordGroup } from "../history-types";
import { displayValue, formatDate, isInactive, isTechnicalField, pretty } from "../history-utils";
import { HistoryEntry } from "./history-entry";

export function RecordRow({ group }: { group: RecordGroup }) {
  const [open, setOpen] = useState(false);
  const preview = group.values.filter(([key]) => !isTechnicalField(key)).slice(0, 3);
  const first = group.entries.at(-1); const latest = group.entries[0];
  return <article className={`record-row history-management-row ${open ? "open" : ""}`}><div className="record-row-main"><div className="record-identity"><div className="record-profile-mark"><Icon name={recordIcon(group.type)} size={20}/></div><div><span>{group.type}</span><strong>{group.subject}</strong><small>{group.entries.length} snapshot{group.entries.length === 1 ? "" : "s"} · since {first ? formatDate(first.date) : "—"}</small></div></div><div className="record-latest-details">{preview.map(([key, value]) => <span key={key}><b>{pretty(key)}:</b> {displayValue(key, value)}</span>)}</div><span className={`record-lifecycle ${isInactive(group.status) ? "inactive" : "current"}`}>{group.status}</span><time>{latest ? formatDate(latest.date) : "—"}</time><button className="record-dropdown-button" onClick={() => setOpen(value => !value)} aria-expanded={open}><span>{open ? "Hide details" : "View details"}</span><b>{open ? "−" : "+"}</b></button></div>{open && <div className="record-row-history"><div className="record-history-heading"><strong>Complete lifecycle and data snapshots</strong><span>Newest snapshot first</span></div>{group.entries.map(entry => <HistoryEntry entry={entry} key={entry.id}/>)}</div>}</article>;
}

function recordIcon(type: string): Parameters<typeof Icon>[0]["name"] { return type === "Student" ? "users" : type === "Teacher" ? "teacher" : type === "Classroom" ? "room" : type === "Course" ? "book" : type === "Timetable" ? "calendar" : type === "Attendance" ? "check" : type === "Department" ? "building" : type === "Grade" ? "grade" : "archive"; }
