export type GradeValues = Record<string, string> & {
  gradeCode: string;
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
  createAt: string;
};

export type GradeItem = { id: string; values: GradeValues };
