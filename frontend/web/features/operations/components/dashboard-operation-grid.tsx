import Link from "next/link";
import { Icon } from "@/components/icon";
import type { OperationSummary } from "../operations-types";
import { statusClass } from "../operation-utils";

const icons = { Students: "users", Teachers: "teacher", Classrooms: "room", Courses: "book" } as const;

export function DashboardOperationGrid({ rows, departmentId }: { rows: OperationSummary[]; departmentId: string }) {
  return <div className="institute-operations-grid">{rows.map(row => <Link className={`institute-operation-card tone-${row.tone}`} href={`${row.route}${departmentId ? `?departmentId=${encodeURIComponent(departmentId)}` : ""}`} key={row.module}><div className="operation-card-head"><span><Icon name={icons[row.module as keyof typeof icons] ?? "dashboard"} size={17}/></span><b className={`table-status ${statusClass(row.status)}`}>{row.status}</b></div><small>{row.summary}</small><div className="operation-card-value"><strong>{row.value}</strong><span>{row.detail}</span></div><div className="operation-card-link">View full {row.module.toLowerCase()} operation <span>→</span></div></Link>)}</div>;
}
