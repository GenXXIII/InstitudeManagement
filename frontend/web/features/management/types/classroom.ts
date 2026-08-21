export type ClassroomValues = Record<string, string> & {
  code: string;
  building: string;
  roomType: string;
  departmentId: string;
  department: string;
  capacity: string;
  status: string;
  studyStatus: string;
  deviceOnline: string;
};

export type ClassroomItem = { id: string; values: ClassroomValues };
