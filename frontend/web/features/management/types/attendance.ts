export type AttendanceValues = Record<string, string> & {
  studentId: string;
  student: string;
  number: string;
  departmentId: string;
  department: string;
  date: string;
  checkedInAt: string;
  status: string;
  method: string;
  academicYear: string;
  term: string;
};

export type AttendanceItem = { id: string; values: AttendanceValues };
