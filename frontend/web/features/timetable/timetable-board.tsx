"use client";

import { useState } from "react";
import { ManagementDataCell } from "@/components/management-data-cell";
import { ManagementActions } from "@/features/management/components/management-actions";
import { workflowCode } from "@/lib/workflow-code";
import type { TimetableItem } from "./timetable-types";

const days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

export function TimetableBoard({ items, onEdit, onDeactivate }: { items: TimetableItem[]; onEdit: (item: TimetableItem) => void; onDeactivate: (item: TimetableItem) => void }) {
  const [selectedDay, setSelectedDay] = useState("All days");
  const [search, setSearch] = useState("");
  const visible = items
    .filter(item => selectedDay === "All days" || item.values.dayOfWeek === selectedDay)
    .filter(item => !search || [item.values.timetableCode, item.values.dayOfWeek, item.values.startsAt, item.values.endsAt, item.values.createAt].some(value => value.toLowerCase().includes(search.toLowerCase())))
    .toSorted((left, right) => days.indexOf(left.values.dayOfWeek) - days.indexOf(right.values.dayOfWeek) || left.values.startsAt.localeCompare(right.values.startsAt) || left.values.classroom.localeCompare(right.values.classroom, undefined, { numeric: true }));
  return <section className="management-timetable-data">
    <div className="panel timetable-data-filters">
      <label><span>Search schedule</span><input value={search} onChange={event => setSearch(event.target.value)} placeholder="Code, time, day, created date..."/></label>
      <label><span>Day</span><select value={selectedDay} onChange={event => setSelectedDay(event.target.value)}><option>All days</option>{days.map(day => <option key={day}>{day}</option>)}</select></label>
      <div className="timetable-data-count"><span>Showing</span><strong>{visible.length}</strong><small>matching schedules</small></div>
    </div>
    <div className="panel timetable-data-table">
      <div className="timetable-data-head"><span>Code</span><span>Time</span><span>Day</span><span>Create At</span><span>Actions</span></div>
      <div className="timetable-data-body">{visible.map(item => {
        return <article className="timetable-data-row" key={item.id}>
          <ManagementDataCell label="Code"><strong className="management-code-value">{workflowCode(item.values.timetableCode, "timetable", "management")}</strong></ManagementDataCell>
          <ManagementDataCell label="Time" className="timetable-time-data"><time>{item.values.startsAt} - {item.values.endsAt}</time></ManagementDataCell>
          <ManagementDataCell label="Day" className="timetable-detail-data"><strong>{item.values.dayOfWeek}</strong></ManagementDataCell>
          <ManagementDataCell label="Create At" className="timetable-detail-data"><strong>{item.values.createAt}</strong></ManagementDataCell>
          <ManagementDataCell label="Actions" className="management-action-cell"><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></ManagementDataCell>
        </article>;
      })}</div>
      {!visible.length && <div className="empty-state"><strong>No schedule rows found</strong><span>Change the day or search filter.</span></div>}
    </div>
  </section>;
}
