import type { CatalogItem } from "../management-types";

export function ManagementActions({ item, onEdit, onDeactivate }: { item: CatalogItem; onEdit: (item: CatalogItem) => void; onDeactivate: (item: CatalogItem) => void }) {
  return <div className="management-actions"><button onClick={() => onEdit(item)}>Edit</button><button className="danger" onClick={() => onDeactivate(item)}>Deactivate</button></div>;
}
