import { DataTable } from "@/components/data-table";
import type { DepartmentOperation } from "../operations-types";
import { statusClass } from "../operation-utils";

export function DepartmentOperationList({ rows }: { rows: DepartmentOperation[] }) {
  return <DataTable className="live-department-list" headerClassName="operation-compact-head live-department-head" rowSelector=":scope > .live-department-row" columns={["Department and head", "Enrollment totals", "Status"]}>{rows.map(row => <div className="live-department-row" key={row.id}><div><strong>{row.department}</strong><span>{row.head}</span></div><div className="department-mini-counts"><span><b>{row.students}</b> students</span><span><b>{row.teachers}</b> teachers</span><span><b>{row.courses}</b> courses</span></div><span className={`table-status ${statusClass(row.status)}`}>{row.status}</span></div>)}</DataTable>;
}
