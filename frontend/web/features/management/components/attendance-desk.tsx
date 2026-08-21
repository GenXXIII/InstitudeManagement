import type { LayoutProps } from "../management-types";
import { initials } from "../management-utils";
import { ManagementActions } from "./management-actions";

export function AttendanceDesk({ items, onEdit, onDeactivate }: LayoutProps) {
  return <section className="panel attendance-desk"><div className="attendance-desk-head"><span>Student</span><span>Department</span><span>Date & time</span><span>Method</span><span>Status</span><span>Actions</span></div>{items.map(item => <div className="attendance-desk-row" key={item.id}><div><span className="initial-chip">{initials(item.values.student)}</span><span><strong>{item.values.student}</strong><small>{item.values.number}</small></span></div><span>{item.values.department}</span><span>{item.values.date}<small>{item.values.checkedInAt || "No check-in"}</small></span><span>{item.values.method}</span><span className={`table-status ${item.values.status.toLowerCase()}`}>{item.values.status}</span><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></div>)}</section>;
}
