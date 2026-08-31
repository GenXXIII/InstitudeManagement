import type { TeacherOperation } from "../operations-types";
import { initials, statusClass } from "../operation-utils";
import { workflowCode } from "@/lib/workflow-code";

export function TeacherOperationTable({ rows }: { rows: TeacherOperation[] }) {
  return <div className="teacher-operation-board"><div className="teacher-operation-head"><span>Enrollment code</span><span>Photo</span><span>Teacher and department</span><span>Real-time attendance</span></div>{rows.map(row => <article className="teacher-operation-row" key={row.id}><div className="operation-row-code"><strong>{workflowCode(row.teacherCode, "teacher", "enrollment")}</strong><small>Enrollment source</small></div><span className="initial-chip">{initials(row.teacher)}</span><div><strong>{row.teacher}</strong><small>{row.department}</small></div><span className={`table-status ${statusClass(row.status)}`}>{row.status}</span></article>)}</div>;
}
