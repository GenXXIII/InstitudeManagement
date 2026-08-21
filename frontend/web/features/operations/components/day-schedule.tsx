import type { WeeklyTimetableSlot } from "../operations-types";
import { isCurrentTime } from "../operation-utils";

export function DaySchedule({ day, today, now, rows }: { day: string; today: string; now: Date; rows: WeeklyTimetableSlot[] }) {
  const isToday = day === today;
  return <section className={`week-day-column ${isToday ? "today" : ""}`}><header><strong>{day.slice(0, 3)}</strong><span>{isToday ? "Today" : day}</span></header><div className="week-course-list">{rows.length ? rows.map(row => { const current = isToday && isCurrentTime(row, now); return <article className={current ? "current-course" : ""} key={row.id}><time>{row.startsAt} – {row.endsAt}</time><strong>{row.course}</strong><span>{row.teacher}</span><small>Room {row.room}</small>{current && <b>Live now</b>}</article>; }) : <div className="week-empty">No classes</div>}</div></section>;
}
