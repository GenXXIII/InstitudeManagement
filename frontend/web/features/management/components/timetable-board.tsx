"use client";

import { useEffect, useMemo, useState } from "react";
import type { References } from "../management-types";
import { timetableApi } from "../timetable/timetable-api";
import type { TimetableItem, TimetablePeriod } from "../types/timetable";
import { ManagementActions } from "./management-actions";

const days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
const years = [1, 2, 3, 4];

export function TimetableBoard({ items, references, onEdit, onDeactivate }: { items: TimetableItem[]; references: References; onEdit: (item: TimetableItem) => void; onDeactivate: (item: TimetableItem) => void }) {
  const [selectedDay, setSelectedDay] = useState(() => days[(new Date().getDay() + 6) % 7]);
  const [selectedYear, setSelectedYear] = useState(0);
  const [periods, setPeriods] = useState<TimetablePeriod[]>([]);

  useEffect(() => { timetableApi.getPeriods().then(setPeriods).catch(() => setPeriods([])); }, []);

  const dayGroup = selectedDay === "Saturday" || selectedDay === "Sunday" ? "Weekend" : "Weekday";
  const visiblePeriods = periods.filter(period => period.dayGroup === dayGroup);
  const visibleItems = items.filter(item => item.values.dayOfWeek === selectedDay && (!selectedYear || Number(item.values.yearLevel) === selectedYear));
  const rooms = references.classrooms.filter(room => room.values.status !== "Inactive").toSorted((a, b) => a.values.code.localeCompare(b.values.code, undefined, { numeric: true }));
  const teacherCount = new Set(visibleItems.map(item => item.values.teacherId)).size;
  const courseCount = new Set(visibleItems.map(item => item.values.courseId)).size;

  const cohortStats = useMemo(() => {
    const stats = new Map<string, { students: number; attendance: number }>();
    for (const student of references.students.filter(student => student.values.status !== "Inactive")) {
      const key = `${student.values.departmentId}|${student.values.year}`;
      const current = stats.get(key) ?? { students: 0, attendance: 0 };
      current.students += 1;
      current.attendance += references.attendance.filter(record => record.values.studentId === student.id).length;
      stats.set(key, current);
    }
    return stats;
  }, [references.attendance, references.students]);

  return <section className="management-timetable-workspace"><div className="panel management-timetable-controls"><div className="management-timetable-days">{days.map(day => <button className={selectedDay === day ? "active" : ""} onClick={() => setSelectedDay(day)} key={day}>{day}</button>)}</div><div className="management-timetable-years"><button className={selectedYear === 0 ? "active" : ""} onClick={() => setSelectedYear(0)}>All years</button>{years.map(year => <button className={selectedYear === year ? "active" : ""} onClick={() => setSelectedYear(year)} key={year}>Year {year}</button>)}</div></div><div className="management-timetable-summary"><span><strong>{rooms.length}</strong> learning spaces</span><span><strong>{visibleItems.length}</strong> classes</span><span><strong>{courseCount}</strong> courses</span><span><strong>{teacherCount}</strong> teachers</span><i>Students and attendance are linked by department and year.</i></div><div className="panel management-room-matrix" style={{ "--management-periods": Math.max(visiblePeriods.length, 1) } as React.CSSProperties}><header><div><strong>Learning space</strong><span>All 13 rooms remain visible</span></div>{visiblePeriods.map(period => <div key={`${period.startsAt}-${period.endsAt}`}><span>{period.session}</span><strong>{period.startsAt}–{period.endsAt}</strong></div>)}</header><div className="management-room-matrix-body">{rooms.map(room => <div className="management-room-row" key={room.id}><div className={`management-room-label ${room.values.roomType === "Meeting Room" ? "meeting" : ""}`}><div><strong>{room.values.code}</strong><span>{room.values.roomType} · {room.values.capacity} seats</span><small>{room.values.department}</small></div><b className={`table-status ${room.values.studyStatus.toLowerCase().replace(" ", "-")}`}>{room.values.studyStatus}</b></div>{visiblePeriods.map(period => {
    const scheduled = visibleItems.filter(item => item.values.classroomId === room.id && item.values.startsAt === period.startsAt && item.values.endsAt === period.endsAt);
    return <div className="management-room-period" key={`${period.startsAt}-${period.endsAt}`}>{scheduled.length ? scheduled.map(item => {
      const stats = cohortStats.get(`${item.values.departmentId}|${item.values.yearLevel}`) ?? { students: 0, attendance: 0 };
      return <article className="management-class-card" key={item.id}><div className="management-class-title"><b>Year {item.values.yearLevel}</b><span className={`table-status ${item.values.status.toLowerCase()}`}>{item.values.status}</span></div><strong>{item.values.course}</strong><span>{item.values.teacher}</span><small>{stats.students} students · {stats.attendance} attendance records</small><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></article>;
    }) : <span className="management-room-available">Available</span>}</div>;
  })}</div>)}</div></div></section>;
}
