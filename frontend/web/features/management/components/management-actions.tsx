import type { ManagementItem } from "../management-types";

export function ManagementActions<TItem extends ManagementItem>({ item, onEdit, onDeactivate }: { item: TItem; onEdit: (item: TItem) => void; onDeactivate: (item: TItem) => void }) {
  return <div className="management-actions"><button onClick={() => onEdit(item)}>Edit</button><button className="danger" onClick={() => onDeactivate(item)}>Deactivate</button></div>;
}
