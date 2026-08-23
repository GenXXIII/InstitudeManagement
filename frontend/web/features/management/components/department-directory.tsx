import Image from "next/image";
import type { References } from "../management-types";
import type { DepartmentItem } from "../types/department";
import { initials } from "../management-utils";
import { ManagementActions } from "./management-actions";
import { ManagementDataCell } from "./management-data-cell";

export function DepartmentDirectory({ items, references, onEdit, onDeactivate }: { items: DepartmentItem[]; references: References; onEdit: (item: DepartmentItem) => void; onDeactivate: (item: DepartmentItem) => void }) {
  return <section className="panel horizontal-management-table department-horizontal"><div className="horizontal-management-head"><span>DepartmentCode</span><span>Department Name</span><span>Head Name</span><span>Students</span><span>Teachers</span><span>Courses</span><span>Create At</span><span>Actions</span></div>{items.map(item => {
    const head = references.teachers.find(teacher => teacher.id === item.values.headTeacherId);
    return <article className="horizontal-management-row" key={item.id}>
      <ManagementDataCell label="DepartmentID"><strong className="management-code-value">{item.values.departmentCode}</strong></ManagementDataCell>
      <ManagementDataCell label="Department Name" className="horizontal-primary"><strong>{item.values.name}</strong></ManagementDataCell>
      <ManagementDataCell label="Head Name"><div className="department-head-horizontal">{head?.values.photoDataUrl ? <Image unoptimized width={32} height={48} src={head.values.photoDataUrl} alt={`${item.values.head} profile`}/> : <span>{initials(item.values.head)}</span>}<div><strong>{item.values.head}</strong><small>{head?.values.email}</small></div></div></ManagementDataCell>
      <ManagementDataCell label="Students" className="relationship-number"><strong>{references.students.filter(student => student.values.departmentId === item.id).length}</strong></ManagementDataCell>
      <ManagementDataCell label="Teachers" className="relationship-number"><strong>{references.teachers.filter(teacher => teacher.values.departmentId === item.id).length}</strong></ManagementDataCell>
      <ManagementDataCell label="Courses" className="relationship-number"><strong>{references.courses.filter(course => course.values.departmentId === item.id).length}</strong></ManagementDataCell>
      <ManagementDataCell label="Create At" className="horizontal-detail"><strong>{item.values.createAt}</strong></ManagementDataCell>
      <ManagementDataCell label="Actions" className="management-action-cell"><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></ManagementDataCell>
    </article>;
  })}</section>;
}
