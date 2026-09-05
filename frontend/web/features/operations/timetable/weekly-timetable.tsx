"use client";

import { useEffect, useState } from "react";
import { workflowCode } from "@/lib/workflow-code";
import type { TimetablePeriod, TimetableRoom, WeeklyTimetableSlot } from "../operations-types";
import { isCurrentTime, statusClass } from "../operation-utils";

const days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
const years = [1, 2, 3, 4];

export function WeeklyTimetable({ rows, periods, rooms, globalYear }: { rows: WeeklyTimetableSlot[]; periods: TimetablePeriod[]; rooms: TimetableRoom[]; globalYear: number }) {
  const [now, setNow] = useState(() => new Date());
  const [selectedDay, setSelectedDay] = useState(() => days[(new Date().getDay() + 6) % 7]);
  const [selectedYear, setSelectedYear] = useState(0);
  useEffect(() => { const timer = window.setInterval(() => setNow(new Date()), 30_000); return () => window.clearInterval(timer); }, []);
  const effectiveYear = globalYear || selectedYear;
  const today = days[(now.getDay() + 6) % 7];
  const dayGroup = selectedDay === "Saturday" || selectedDay === "Sunday" ? "Weekend" : "Weekday";
  const visiblePeriods = periods.filter(period => period.dayGroup === dayGroup);
  const visibleRows = rows.filter(row => row.day === selectedDay && (!effectiveYear || row.yearLevel === effectiveYear));
  const dayCount = (day: string) => rows.filter(row => row.day === day && (!effectiveYear || row.yearLevel === effectiveYear)).length;
  const configured = new Set(periods.flatMap(period => (period.dayGroup === "Weekday" ? days.slice(0, 5) : days.slice(5)).map(day => `${day}|${period.startsAt}|${period.endsAt}`)));
  const customCount = rows.filter(row => !configured.has(`${row.day}|${row.startsAt}|${row.endsAt}`)).length;

  return <div className="weekly-timetable room-timetable">
    <div className="timetable-control-bar"><div className="timetable-day-tabs">{days.map(day => <button className={`${day === selectedDay ? "active" : ""} ${day === today ? "today" : ""}`} onClick={() => setSelectedDay(day)} key={day}><strong>{day.slice(0, 3)}</strong><span>{day === today ? "Today" : day} · {dayCount(day)}</span></button>)}</div><div className="timetable-year-filter"><button className={effectiveYear === 0 ? "active" : ""} disabled={Boolean(globalYear)} onClick={() => setSelectedYear(0)}>All years</button>{years.map(year => <button className={effectiveYear === year ? "active" : ""} disabled={Boolean(globalYear)} onClick={() => setSelectedYear(year)} key={year}>Year {year}</button>)}</div></div>
    <div className="concurrency-note"><strong>Semester course rotation</strong><span>Every operation code comes from its saved EnrollmentCode.</span><i>{rooms.length} enrolled learning spaces</i></div>
    <section className="room-schedule-matrix" style={{ "--period-columns": visiblePeriods.length } as React.CSSProperties}>
      <header><div><strong>Enrolled learning space</strong><span>{selectedDay} · {visibleRows.length} classes</span></div>{visiblePeriods.map(period => <div className={`session-${period.session.toLowerCase()}`} key={period.startsAt}><span>{period.session}</span><strong>{period.startsAt}–{period.endsAt}</strong></div>)}</header>
      <div className="room-schedule-body">{rooms.map(room => <div className="room-schedule-row" key={room.id}>
        <div className="room-schedule-label"><div><strong>{workflowCode(room.enrollmentCode, "classroom", "operation")}</strong><span>{room.enrollmentCode} · {room.roomType}</span></div><b className={`table-status ${statusClass(room.status)}`}>{room.status}</b></div>
        {visiblePeriods.map(period => {
          const scheduled = visibleRows.filter(row => row.room === room.room && row.startsAt === period.startsAt && row.endsAt === period.endsAt);
          return <div className="room-period-cell" key={period.startsAt}>{scheduled.length ? scheduled.map(row => {
            const current = selectedDay === today && isCurrentTime(row, now);
            const running = current && row.status === "Running";
            const teacherMissing = current && row.status === "Available" && (row.teacherAttendance === "Absent" || row.teacherAttendance === "Permission");
            return <article className={`${running ? "current-course" : ""} ${teacherMissing ? "teacher-missing-course" : ""}`} key={row.id}><div><b>{workflowCode(row.enrollmentCode, "timetable", "operation")}</b><strong>{row.course}</strong>{current && <i>{running ? "Live" : row.status}</i>}</div><span>{row.enrollmentCode} · {row.teacher} · Year {row.yearLevel}{current && !running ? ` · ${row.statusDetail}` : ""}</span></article>;
          }) : <span className="room-period-empty">Available</span>}</div>;
        })}
      </div>)}</div>
    </section>
    {customCount > 0 && <section className="custom-timetable-warning"><strong>Needs rescheduling</strong><span>{customCount} older timetable entries are outside the configured teaching periods.</span></section>}
  </div>;
}
