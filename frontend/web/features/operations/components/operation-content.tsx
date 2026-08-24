import type { Operation } from "../operations-types";
import { AttendanceOperationList } from "./attendance-operation-list";
import { ClassroomBuilding } from "./classroom-building";
import { CourseOperationList } from "./course-operation-list";
import { DashboardOperationGrid } from "./dashboard-operation-grid";
import { DepartmentOperationList } from "./department-operation-list";
import { GradeOperationList } from "./grade-operation-list";
import { StudentOperationTable } from "./student-operation-table";
import { TeacherOperationTable } from "./teacher-operation-table";
import { WeeklyTimetable } from "./weekly-timetable";

export function OperationContent({ data, departmentId, year }: { data: Operation; departmentId: string; year: number }) {
  if (data.module === "dashboard") return <DashboardOperationGrid data={data} departmentId={departmentId} year={year}/>;
  if (data.module === "students") return <PaginatedOperationList rows={data.students ?? []} resetKey={`${data.module}-${departmentId}-${year}`}>{rows => <StudentOperationTable rows={rows}/>}</PaginatedOperationList>;
  if (data.module === "teachers") return <PaginatedOperationList rows={data.teachers ?? []} resetKey={`${data.module}-${departmentId}-${year}`}>{rows => <TeacherOperationTable rows={rows}/>}</PaginatedOperationList>;
  if (data.module === "classrooms") return <ClassroomBuilding rows={data.classrooms ?? []}/>;
  if (data.module === "timetable") return <WeeklyTimetable rows={data.weeklySchedule ?? []} periods={data.timetablePeriods ?? []} rooms={data.timetableRooms ?? []} globalYear={year}/>;
  if (data.module === "courses") return <PaginatedOperationList rows={data.courses ?? []} resetKey={`${data.module}-${departmentId}-${year}`}>{rows => <CourseOperationList rows={rows}/>}</PaginatedOperationList>;
  if (data.module === "attendance") return <PaginatedOperationList rows={data.attendance ?? []} resetKey={`${data.module}-${departmentId}-${year}`}>{rows => <AttendanceOperationList rows={rows}/>}</PaginatedOperationList>;
  if (data.module === "departments") return <PaginatedOperationList rows={data.departments ?? []} resetKey={`${data.module}-${departmentId}-${year}`}>{rows => <DepartmentOperationList rows={rows}/>}</PaginatedOperationList>;
  return <PaginatedOperationList rows={data.grades ?? []} resetKey={`${data.module}-${departmentId}-${year}`}>{rows => <GradeOperationList rows={rows}/>}</PaginatedOperationList>;
}

function PaginatedOperationList<T>({ rows, resetKey, children }: { rows: T[]; resetKey: string; children: (rows: T[]) => React.ReactNode }) {
  const pagination = useDataPagination(rows, resetKey);
  return <div className="operation-paginated-list"><div className="operation-page-rows">{children(pagination.pageItems)}</div><DataPagination page={pagination.page} pageCount={pagination.pageCount} total={rows.length} onPage={pagination.setPage}/></div>;
}
import { DataPagination, useDataPagination } from "@/components/data-pagination";
