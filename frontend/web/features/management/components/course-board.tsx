import type { CourseItem } from "../types/course";
import type { TimetableItem } from "../types/timetable";
import { ManagementActions } from "./management-actions";
import { ManagementDataCell } from "./management-data-cell";

export function CourseBoard({ items, timetable, onEdit, onDeactivate }: { items: CourseItem[]; timetable: TimetableItem[]; onEdit: (item: CourseItem) => void; onDeactivate: (item: CourseItem) => void }) {
  return <section className="panel horizontal-management-table course-master-horizontal"><div className="horizontal-management-head"><span>CourseCode</span><span>Course Name</span><span>Year Level</span><span>Create At</span><span>Actions</span></div>{items.map(item => <article className="horizontal-management-row" key={item.id}>
    <ManagementDataCell label="CourseID"><strong className="management-code-value">{item.values.courseCode}</strong></ManagementDataCell>
    <ManagementDataCell label="Course Name" className="horizontal-primary"><strong>{item.values.name}</strong></ManagementDataCell>
    <ManagementDataCell label="Year Level" className="horizontal-detail"><strong>{courseYearLabel(item.id, timetable)}</strong></ManagementDataCell>
    <ManagementDataCell label="Create At" className="horizontal-detail"><strong>{item.values.createAt}</strong></ManagementDataCell>
    <ManagementDataCell label="Actions" className="management-action-cell"><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></ManagementDataCell>
  </article>)}</section>;
}

function courseYearLabel(courseId: string, timetable: TimetableItem[]) {
  const years = [...new Set(timetable
    .filter(entry => entry.values.courseId === courseId && entry.values.status !== "Cancelled")
    .map(entry => Number(entry.values.yearLevel))
    .filter(year => Number.isInteger(year) && year >= 1 && year <= 4))]
    .toSorted((left, right) => left - right);
  return years.length ? years.map(year => `Year ${year}`).join(", ") : "Not scheduled";
}
