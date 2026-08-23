import type { CourseItem } from "../types/course";
import { ManagementActions } from "./management-actions";
import { ManagementDataCell } from "./management-data-cell";

export function CourseBoard({ items, onEdit, onDeactivate }: { items: CourseItem[]; onEdit: (item: CourseItem) => void; onDeactivate: (item: CourseItem) => void }) {
  return <section className="panel horizontal-management-table course-horizontal"><div className="horizontal-management-head"><span>CourseCode</span><span>Course Name</span><span>Department</span><span>Teacher Name</span><span>Capacity</span><span>Create At</span><span>Actions</span></div>{items.map(item => <article className="horizontal-management-row" key={item.id}>
    <ManagementDataCell label="CourseID"><strong className="management-code-value">{item.values.courseCode}</strong></ManagementDataCell>
    <ManagementDataCell label="Course Name" className="horizontal-primary"><strong>{item.values.name}</strong></ManagementDataCell>
    <ManagementDataCell label="Department" className="horizontal-detail"><strong>{item.values.department}</strong></ManagementDataCell>
    <ManagementDataCell label="Teacher Name" className="horizontal-detail"><strong>{item.values.teacher}</strong></ManagementDataCell>
    <ManagementDataCell label="Capacity" className="relationship-number"><strong>{item.values.capacity}</strong></ManagementDataCell>
    <ManagementDataCell label="Create At" className="horizontal-detail"><strong>{item.values.createAt}</strong></ManagementDataCell>
    <ManagementDataCell label="Actions" className="management-action-cell"><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></ManagementDataCell>
  </article>)}</section>;
}
