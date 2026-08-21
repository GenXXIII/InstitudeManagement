export type TimetableValues = Record<string, string> & {
  courseId: string;
  course: string;
  teacherId: string;
  teacher: string;
  classroomId: string;
  classroom: string;
  classroomType: string;
  departmentId: string;
  department: string;
  yearLevel: string;
  dayOfWeek: string;
  startsAt: string;
  endsAt: string;
  status: string;
};

export type TimetableItem = { id: string; values: TimetableValues };

export type TimetablePeriod = {
  dayGroup: "Weekday" | "Weekend";
  session: "Morning" | "Afternoon" | "Evening";
  startsAt: string;
  endsAt: string;
};
