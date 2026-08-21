"use client";

import { useState } from "react";
import { Icon } from "@/components/icon";
import type { OperationalRecord } from "../record-types";

export function OperationalRecordRow({ row }: { row: OperationalRecord }) {
  const [open, setOpen] = useState(false);
  return <article className={`operational-record-row ${open ? "open" : ""}`}><div className="operational-record-main"><div className="operational-record-identity"><span className="operational-record-icon"><Icon name={recordIcon(row.module)} size={17}/></span><div><strong>{row.subject}</strong><span>{row.identifier}</span></div></div><p>{row.summary}</p><span className={`table-status ${row.status.toLowerCase().replaceAll(" ", "-")}`}>{row.status}</span><time>{row.lastActivityAt ? new Date(row.lastActivityAt).toLocaleString() : "No activity yet"}</time><button className="record-dropdown-button" onClick={() => setOpen(value => !value)} aria-expanded={open} disabled={!row.activities.length}><span>{row.activities.length ? open ? "Hide activity" : "View activity" : "No activity"}</span><b>{row.activities.length ? open ? "−" : "+" : "—"}</b></button></div>{open && <div className="operational-activity-list"><div className="record-history-heading"><strong>Recorded operational activity</strong><span>{row.activities.length} entries · newest first</span></div>{row.activities.map((activity, index) => <div className="operational-activity-entry" key={`${row.id}-${index}`}>{Object.entries(activity).map(([key, value]) => <div key={key}><span>{key}</span><strong>{value}</strong></div>)}</div>)}</div>}</article>;
}

function recordIcon(module: string): Parameters<typeof Icon>[0]["name"] {
  return module === "Student" || module === "Attendance" ? "users" : module === "Teacher" ? "teacher" : module === "Classroom" ? "room" : "book";
}
