import { DataTable } from "@/components/data-table";
import type { CourseOperation } from "../operations-types";
import { statusClass } from "../operation-utils";

export function CourseOperationList({ rows }: { rows: CourseOperation[] }) {
  return <DataTable className="live-course-list" headerClassName="live-course-head" rowSelector=":scope > .live-course-row" columns={["Operation code", "Course and department", "Teacher", { key: "classroom", label: "Classroom", align: "center" }, "Capacity", "Current status"]}>{rows.map(row => <div className="live-course-row" key={row.id}><div className="workflow-operation-code operation-code-text"><strong>{row.operationCode}</strong><small>{row.courseCode}</small></div><div><strong>{row.course}</strong><small>{row.department}</small></div><div className="course-operation-teacher"><strong>{row.teacher}</strong><small>{row.teacherAttendance === "Present" ? "Teacher present" : `Teacher ${row.teacherAttendance.toLowerCase()}`}</small></div><strong>{row.classroom}</strong><b>{row.capacity} seats</b><div className="course-operation-state"><span className={`table-status ${statusClass(row.status)}`}>{row.status}</span><small>{row.statusDetail}</small></div></div>)}</DataTable>;
}
