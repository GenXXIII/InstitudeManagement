import { Icon } from "@/components/icon";
import type { Operation } from "../operations-types";
import { OperationContent } from "./operation-content";

const titles: Record<string, string> = { dashboard: "All operations at a glance", students: "Current-period student attendance", teachers: "Current-period teacher attendance", classrooms: "Five-floor learning spaces", courses: "Courses running this period", timetable: "Complete weekly timetable", attendance: "Attendance register", departments: "Department coverage", grades: "Current gradebook" };

export function OperationPanel({ data, departmentId, year, className = "", kicker }: { data: Operation; departmentId: string; year: number; className?: string; kicker: string }) {
  return <section className={className}><article className={`panel data-panel operation-${data.module}`}><div className="panel-title"><div><span className="panel-kicker">{kicker}</span><h3>{titles[data.module] ?? "Operational activity"}</h3></div><span className="updated">Updated just now</span></div>{data.module === "dashboard" && <div className="operation-next-shift-visual"><span><Icon name="calendar" size={18}/></span><div><small>Upcoming timetable</small><strong>{nextShiftText(data.description)}</strong></div></div>}<OperationContent data={data} departmentId={departmentId} year={year}/></article></section>;
}

function nextShiftText(description: string) {
  const marker = "Next shift:";
  const markerIndex = description.indexOf(marker);
  return markerIndex >= 0 ? description.slice(markerIndex) : description;
}
