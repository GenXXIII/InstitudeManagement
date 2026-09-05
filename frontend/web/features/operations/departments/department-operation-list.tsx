import type { DepartmentOperation } from "../operations-types";
import { statusClass } from "../operation-utils";

export function DepartmentOperationList({ rows }: { rows: DepartmentOperation[] }) {
  return <div className="live-department-list">{rows.map(row => <div key={row.id}><div><strong>{row.department}</strong><span>{row.head}</span></div><div className="department-mini-counts"><span><b>{row.students}</b> students</span><span><b>{row.teachers}</b> teachers</span><span><b>{row.courses}</b> courses</span></div><span className={`table-status ${statusClass(row.status)}`}>{row.status}</span></div>)}</div>;
}
