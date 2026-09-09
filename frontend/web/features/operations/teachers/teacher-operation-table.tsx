import { DataTable } from "@/components/data-table";
import type { TeacherOperation } from "../operations-types";
import { initials, statusClass } from "../operation-utils";

export function TeacherOperationTable({ rows }: { rows: TeacherOperation[] }) {
  return <DataTable className="teacher-operation-board" headerClassName="teacher-operation-head" rowSelector=":scope > .teacher-operation-row" columns={["Operation code", "Photo", "Teacher and department", "Course", "Real-time attendance"]}>{rows.map(row => <article className="teacher-operation-row" key={row.id}><div className="operation-row-code"><strong>{row.operationCode}</strong><small>{row.teacherCode}</small></div><span className="initial-chip">{initials(row.teacher)}</span><div><strong>{row.teacher}</strong><small>{row.department}</small></div><strong>{row.course}</strong><span className={`table-status ${statusClass(row.status)}`}>{row.status}</span></article>)}</DataTable>;
}
