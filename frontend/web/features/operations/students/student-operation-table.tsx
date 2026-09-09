import { DataTable } from "@/components/data-table";
import type { StudentOperation } from "../operations-types";
import { initials, statusClass } from "../operation-utils";

export function StudentOperationTable({ rows }: { rows: StudentOperation[] }) {
  return <DataTable className="student-presence-board" headerClassName="student-presence-head" rowSelector=":scope > .student-presence-row" columns={["Operation code", "Photo", "Student and department", "Course", "Year and shift", "Real-time attendance"]}>{rows.map(row => <article className="student-presence-row" key={row.id}><div className="operation-row-code"><strong>{row.operationCode}</strong><small>{row.studentCode}</small></div><span className="initial-chip">{initials(row.student)}</span><div><strong>{row.student}</strong><small>{row.department}</small></div><strong>{row.course}</strong><b>Year {row.year} - {row.shift}</b><span className={`table-status ${statusClass(row.attendanceStatus)}`}>{row.attendanceStatus}</span></article>)}</DataTable>;
}
