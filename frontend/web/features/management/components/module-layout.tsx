import type { ManagementItem, ManagementModule, References } from "../management-types";
import type { ClassroomItem } from "@/features/management/classrooms/classroom-types";
import type { CourseItem } from "@/features/management/courses/course-types";
import type { DepartmentItem } from "@/features/management/departments/department-types";
import type { StudentItem } from "@/features/management/students/student-types";
import type { TeacherItem } from "@/features/management/teachers/teacher-types";
import { TimetableBoard } from "@/features/timetable/timetable-board";
import type { TimetableItem } from "@/features/timetable/timetable-types";
import { RoomBoard } from "../classrooms/room-board";
import { CourseBoard } from "../courses/course-board";
import { DepartmentDirectory } from "../departments/department-directory";
import { StudentRoster } from "../students/student-roster";
import { TeacherRoster } from "../teachers/teacher-roster";

export function ModuleLayout({ module, items, references, onEdit, onDeactivate }: { module: Exclude<ManagementModule, "overview">; items: ManagementItem[]; references: References; onEdit: (item: ManagementItem) => void; onDeactivate: (item: ManagementItem) => void }) {
  if (module === "students") return <StudentRoster items={items as StudentItem[]} onEdit={onEdit} onDeactivate={onDeactivate}/>;
  if (module === "teachers") return <TeacherRoster items={items as TeacherItem[]} onEdit={onEdit} onDeactivate={onDeactivate}/>;
  if (module === "classrooms") return <RoomBoard items={items as ClassroomItem[]} onEdit={onEdit} onDeactivate={onDeactivate}/>;
  if (module === "courses") return <CourseBoard items={items as CourseItem[]} timetable={references.timetable} onEdit={onEdit} onDeactivate={onDeactivate}/>;
  if (module === "timetable") return <TimetableBoard items={items as TimetableItem[]} onEdit={onEdit} onDeactivate={onDeactivate}/>;
  return <DepartmentDirectory items={items as DepartmentItem[]} references={references} onEdit={onEdit} onDeactivate={onDeactivate}/>;
}
