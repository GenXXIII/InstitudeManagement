export const recordTypes: Record<string, { type: string; title: string; description: string }> = {
  overview: { type: "all", title: "History Overview", description: "One permanent overview for every coded change and snapshot created by Enrollment, Management, and institute operations." },
  all: { type: "all", title: "All History", description: "One permanent register for every change and snapshot created by Enrollment, Management, and institute operations." },
  "class-sessions": { type: "Class session", title: "Class session history", description: "Every completed class session and attendance correction recorded by institute operations." },
  students: { type: "Student", title: "Student records", description: "Every current and former student, with complete enrollment, profile, department, and status snapshots." },
  teachers: { type: "Teacher", title: "Teacher records", description: "Every current and former faculty member, with profile, assignment, and employment-status history." },
  classrooms: { type: "Classroom", title: "Classroom records", description: "All active and inactive rooms, including ownership, capacity, device, and status history." },
  courses: { type: "Course", title: "Course records", description: "All active and inactive courses, including department, teacher, capacity, and lifecycle history." },
  timetable: { type: "Timetable", title: "Timetable records", description: "All scheduled, completed, and cancelled classes with their complete scheduling history." },
  attendance: { type: "Attendance", title: "Attendance records", description: "All attendance entries and corrections, including records belonging to former students." },
  departments: { type: "Department", title: "Department records", description: "All active and inactive departments with leadership and organizational history." },
};
