"use client";

import { useState } from "react";
import { Icon } from "@/components/icon";
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
    .filter(item => !search || [
      item.values.timetableCode,
      item.values.courseCode,
      item.values.course,
      item.values.teacherCode,
      item.values.teacher,
      item.values.classroom,
      item.values.yearLevel,
      item.values.dayOfWeek,
      item.values.startsAt,
      item.values.endsAt,
      item.values.createAt,
    ].some(value => value.toLowerCase().includes(search.toLowerCase())))
    .toSorted((left, right) => days.indexOf(left.values.dayOfWeek) - days.indexOf(right.values.dayOfWeek) || left.values.startsAt.localeCompare(right.values.startsAt) || left.values.classroom.localeCompare(right.values.classroom, undefined, { numeric: true }));
  return <section className="management-timetable-data">
    <div className="panel timetable-data-filters">
      <label className="management-search module-search-field timetable-module-search"><Icon name="search" size={16}/><input value={search} onChange={event => setSearch(event.target.value)} placeholder="Search code, course, teacher, classroom, year, time, or day..." aria-label="Search schedule"/></label>
      <label><span>Day</span><select value={selectedDay} onChange={event => setSelectedDay(event.target.value)}><option>All days</option>{days.map(day => <option key={day}>{day}</option>)}</select></label>
      <div className="timetable-data-count"><span>Showing</span><strong>{visible.length}</strong><small>matching schedules</small></div>
    </div>
    <div className="panel timetable-data-table">
      <div className="timetable-data-head"><span>Code</span><span>Course</span><span>Teacher</span><span>Classroom</span><span>Student year</span><span>Time</span><span>Day</span><span>Created at</span><span>Actions</span></div>
      <div className="timetable-data-body">{visible.map(item => {
        return <article className="timetable-data-row" key={item.id}>
          <ManagementDataCell label="Code"><strong className="management-code-value">{workflowCode(item.values.timetableCode, "timetable", "management")}</strong></ManagementDataCell>
          <ManagementDataCell label="Course" className="timetable-course-data"><strong>{item.values.course}</strong><span>{item.values.courseCode}</span></ManagementDataCell>
          <ManagementDataCell label="Teacher" className="timetable-course-data"><strong>{item.values.teacher}</strong><span>{item.values.teacherCode}</span></ManagementDataCell>
          <ManagementDataCell label="Classroom" className="timetable-room-data"><strong>{item.values.classroom}</strong><span>{item.values.classroomType}</span></ManagementDataCell>
          <ManagementDataCell label="Student year" className="timetable-detail-data"><strong>Year {item.values.yearLevel}</strong></ManagementDataCell>
          <ManagementDataCell label="Time" className="timetable-time-data"><time>{item.values.startsAt} - {item.values.endsAt}</time></ManagementDataCell>
          <ManagementDataCell label="Day" className="timetable-detail-data"><strong>{item.values.dayOfWeek}</strong></ManagementDataCell>
          <ManagementDataCell label="Created at" className="timetable-detail-data"><strong>{item.values.createAt}</strong></ManagementDataCell>
          <ManagementDataCell label="Actions" className="management-action-cell"><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></ManagementDataCell>
        </article>;
      })}</div>
      {!visible.length && <div className="empty-state"><strong>No schedule rows found</strong><span>Change the day or search filter.</span></div>}
    </div>
  </section>;
}
