import { PaginatedDataRegion } from "@/components/data-table";
import type { Operation } from "../operations-types";
import { AttendanceOperationList } from "../attendance/attendance-operation-list";
import { ClassroomBuilding } from "../classrooms/classroom-building";
import { CourseOperationList } from "../courses/course-operation-list";
import { DashboardOperationGrid } from "../dashboard/dashboard-operation-grid";
import { DepartmentOperationList } from "../departments/department-operation-list";
import { GradeOperationList } from "../grades/grade-operation-list";
import { StudentOperationTable } from "../students/student-operation-table";
import { TeacherOperationTable } from "../teachers/teacher-operation-table";
import { WeeklyTimetable } from "../timetable/weekly-timetable";
import { compareAcademicRows } from "@/lib/academic-order";

export function OperationContent({ data, departmentId, year }: { data: Operation; departmentId: string; year: number }) {
  if (data.module === "dashboard") return <DashboardOperationGrid data={data} departmentId={departmentId} year={year}/>;
  if (data.module === "students") return <PaginatedOperationList rows={academicSort(data.students ?? [])} resetKey={`${data.module}-${departmentId}-${year}`}>{rows => <StudentOperationTable rows={rows}/>}</PaginatedOperationList>;
  if (data.module === "teachers") return <PaginatedOperationList rows={academicSort(data.teachers ?? [])} resetKey={`${data.module}-${departmentId}-${year}`}>{rows => <TeacherOperationTable rows={rows}/>}</PaginatedOperationList>;
  if (data.module === "classrooms") return <ClassroomBuilding rows={data.classrooms ?? []}/>;
  if (data.module === "timetable") return <WeeklyTimetable rows={data.weeklySchedule ?? []} periods={data.timetablePeriods ?? []} rooms={data.timetableRooms ?? []} globalYear={year}/>;
  if (data.module === "courses") return <PaginatedOperationList rows={academicSort(data.courses ?? [])} resetKey={`${data.module}-${departmentId}-${year}`}>{rows => <CourseOperationList rows={rows}/>}</PaginatedOperationList>;
  if (data.module === "attendance") return <PaginatedOperationList rows={academicSort(data.attendance ?? [])} resetKey={`${data.module}-${departmentId}-${year}`}>{rows => <AttendanceOperationList rows={rows}/>}</PaginatedOperationList>;
  if (data.module === "departments") return <PaginatedOperationList rows={academicSort(data.departments ?? [])} resetKey={`${data.module}-${departmentId}-${year}`}>{rows => <DepartmentOperationList rows={rows}/>}</PaginatedOperationList>;
  return <PaginatedOperationList rows={academicSort(data.grades ?? [])} resetKey={`${data.module}-${departmentId}-${year}`}>{rows => <GradeOperationList rows={rows}/>}</PaginatedOperationList>;
}

function academicSort<T>(rows: T[]) { return rows.toSorted((left, right) => compareAcademicRows(left as Record<string, unknown>, right as Record<string, unknown>)); }

function PaginatedOperationList<T>({ rows, resetKey, children }: { rows: T[]; resetKey: string; children: (rows: T[]) => React.ReactNode }) {
  return <PaginatedDataRegion items={rows} resetKey={resetKey} className="operation-paginated-list">{pageItems => <div className="operation-page-rows">{children(pageItems)}</div>}</PaginatedDataRegion>;
}
