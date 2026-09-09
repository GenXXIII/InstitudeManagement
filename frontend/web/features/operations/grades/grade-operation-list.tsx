import { DataTable } from "@/components/data-table";
import type { GradeOperation } from "../operations-types";

export function GradeOperationList({ rows }: { rows: GradeOperation[] }) {
  return <DataTable className="live-grade-list" headerClassName="operation-compact-head live-grade-head" rowSelector=":scope > .live-grade-row" columns={["Grade", "Student and course", "Score"]}>{rows.map(row => <div className="live-grade-row" key={row.id}><span className={`grade-letter grade-${row.grade.charAt(0).toLowerCase()}`}>{row.grade}</span><div><strong>{row.student}</strong><small>{row.course} · {row.term}</small></div><b>{row.score}%</b></div>)}</DataTable>;
}
