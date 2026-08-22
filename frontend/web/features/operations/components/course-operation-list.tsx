import type { CourseOperation } from "../operations-types";
import { statusClass } from "../operation-utils";

export function CourseOperationList({ rows }: { rows: CourseOperation[] }) {
  return <div className="live-course-list">{rows.map(row => <div key={row.id}><span className="course-code">{row.courseCode}</span><div><strong>{row.course}</strong><small>{row.department} - {row.teacher}</small></div><b>{row.capacity} seats</b><span className={`table-status ${statusClass(row.status)}`}>{row.status}</span></div>)}</div>;
}
