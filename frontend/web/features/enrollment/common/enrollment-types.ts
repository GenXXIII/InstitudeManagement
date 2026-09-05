export type EnrollmentResource = "students" | "student-assignments" | "teachers" | "courses" | "classrooms" | "timetable" | "departments";

export type EnrollmentItem = {
  id: string;
  values: Record<string, string>;
};

export type EnrollmentDisplayItem = EnrollmentItem & {
  rowKey: string;
  assignedCourse?: string;
};
