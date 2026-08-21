export type TeacherValues = Record<string, string> & {
  photoDataUrl: string;
  number: string;
  name: string;
  email: string;
  departmentId: string;
  department: string;
  status: string;
};

export type TeacherItem = { id: string; values: TeacherValues };
