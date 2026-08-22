import type { ManagementModule, References } from "./management-types";

export function managementCode(module: ManagementModule, values: Record<string, string>) {
  const key = module === "students" ? "studentCode"
    : module === "teachers" ? "teacherCode"
      : module === "departments" || module === "overview" ? "departmentCode"
        : module === "courses" ? "courseCode"
          : module === "classrooms" ? "classroomCode"
            : module === "timetable" ? "timetableCode"
              : module === "attendance" ? "attendanceCode"
                : "gradeCode";
  return values[key] ?? "";
}

export function relationshipCode(source: keyof References, values: Record<string, string>) {
  if (source === "students") return values.studentCode;
  if (source === "teachers") return values.teacherCode;
  if (source === "departments") return values.departmentCode;
  if (source === "courses") return values.courseCode;
  if (source === "classrooms") return values.classroomCode;
  if (source === "timetable") return values.timetableCode;
  return values.attendanceCode;
}
