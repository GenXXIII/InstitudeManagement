import Image from "next/image";
import type { References } from "../management-types";
import type { TeacherItem } from "../types/teacher";
import { ManagementActions } from "./management-actions";
import { ManagementDataCell } from "./management-data-cell";

export function TeacherRoster({ items, references, onEdit, onDeactivate }: { items: TeacherItem[]; references: References; onEdit: (item: TeacherItem) => void; onDeactivate: (item: TeacherItem) => void }) {
  return <section className="panel horizontal-management-table people-horizontal teachers-horizontal"><div className="horizontal-management-head"><span>TeacherCode</span><span>Photo</span><span>Teacher Name</span><span>Email</span><span>Courses</span><span>Weekly classes</span><span>Learning spaces</span><span>Create At</span><span>Actions</span></div>{items.map(item => {
    const schedule = references.timetable.filter(entry => entry.values.teacherId === item.id && entry.values.status !== "Cancelled");
    const courses = new Set(schedule.map(entry => entry.values.courseId)).size;
    const rooms = new Set(schedule.map(entry => entry.values.classroomId)).size;
    return <article className="horizontal-management-row" key={item.id}>
      <ManagementDataCell label="TeacherID"><strong className="management-code-value">{item.values.teacherCode}</strong></ManagementDataCell>
      <ManagementDataCell label="Photo" className="horizontal-portrait"><Image unoptimized width={48} height={72} src={item.values.photoDataUrl} alt={`${item.values.name} profile`}/></ManagementDataCell>
      <ManagementDataCell label="Teacher Name" className="horizontal-primary"><strong>{item.values.name}</strong></ManagementDataCell>
      <ManagementDataCell label="Email" className="horizontal-detail"><strong>{item.values.email}</strong></ManagementDataCell>
      <ManagementDataCell label="Assigned courses" className="relationship-number"><strong>{courses}</strong></ManagementDataCell>
      <ManagementDataCell label="Scheduled periods" className="relationship-number"><strong>{schedule.length}</strong></ManagementDataCell>
      <ManagementDataCell label="Rooms used" className="relationship-number"><strong>{rooms}</strong></ManagementDataCell>
      <ManagementDataCell label="Create At" className="horizontal-detail"><strong>{item.values.createAt}</strong></ManagementDataCell>
      <ManagementDataCell label="Actions" className="management-action-cell"><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></ManagementDataCell>
    </article>;
  })}</section>;
}
