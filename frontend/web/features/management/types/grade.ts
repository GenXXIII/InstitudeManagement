export type GradeValues = Record<string, string> & {
  studentId: string;
  student: string;
  courseId: string;
  course: string;
  departmentId: string;
  department: string;
  score: string;
  grade: string;
  academicYear: string;
  term: string;
  status: string;
};

export type GradeItem = { id: string; values: GradeValues };
