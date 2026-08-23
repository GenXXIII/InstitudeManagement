import type { ClassroomItem } from "../types/classroom";
import { ManagementActions } from "./management-actions";
import { ManagementDataCell } from "./management-data-cell";

export function RoomBoard({ items, onEdit, onDeactivate }: { items: ClassroomItem[]; onEdit: (item: ClassroomItem) => void; onDeactivate: (item: ClassroomItem) => void }) {
  return <section className="panel horizontal-management-table room-horizontal"><div className="horizontal-management-head"><span>ClassroomCode</span><span>Building</span><span>Access</span><span>Capacity</span><span>Create At</span><span>Actions</span></div>{items.map(item => <article className="horizontal-management-row" key={item.id}>
    <ManagementDataCell label="ClassroomID"><div className="room-code-value"><strong className="management-code-value">{item.values.classroomCode}</strong><small>{item.values.roomType}</small></div></ManagementDataCell>
    <ManagementDataCell label="Building" className="horizontal-detail"><strong>{item.values.building}</strong></ManagementDataCell>
    <ManagementDataCell label="Access" className="horizontal-detail"><strong>{item.values.department}</strong></ManagementDataCell>
    <ManagementDataCell label="Capacity" className="horizontal-detail"><strong>{item.values.capacity} seats</strong></ManagementDataCell>
    <ManagementDataCell label="Create At" className="horizontal-detail"><strong>{item.values.createAt}</strong></ManagementDataCell>
    <ManagementDataCell label="Actions" className="management-action-cell"><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></ManagementDataCell>
  </article>)}</section>;
}
