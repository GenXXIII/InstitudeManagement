import type { StudentOperation } from "../operations-types";
import { initials, statusClass } from "../operation-utils";

export function StudentOperationTable({ rows }: { rows: StudentOperation[] }) {
  return <div className="student-presence-board"><div className="student-presence-head"><span>Photo</span><span>Student and department</span><span>Year level</span><span>Today&apos;s attendance</span></div>{rows.map(row => <article className="student-presence-row" key={row.id}><span className="initial-chip">{initials(row.student)}</span><div><strong>{row.student}</strong><small>{row.studentCode} - {row.department}</small></div><b>Year {row.year}</b><span className={`table-status ${statusClass(row.attendanceStatus)}`}>{row.attendanceStatus}</span></article>)}</div>;
}
