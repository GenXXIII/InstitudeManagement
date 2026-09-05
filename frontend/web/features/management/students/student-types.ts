export type StudentValues = Record<string, string> & {
  photoDataUrl: string;
  studentCode: string;
  name: string;
  email: string;
  departmentId: string;
  department: string;
  year: string;
  shift: string;
  status: string;
  createAt: string;
};

export type StudentItem = { id: string; values: StudentValues };
