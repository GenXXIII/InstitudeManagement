import Image from "next/image";
import { workflowCode } from "@/lib/workflow-code";
import type { StudentItem } from "../types/student";
import { ManagementActions } from "./management-actions";
import { ManagementDataCell } from "./management-data-cell";

export function StudentRoster({ items, onEdit, onDeactivate }: { items: StudentItem[]; onEdit: (item: StudentItem) => void; onDeactivate: (item: StudentItem) => void }) {
  return <section className="panel horizontal-management-table people-horizontal student-profile-horizontal">
    <div className="horizontal-management-head"><span>StudentCode</span><span>Photo</span><span>Student Name</span><span>Email</span><span>Create At</span><span>Actions</span></div>
    {items.map(item => <article className="horizontal-management-row" key={item.id}>
      <ManagementDataCell label="StudentID"><strong className="management-code-value">{workflowCode(item.values.studentCode, "student", "management")}</strong></ManagementDataCell>
      <ManagementDataCell label="Photo" className="horizontal-portrait"><Image unoptimized width={48} height={72} src={item.values.photoDataUrl} alt={`${item.values.name} profile`}/></ManagementDataCell>
      <ManagementDataCell label="Student Name" className="horizontal-primary"><strong>{item.values.name}</strong></ManagementDataCell>
      <ManagementDataCell label="Email" className="horizontal-detail"><strong>{item.values.email}</strong></ManagementDataCell>
      <ManagementDataCell label="Create At" className="horizontal-detail"><strong>{item.values.createAt}</strong></ManagementDataCell>
      <ManagementDataCell label="Actions" className="management-action-cell"><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></ManagementDataCell>
    </article>)}
  </section>;
}
