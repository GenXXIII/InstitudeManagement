import { ManagementDataCell } from "@/components/management-data-cell";
import type { EnrollmentDisplayItem, EnrollmentResource } from "../common/enrollment-types";
import {
  classroomEnrollmentStatusClass,
  enrollmentCells,
  enrollmentCopy,
} from "../enrollment-workspace-model";

export function EnrollmentRow({ resource, item, onEdit, onRemove }: {
  resource: EnrollmentResource;
  item: EnrollmentDisplayItem;
  onEdit?: () => void;
  onRemove?: () => void;
}) {
  const cells = enrollmentCells(resource, item);

  return <article className="horizontal-management-row">
    {cells.map((cell, index) => {
      const column = enrollmentCopy[resource].columns[index];
      const relationship = column === "Assigned course" || column === "Assigned courses" || column === "Year levels";
      const className = [index === 1 ? "horizontal-primary" : "horizontal-detail", relationship ? "enrollment-relationship-cell" : ""].filter(Boolean).join(" ");
      return <ManagementDataCell label={column} className={className} key={`${item.id}-${index}`}>
        {index === 0
          ? <strong className="management-code-value" title={cell}>{cell}</strong>
          : column === "Status" || column === "Period state"
            ? <span className={`table-status ${column === "Period state" ? cell.toLowerCase() : classroomEnrollmentStatusClass(cell)}`}>{cell}</span>
            : <strong className={relationship ? "enrollment-relationship-value" : undefined} title={cell || "Unassigned"}>{cell || "Unassigned"}</strong>}
      </ManagementDataCell>;
    })}
    {resource === "students" || resource === "timetable"
      ? <ManagementDataCell label="Actions" className="management-action-cell">{onEdit && onRemove ? <div className="management-actions"><button type="button" onClick={onEdit}>Edit</button><button type="button" className="danger" onClick={onRemove}>Remove</button></div> : <span className="table-status retained">Read only</span>}</ManagementDataCell>
      : null}
  </article>;
}
