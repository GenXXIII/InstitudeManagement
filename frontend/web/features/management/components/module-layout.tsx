import type { CatalogItem, ManagementModule, References } from "../management-types";
import { AttendanceDesk } from "./attendance-desk";
import { CourseBoard } from "./course-board";
import { DepartmentDirectory } from "./department-directory";
import { Gradebook } from "./gradebook";
import { PeopleRoster } from "./people-roster";
import { RoomBoard } from "./room-board";
import { TimetableBoard } from "./timetable-board";

export function ModuleLayout({ module, items, references, onEdit, onDeactivate }: { module: Exclude<ManagementModule, "overview">; items: CatalogItem[]; references: References; onEdit: (item: CatalogItem) => void; onDeactivate: (item: CatalogItem) => void }) {
  if (module === "students" || module === "teachers") return <PeopleRoster type={module} items={items} onEdit={onEdit} onDeactivate={onDeactivate}/>;
  if (module === "classrooms") return <RoomBoard items={items} onEdit={onEdit} onDeactivate={onDeactivate}/>;
  if (module === "courses") return <CourseBoard items={items} onEdit={onEdit} onDeactivate={onDeactivate}/>;
  if (module === "timetable") return <TimetableBoard items={items} onEdit={onEdit} onDeactivate={onDeactivate}/>;
  if (module === "attendance") return <AttendanceDesk items={items} onEdit={onEdit} onDeactivate={onDeactivate}/>;
  if (module === "departments") return <DepartmentDirectory items={items} references={references} onEdit={onEdit} onDeactivate={onDeactivate}/>;
  return <Gradebook items={items} onEdit={onEdit} onDeactivate={onDeactivate}/>;
}
