import Image from "next/image";
import { workflowCode } from "@/lib/workflow-code";
import type { TeacherItem } from "../types/teacher";
import { ManagementActions } from "./management-actions";
import { ManagementDataCell } from "./management-data-cell";

export function TeacherRoster({ items, onEdit, onDeactivate }: { items: TeacherItem[]; onEdit: (item: TeacherItem) => void; onDeactivate: (item: TeacherItem) => void }) {
  return <section className="panel horizontal-management-table people-horizontal teacher-profile-horizontal"><div className="horizontal-management-head"><span>TeacherCode</span><span>Photo</span><span>Teacher Name</span><span>Email</span><span>Create At</span><span>Actions</span></div>{items.map(item => <article className="horizontal-management-row" key={item.id}>
      <ManagementDataCell label="TeacherID"><strong className="management-code-value">{workflowCode(item.values.teacherCode, "teacher", "management")}</strong></ManagementDataCell>
      <ManagementDataCell label="Photo" className="horizontal-portrait"><Image unoptimized width={48} height={72} src={item.values.photoDataUrl} alt={`${item.values.name} profile`}/></ManagementDataCell>
      <ManagementDataCell label="Teacher Name" className="horizontal-primary"><strong>{item.values.name}</strong></ManagementDataCell>
      <ManagementDataCell label="Email" className="horizontal-detail"><strong>{item.values.email}</strong></ManagementDataCell>
      <ManagementDataCell label="Create At" className="horizontal-detail"><strong>{item.values.createAt}</strong></ManagementDataCell>
      <ManagementDataCell label="Actions" className="management-action-cell"><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></ManagementDataCell>
    </article>)}</section>;
}
