"use client";

import { useMemo, useState } from "react";
import type { References } from "../management-types";
import type { TimetableItem } from "../types/timetable";
import { ManagementActions } from "./management-actions";
import { ManagementDataCell } from "./management-data-cell";

const days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
const years = [1, 2, 3, 4];

export function TimetableBoard({ items, references, onEdit, onDeactivate }: { items: TimetableItem[]; references: References; onEdit: (item: TimetableItem) => void; onDeactivate: (item: TimetableItem) => void }) {
  const [selectedDay, setSelectedDay] = useState("All days");
  const [selectedYear, setSelectedYear] = useState(0);
  const [search, setSearch] = useState("");
  const studentCounts = useMemo(() => {
    const counts = new Map<string, number>();
    for (const student of references.students.filter(student => student.values.status !== "Inactive")) {
      const key = `${student.values.departmentId}|${student.values.year}|${student.values.shift}`;
      counts.set(key, (counts.get(key) ?? 0) + 1);
    }
    return counts;
  }, [references.students]);
  const visible = items
    .filter(item => selectedDay === "All days" || item.values.dayOfWeek === selectedDay)
    .filter(item => !selectedYear || Number(item.values.yearLevel) === selectedYear)
    .filter(item => !search || [item.values.course, item.values.teacher, item.values.classroom, item.values.department].some(value => value.toLowerCase().includes(search.toLowerCase())))
    .toSorted((left, right) => days.indexOf(left.values.dayOfWeek) - days.indexOf(right.values.dayOfWeek) || left.values.startsAt.localeCompare(right.values.startsAt) || left.values.classroom.localeCompare(right.values.classroom, undefined, { numeric: true }));
  return <section className="management-timetable-data">
    <div className="panel timetable-data-filters">
      <label><span>Search timetable</span><input value={search} onChange={event => setSearch(event.target.value)} placeholder="Course, teacher, room, department..."/></label>
      <label><span>Day</span><select value={selectedDay} onChange={event => setSelectedDay(event.target.value)}><option>All days</option>{days.map(day => <option key={day}>{day}</option>)}</select></label>
      <label><span>Student year</span><select value={selectedYear} onChange={event => setSelectedYear(Number(event.target.value))}><option value={0}>All years</option>{years.map(year => <option value={year} key={year}>Year {year}</option>)}</select></label>
      <div className="timetable-data-count"><span>Showing</span><strong>{visible.length}</strong><small>matching classes</small></div>
    </div>
    <div className="panel timetable-data-table">
      <div className="timetable-data-head"><span>TimetableCode</span><span>Day & time</span><span>Course</span><span>Department / cohort</span><span>Teacher</span><span>Learning room</span><span>Students</span><span>Create At</span><span>Actions</span></div>
      <div className="timetable-data-body">{visible.map(item => {
        const students = studentCounts.get(`${item.values.departmentId}|${item.values.yearLevel}|${session(item.values.startsAt)}`) ?? 0;
        return <article className="timetable-data-row" key={item.id}>
          <ManagementDataCell label="TimetableID"><strong className="management-code-value">{item.values.timetableCode}</strong></ManagementDataCell>
          <ManagementDataCell label="Day and time" className="timetable-time-data"><strong>{item.values.dayOfWeek}</strong><time>{item.values.startsAt} - {item.values.endsAt}</time><small>{session(item.values.startsAt)}</small></ManagementDataCell>
          <ManagementDataCell label="Course" className="timetable-course-data"><strong>{item.values.course}</strong></ManagementDataCell>
          <ManagementDataCell label="Department / cohort" className="timetable-detail-data"><strong>{item.values.department}</strong><small>Year {item.values.yearLevel}</small></ManagementDataCell>
          <ManagementDataCell label="Teacher" className="timetable-detail-data"><strong>{item.values.teacher}</strong></ManagementDataCell>
          <ManagementDataCell label="Learning room" className="timetable-room-data"><strong>{item.values.classroom}</strong><small>{item.values.classroomType}</small></ManagementDataCell>
          <ManagementDataCell label="Students" className="timetable-student-count"><strong>{students}</strong><small>Enrolled</small></ManagementDataCell>
          <ManagementDataCell label="Create At" className="timetable-detail-data"><strong>{item.values.createAt}</strong></ManagementDataCell>
          <ManagementDataCell label="Actions" className="management-action-cell"><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></ManagementDataCell>
        </article>;
      })}</div>
      {!visible.length && <div className="empty-state"><strong>No timetable rows found</strong><span>Change the day, year, or search filter.</span></div>}
    </div>
  </section>;
}

function session(startsAt: string) { const hour = Number(startsAt.slice(0, 2)); return hour >= 17 ? "Evening" : hour >= 13 ? "Afternoon" : "Morning"; }
