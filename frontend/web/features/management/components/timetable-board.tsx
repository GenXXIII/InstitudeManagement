import type { LayoutProps } from "../management-types";
import { ManagementActions } from "./management-actions";

const days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

export function TimetableBoard({ items, onEdit, onDeactivate }: LayoutProps) {
  return <section className="timetable-management">{days.map(day => { const rows = items.filter(x => x.values.dayOfWeek === day); return rows.length ? <article className="timetable-day panel" key={day}><div className="timetable-day-title"><span>{day.slice(0, 3)}</span><h3>{day}</h3><small>{rows.length} classes</small></div><div className="timetable-slots">{rows.map(item => <div className="timetable-slot" key={item.id}><time>{item.values.startsAt}<span>–</span>{item.values.endsAt}</time><div><strong>{item.values.course}</strong><span>{item.values.department} · {item.values.teacher} · Room {item.values.classroom}</span></div><span className={`table-status ${item.values.status.toLowerCase()}`}>{item.values.status}</span><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></div>)}</div></article> : null; })}</section>;
}
