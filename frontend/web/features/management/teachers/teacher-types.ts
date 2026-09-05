export type TeacherValues = Record<string, string> & {
  photoDataUrl: string;
  teacherCode: string;
  name: string;
  email: string;
  departmentId?: string;
  department?: string;
  status: string;
  createAt: string;
};

export type TeacherItem = { id: string; values: TeacherValues };
