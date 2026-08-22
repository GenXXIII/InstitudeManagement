export type CourseValues = Record<string, string> & {
  courseCode: string;
  name: string;
  departmentId: string;
  department: string;
  teacherId: string;
  teacher: string;
  capacity: string;
  status: string;
  createAt: string;
};

export type CourseItem = { id: string; values: CourseValues };
