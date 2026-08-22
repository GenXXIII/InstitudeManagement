import Image from "next/image";
import type { StudentItem } from "../types/student";
import { ManagementActions } from "./management-actions";

export function StudentRoster({ items, onEdit, onDeactivate }: { items: StudentItem[]; onEdit: (item: StudentItem) => void; onDeactivate: (item: StudentItem) => void }) {
  return <section className="panel horizontal-management-table people-horizontal students-horizontal">
    <div className="horizontal-management-head"><span>StudentCode</span><span>Photo</span><span>Student Name</span><span>Email</span><span>Department</span><span>Academic level</span><span>Create At</span><span>Status</span><span>Actions</span></div>
    {items.map(item => <article className="horizontal-management-row" key={item.id}>
      <strong className="management-code-value">{item.values.studentCode}</strong>
      <div className="horizontal-portrait"><Image unoptimized width={48} height={72} src={item.values.photoDataUrl} alt={`${item.values.name} profile`}/></div>
      <div className="horizontal-primary"><strong>{item.values.name}</strong></div>
      <div className="horizontal-detail"><strong>{item.values.email}</strong></div>
      <div className="horizontal-detail"><strong>{item.values.department}</strong></div>
      <div className="horizontal-detail"><strong>Year {item.values.year}</strong></div>
      <div className="horizontal-detail"><strong>{item.values.createAt}</strong></div>
      <span className={`table-status ${item.values.status.toLowerCase().replace(" ", "-")}`}>{item.values.status}</span>
      <ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/>
    </article>)}
  </section>;
}
