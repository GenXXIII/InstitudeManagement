export type StudentValues = Record<string, string> & {
  photoDataUrl: string;
  number: string;
  name: string;
  email: string;
  departmentId: string;
  department: string;
  year: string;
  status: string;
};

export type StudentItem = { id: string; values: StudentValues };
