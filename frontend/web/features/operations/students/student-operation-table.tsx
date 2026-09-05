import type { StudentOperation } from "../operations-types";
import { initials, statusClass } from "../operation-utils";
import { workflowCode } from "@/lib/workflow-code";

export function StudentOperationTable({ rows }: { rows: StudentOperation[] }) {
  return <div className="student-presence-board"><div className="student-presence-head"><span>Enrollment code</span><span>Photo</span><span>Student and department</span><span>Year and shift</span><span>Real-time attendance</span></div>{rows.map(row => <article className="student-presence-row" key={row.id}><div className="operation-row-code"><strong>{workflowCode(row.studentCode, "student", "enrollment")}</strong><small>Enrollment source</small></div><span className="initial-chip">{initials(row.student)}</span><div><strong>{row.student}</strong><small>{row.department}</small></div><b>Year {row.year} - {row.shift}</b><span className={`table-status ${statusClass(row.attendanceStatus)}`}>{row.attendanceStatus}</span></article>)}</div>;
}
