import { ManagementDataCell } from "@/components/management-data-cell";
import type { EnrollmentDisplayItem, EnrollmentItem, EnrollmentResource } from "../common/enrollment-types";
import {
  classroomEnrollmentStatusClass,
  enrollmentCells,
  enrollmentCopy,
} from "../enrollment-workspace-model";

export function EnrollmentRow({ resource, item, studentSchedules, onEdit, onRemove }: {
  resource: EnrollmentResource;
  item: EnrollmentDisplayItem;
  studentSchedules: EnrollmentItem[];
  onEdit?: () => void;
  onRemove?: () => void;
}) {
  const cells = enrollmentCells(resource, item, studentSchedules);

  return <article className="horizontal-management-row">
    {cells.map((cell, index) => {
      const relationship = (resource === "classrooms" && index === 3) || (resource === "teachers" && index === 3) || (resource === "student-assignments" && (index === 4 || index === 5));
      const className = [index === 1 ? "horizontal-primary" : "horizontal-detail", relationship ? "enrollment-relationship-cell" : ""].filter(Boolean).join(" ");
      return <ManagementDataCell label={enrollmentCopy[resource].columns[index]} className={className} key={`${item.id}-${index}`}>
        {index === 0
          ? <strong className="management-code-value">{cell}</strong>
          : (resource === "classrooms" && index === 5) || (resource === "timetable" && index === 7)
            ? <span className={`table-status ${classroomEnrollmentStatusClass(cell)}`}>{cell}</span>
            : <strong className={relationship ? "enrollment-relationship-value" : undefined} title={relationship ? cell : undefined}>{cell || "Unassigned"}</strong>}
      </ManagementDataCell>;
    })}
    {onEdit && onRemove
      ? <ManagementDataCell label="Actions" className="management-action-cell"><div className="management-actions"><button type="button" onClick={onEdit}>Edit</button><button type="button" className="danger" onClick={onRemove}>Remove</button></div></ManagementDataCell>
      : null}
  </article>;
}
