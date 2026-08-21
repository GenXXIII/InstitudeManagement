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

export function OperationContent({ data, departmentId }: { data: Operation; departmentId: string }) {
  if (data.module === "dashboard") return <DashboardOperationGrid rows={data.summary ?? []} departmentId={departmentId}/>;
  if (data.module === "students") return <StudentOperationTable rows={data.students ?? []}/>;
  if (data.module === "teachers") return <TeacherOperationTable rows={data.teachers ?? []}/>;
  if (data.module === "classrooms") return <ClassroomBuilding rows={data.classrooms ?? []}/>;
  if (data.module === "timetable") return <WeeklyTimetable rows={data.weeklySchedule ?? []} periods={data.timetablePeriods ?? []} rooms={data.timetableRooms ?? []}/>;
  if (data.module === "courses") return <CourseOperationList rows={data.courses ?? []}/>;
  if (data.module === "attendance") return <AttendanceOperationList rows={data.attendance ?? []}/>;
  if (data.module === "departments") return <DepartmentOperationList rows={data.departments ?? []}/>;
  return <GradeOperationList rows={data.grades ?? []}/>;
}
