import type { Operation } from "../operations-types";
import { OperationContent } from "./operation-content";

const titles: Record<string, string> = { dashboard: "All operations at a glance", students: "Live student roster", teachers: "Faculty availability", classrooms: "Five-floor learning spaces", courses: "Active course delivery", timetable: "Complete weekly timetable", attendance: "Attendance register", departments: "Department coverage", grades: "Current gradebook" };

export function OperationPanel({ data, departmentId, year, className = "", kicker }: { data: Operation; departmentId: string; year: number; className?: string; kicker: string }) {
  return <section className={className}><article className={`panel data-panel operation-${data.module}`}><div className="panel-title"><div><span className="panel-kicker">{kicker}</span><h3>{titles[data.module] ?? "Operational activity"}</h3></div><span className="updated">Updated just now</span></div><OperationContent data={data} departmentId={departmentId} year={year}/></article></section>;
}
