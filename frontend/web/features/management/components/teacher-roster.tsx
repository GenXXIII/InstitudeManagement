import Image from "next/image";
import type { References } from "../management-types";
import type { TeacherItem } from "../types/teacher";
import { ManagementActions } from "./management-actions";

export function TeacherRoster({ items, references, onEdit, onDeactivate }: { items: TeacherItem[]; references: References; onEdit: (item: TeacherItem) => void; onDeactivate: (item: TeacherItem) => void }) {
  return <section className="panel horizontal-management-table people-horizontal teachers-horizontal"><div className="horizontal-management-head"><span>Photo</span><span>Teacher identity</span><span>Contact</span><span>Department</span><span>Courses</span><span>Weekly classes</span><span>Learning spaces</span><span>Status</span><span>Actions</span></div>{items.map(item => {
    const schedule = references.timetable.filter(entry => entry.values.teacherId === item.id && entry.values.status !== "Cancelled");
    const courses = new Set(schedule.map(entry => entry.values.courseId)).size;
    const rooms = new Set(schedule.map(entry => entry.values.classroomId)).size;
    return <article className="horizontal-management-row" key={item.id}><div className="horizontal-portrait"><Image unoptimized width={48} height={72} src={item.values.photoDataUrl} alt={`${item.values.name} profile`}/></div><div className="horizontal-primary"><strong>{item.values.name}</strong><span>Teacher ID · {item.values.number}</span></div><div className="horizontal-detail"><span>Email address</span><strong>{item.values.email}</strong></div><div className="horizontal-detail"><span>Assigned department</span><strong>{item.values.department}</strong></div><div className="relationship-number"><strong>{courses}</strong><span>Assigned courses</span></div><div className="relationship-number"><strong>{schedule.length}</strong><span>Scheduled periods</span></div><div className="relationship-number"><strong>{rooms}</strong><span>Rooms used</span></div><span className={`table-status ${item.values.status.toLowerCase().replace(" ", "-")}`}>{item.values.status}</span><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></article>;
  })}</section>;
}
