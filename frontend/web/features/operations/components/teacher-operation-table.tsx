import type { TeacherOperation } from "../operations-types";
import { initials, statusClass } from "../operation-utils";
import { workflowCode } from "@/lib/workflow-code";

export function TeacherOperationTable({ rows }: { rows: TeacherOperation[] }) {
  return <div className="teacher-operation-board"><div className="teacher-operation-head"><span>Photo</span><span>Teacher and department</span><span>Operation code</span><span>Real-time attendance</span></div>{rows.map(row => <article className="teacher-operation-row" key={row.id}><span className="initial-chip">{initials(row.teacher)}</span><div><strong>{row.teacher}</strong><small>{row.department} · Enrollment {workflowCode(row.teacherCode, "teacher", "enrollment")}</small></div><b>{workflowCode(row.teacherCode, "teacher", "operation")}</b><span className={`table-status ${statusClass(row.status)}`}>{row.status}</span></article>)}</div>;
}
