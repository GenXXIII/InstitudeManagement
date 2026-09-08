import { ManagementDataCell } from "@/components/management-data-cell";
import { workflowCode } from "@/lib/workflow-code";
import type { CourseItem } from "@/features/management/courses/course-types";
import { ManagementActions } from "../components/management-actions";

export function CourseBoard({ items, onEdit, onDeactivate }: { items: CourseItem[]; onEdit: (item: CourseItem) => void; onDeactivate: (item: CourseItem) => void }) {
  return <section className="panel horizontal-management-table course-master-horizontal"><div className="horizontal-management-head"><span>CourseCode</span><span>Course Name</span><span>Student Year</span><span>Semester</span><span>Create At</span><span>Actions</span></div>{items.map(item => <article className="horizontal-management-row" key={item.id}>
    <ManagementDataCell label="CourseCode"><strong className="management-code-value">{workflowCode(item.values.courseCode, "course", "management")}</strong></ManagementDataCell>
    <ManagementDataCell label="Course Name" className="horizontal-primary"><strong>{item.values.name}</strong></ManagementDataCell>
    <ManagementDataCell label="Student Year" className="horizontal-detail"><strong>Year {item.values.yearLevel}</strong></ManagementDataCell>
    <ManagementDataCell label="Semester" className="horizontal-detail"><strong>{item.values.semester}</strong></ManagementDataCell>
    <ManagementDataCell label="Create At" className="horizontal-detail"><strong>{item.values.createAt}</strong></ManagementDataCell>
    <ManagementDataCell label="Actions" className="management-action-cell"><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></ManagementDataCell>
  </article>)}</section>;
}
