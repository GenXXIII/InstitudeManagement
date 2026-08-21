import Image from "next/image";
import type { References } from "../management-types";
import type { StudentItem } from "../types/student";
import { ManagementActions } from "./management-actions";

export function StudentRoster({ items, references, onEdit, onDeactivate }: { items: StudentItem[]; references: References; onEdit: (item: StudentItem) => void; onDeactivate: (item: StudentItem) => void }) {
  return <section className="panel horizontal-management-table people-horizontal students-horizontal"><div className="horizontal-management-head"><span>Photo</span><span>Student identity</span><span>Contact</span><span>Department</span><span>Academic level</span><span>Cohort courses</span><span>Weekly classes</span><span>Attendance</span><span>Status</span><span>Actions</span></div>{items.map(item => {
    const cohortSchedule = references.timetable.filter(entry => entry.values.departmentId === item.values.departmentId && entry.values.yearLevel === item.values.year && entry.values.status !== "Cancelled");
    const courses = new Set(cohortSchedule.map(entry => entry.values.courseId)).size;
    const attendance = references.attendance.filter(entry => entry.values.studentId === item.id);
    const present = attendance.filter(entry => entry.values.status === "Present").length;
    const attendanceRate = attendance.length ? Math.round((present / attendance.length) * 100) : 0;
    return <article className="horizontal-management-row" key={item.id}><div className="horizontal-portrait"><Image unoptimized width={48} height={72} src={item.values.photoDataUrl} alt={`${item.values.name} profile`}/></div><div className="horizontal-primary"><strong>{item.values.name}</strong><span>Student ID · {item.values.number}</span></div><div className="horizontal-detail"><span>Email address</span><strong>{item.values.email}</strong></div><div className="horizontal-detail"><span>Assigned department</span><strong>{item.values.department}</strong></div><div className="horizontal-detail"><span>Current cohort</span><strong>Year {item.values.year}</strong></div><div className="relationship-number"><strong>{courses}</strong><span>Related courses</span></div><div className="relationship-number"><strong>{cohortSchedule.length}</strong><span>Scheduled periods</span></div><div className="horizontal-detail"><span>{present} present / {attendance.length} records</span><strong>{attendanceRate}% attendance</strong></div><span className={`table-status ${item.values.status.toLowerCase().replace(" ", "-")}`}>{item.values.status}</span><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></article>;
  })}</section>;
}
