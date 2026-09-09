import { DataTable } from "@/components/data-table";
import type { AttendanceOperation } from "../operations-types";
import { initials, statusClass } from "../operation-utils";

export function AttendanceOperationList({ rows }: { rows: AttendanceOperation[] }) {
  return <DataTable className="live-scan-list" headerClassName="operation-compact-head live-scan-head" rowSelector=":scope > .live-scan-row" columns={["Time", "Photo", "Student and method", "Status"]}>{rows.map(row => <div className="live-scan-row" key={row.id}><span className="scan-time">{row.time}</span><span className="initial-chip">{initials(row.student)}</span><div><strong>{row.student}</strong><small>{row.studentCode} - {row.method}</small></div><span className={`table-status ${statusClass(row.status)}`}>{row.status}</span></div>)}</DataTable>;
}
