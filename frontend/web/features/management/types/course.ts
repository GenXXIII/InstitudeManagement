export type CourseValues = Record<string, string> & {
  code: string;
  name: string;
  departmentId: string;
  department: string;
  teacherId: string;
  teacher: string;
  capacity: string;
  status: string;
};

export type CourseItem = { id: string; values: CourseValues };
