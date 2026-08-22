export type ClassroomValues = Record<string, string> & {
  classroomCode: string;
  building: string;
  roomType: string;
  departmentId: string;
  department: string;
  capacity: string;
  status: string;
  studyStatus: string;
  deviceOnline: string;
  createAt: string;
};

export type ClassroomItem = { id: string; values: ClassroomValues };
