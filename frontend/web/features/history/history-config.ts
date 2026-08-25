export const recordTypes: Record<string, { type: string; title: string; description: string }> = {
  students: { type: "Student", title: "Student records", description: "Every current and former student, with complete enrollment, profile, department, and status snapshots." },
  teachers: { type: "Teacher", title: "Teacher records", description: "Every current and former faculty member, with profile, assignment, and employment-status history." },
  classrooms: { type: "Classroom", title: "Classroom records", description: "All active and inactive rooms, including ownership, capacity, device, and status history." },
  courses: { type: "Course", title: "Course records", description: "All active and inactive courses, including department, teacher, capacity, and lifecycle history." },
  timetable: { type: "Timetable", title: "Timetable records", description: "All scheduled, completed, and cancelled classes with their complete scheduling history." },
  attendance: { type: "Attendance", title: "Attendance records", description: "All attendance entries and corrections, including records belonging to former students." },
  departments: { type: "Department", title: "Department records", description: "All active and inactive departments with leadership and organizational history." },
};
