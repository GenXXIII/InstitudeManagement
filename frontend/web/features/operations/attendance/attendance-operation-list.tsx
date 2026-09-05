import type { AttendanceOperation } from "../operations-types";
import { initials, statusClass } from "../operation-utils";

export function AttendanceOperationList({ rows }: { rows: AttendanceOperation[] }) {
  return <div className="live-scan-list">{rows.map(row => <div key={row.id}><span className="scan-time">{row.time}</span><span className="initial-chip">{initials(row.student)}</span><div><strong>{row.student}</strong><small>{row.studentCode} - {row.method}</small></div><span className={`table-status ${statusClass(row.status)}`}>{row.status}</span></div>)}</div>;
}
