import type { GradeOperation } from "../operations-types";

export function GradeOperationList({ rows }: { rows: GradeOperation[] }) {
  return <div className="live-grade-list">{rows.map(row => <div key={row.id}><span className={`grade-letter grade-${row.grade.charAt(0).toLowerCase()}`}>{row.grade}</span><div><strong>{row.student}</strong><small>{row.course} · {row.term}</small></div><b>{row.score}%</b></div>)}</div>;
}
