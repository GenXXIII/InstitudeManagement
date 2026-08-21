import type { LayoutProps } from "../management-types";
import { ManagementActions } from "./management-actions";

export function Gradebook({ items, onEdit, onDeactivate }: LayoutProps) {
  return <section className="panel gradebook"><div className="gradebook-head"><span>Student</span><span>Course</span><span>Department</span><span>Term</span><span>Score</span><span>Grade</span><span>Actions</span></div>{items.map(item => <div className="gradebook-row" key={item.id}><strong>{item.values.student}</strong><span>{item.values.course}</span><span>{item.values.department}</span><span>{item.values.term}</span><b>{item.values.score}%</b><span className={`grade-letter grade-${item.values.grade.toLowerCase()}`}>{item.values.grade}</span><ManagementActions item={item} onEdit={onEdit} onDeactivate={onDeactivate}/></div>)}</section>;
}
