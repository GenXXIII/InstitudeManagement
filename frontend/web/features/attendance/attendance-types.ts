export type AttendanceValues = Record<string, string> & {
  attendanceCode: string;
  studentId: string;
  student: string;
  studentCode: string;
  departmentId: string;
  department: string;
  date: string;
  checkedInAt: string;
  status: string;
  method: string;
  academicYear: string;
  term: string;
  createAt: string;
};

export type AttendanceItem = { id: string; values: AttendanceValues };
