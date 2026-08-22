import type { ManagementItem, ManagementModule, References } from "../management-types";
import type { AttendanceItem } from "../types/attendance";
import type { ClassroomItem } from "../types/classroom";
import type { CourseItem } from "../types/course";
import type { DepartmentItem } from "../types/department";
import type { GradeItem } from "../types/grade";
import type { StudentItem } from "../types/student";
import type { TeacherItem } from "../types/teacher";
import type { TimetableItem } from "../types/timetable";
import { AttendanceDesk } from "./attendance-desk";
import { CourseBoard } from "./course-board";
import { DepartmentDirectory } from "./department-directory";
import { Gradebook } from "./gradebook";
import { RoomBoard } from "./room-board";
import { StudentRoster } from "./student-roster";
import { TeacherRoster } from "./teacher-roster";
import { TimetableBoard } from "./timetable-board";

export function ModuleLayout({ module, items, references, onEdit, onDeactivate }: { module: Exclude<ManagementModule, "overview">; items: ManagementItem[]; references: References; onEdit: (item: ManagementItem) => void; onDeactivate: (item: ManagementItem) => void }) {
  if (module === "students") return <StudentRoster items={items as StudentItem[]} onEdit={onEdit} onDeactivate={onDeactivate}/>;
  if (module === "teachers") return <TeacherRoster items={items as TeacherItem[]} references={references} onEdit={onEdit} onDeactivate={onDeactivate}/>;
  if (module === "classrooms") return <RoomBoard items={items as ClassroomItem[]} onEdit={onEdit} onDeactivate={onDeactivate}/>;
  if (module === "courses") return <CourseBoard items={items as CourseItem[]} onEdit={onEdit} onDeactivate={onDeactivate}/>;
  if (module === "timetable") return <TimetableBoard items={items as TimetableItem[]} references={references} onEdit={onEdit} onDeactivate={onDeactivate}/>;
  if (module === "attendance") return <AttendanceDesk items={items as AttendanceItem[]} onEdit={onEdit} onDeactivate={onDeactivate}/>;
  if (module === "departments") return <DepartmentDirectory items={items as DepartmentItem[]} references={references} onEdit={onEdit} onDeactivate={onDeactivate}/>;
  return <Gradebook items={items as GradeItem[]} courses={references.courses} students={references.students} onEdit={onEdit} onDeactivate={onDeactivate}/>;
}
