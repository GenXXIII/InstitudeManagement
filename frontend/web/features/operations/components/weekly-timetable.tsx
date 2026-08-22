"use client";

import { useEffect, useState } from "react";
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
  const configured = new Set(periods.flatMap(period => (period.dayGroup === "Weekday" ? days.slice(0, 5) : days.slice(5)).map(day => `${day}|${period.startsAt}|${period.endsAt}`)));
  const customCount = rows.filter(row => !configured.has(`${row.day}|${row.startsAt}|${row.endsAt}`)).length;

  return <div className="weekly-timetable room-timetable"><div className="timetable-control-bar"><div className="timetable-day-tabs">{days.map(day => <button className={`${day === selectedDay ? "active" : ""} ${day === today ? "today" : ""}`} onClick={() => setSelectedDay(day)} key={day}><strong>{day.slice(0, 3)}</strong><span>{day === today ? "Today" : day}</span></button>)}</div><div className="timetable-year-filter"><button className={effectiveYear === 0 ? "active" : ""} disabled={Boolean(globalYear)} onClick={() => setSelectedYear(0)}>All years</button>{years.map(year => <button className={effectiveYear === year ? "active" : ""} disabled={Boolean(globalYear)} onClick={() => setSelectedYear(year)} key={year}>Year {year}</button>)}</div></div><div className="concurrency-note"><strong>{rooms.length} learning spaces</strong><span>Every room may run a different class at the same time when its teacher is available.</span><i>Meeting rooms included</i></div><section className="room-schedule-matrix" style={{ "--period-columns": visiblePeriods.length } as React.CSSProperties}><header><div><strong>Learning space</strong><span>{selectedDay}</span></div>{visiblePeriods.map(period => <div className={`session-${period.session.toLowerCase()}`} key={period.startsAt}><span>{period.session}</span><strong>{period.startsAt}–{period.endsAt}</strong></div>)}</header><div className="room-schedule-body">{rooms.map(room => <div className="room-schedule-row" key={room.id}><div className={`room-schedule-label ${room.roomType === "Meeting Room" ? "meeting" : ""}`}><div><strong>{room.room}</strong><span>{room.roomType}</span></div><b className={`table-status ${statusClass(room.status)}`}>{room.status}</b></div>{visiblePeriods.map(period => {
    const scheduled = visibleRows.filter(row => row.room === room.room && row.startsAt === period.startsAt && row.endsAt === period.endsAt);
    return <div className="room-period-cell" key={period.startsAt}>{scheduled.length ? scheduled.map(row => {
      const current = selectedDay === today && isCurrentTime(row, now);
      return <article className={current ? "current-course" : ""} key={row.id}><div><b>Y{row.yearLevel}</b><strong>{row.course}</strong>{current && <i>Live</i>}</div><span>{row.teacher}</span></article>;
    }) : <span className="room-period-empty">Available</span>}</div>;
  })}</div>)}</div></section>{customCount > 0 && <section className="custom-timetable-warning"><strong>Needs rescheduling</strong><span>{customCount} older timetable entries are outside the configured teaching periods.</span></section>}</div>;
}
