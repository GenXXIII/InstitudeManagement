import type { CourseOperation } from "../operations-types";
import { statusClass } from "../operation-utils";

export function CourseOperationList({ rows }: { rows: CourseOperation[] }) {
  return <div className="live-course-list"><header className="live-course-head"><span>CourseCode</span><span>Course and department</span><span>Teacher</span><span>Capacity</span><span>Current status</span></header>{rows.map(row => <div key={row.id}><span className="course-code">{row.courseCode}</span><div><strong>{row.course}</strong><small>{row.department}</small></div><div className="course-operation-teacher"><strong>{row.teacher}</strong><small>Assigned teacher</small></div><b>{row.capacity} seats</b><span className={`table-status ${statusClass(row.status)}`}>{row.status}</span></div>)}</div>;
}
